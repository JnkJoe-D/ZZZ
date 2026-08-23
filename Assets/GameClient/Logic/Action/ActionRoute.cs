using System;
using System.Collections.Generic;
using Game.Input;
using Game.Logic;
using Game.Framework;
using UnityEngine;

namespace Game.Logic
{
    public enum RouteTriggerCategory
    {
        IntentCommand = 0,
        DirectAsset = 10,
        Event = 20,
        AutoTransition = 30,
        ConditionOnly = 40
    }

    public enum RouteEventType
    {
        None = 0,
        SwitchIn = 10,
        SwitchOut = 20,
    }

    public enum ModifierCategory
    {
        None = 0,
        KeyState = 10,
        Condition = 20
    }

    public enum SpeedMultiplierType
    {
        None = 0,
        Jog = 10,
        Dash = 20,
        Dodge = 30,
        Attack = 40,
        Skill = 50
    }

    public enum RouteSingleModifierCheckTiming
    {
        EveryFrameInWindow = 0,
        OnWindowEnter = 10,
        OnWindowExit = 20,
    }

    public enum ExecuteTarget
    {
        None = 0,
        Action = 10,
        Event = 20,
    }
    public enum ExecuteEvent
    {
        None = 0,
        SwitchCaptureSucceed = 10,
        [InspectorName("时间轴回卷 (TimelineRewind)")]
        TimelineRewind = 20,
        [InspectorName("设置时间轴跳跃标记 (TimelineSkip)")]
        TimelineSkip = 30,
    }

    [Serializable]
    public class RouteModifierCheck
    {
        public ModifierCategory Category = ModifierCategory.None;

        [ShowIf("Category", ModifierCategory.KeyState)]
        public HardwareInputType RequiredKey;

        [ShowIf("Category", ModifierCategory.Condition)]
        public ConditionCommand Condition = ConditionCommand.None;

        public bool Inverse = false;

        public bool Evaluate(RoleEntity actor)
        {
            switch (Category)
            {
                case ModifierCategory.None:
                    return true;
                case ModifierCategory.Condition:
                    bool conditionResult = CheckCondition(Condition, actor);
                    return Inverse ? !conditionResult : conditionResult;
                case ModifierCategory.KeyState:
                    if (actor?.InputProvider == null)
                        return false;
                    bool isHeld = actor.InputProvider.IsHeld((int)RequiredKey);
                    return Inverse ? !isHeld : isHeld;
                default:
                    return true;
            }
        }

        private bool CheckCondition(ConditionCommand condition, RoleEntity entity)
        {
            if (entity == null) return false;
            return condition switch
            {
                ConditionCommand.None => true,
                ConditionCommand.Move => entity.InputProvider != null && entity.InputProvider.HasMovementInput(),
                ConditionCommand.ShortMove => entity.DataModule?.Get<ActionRuntimeData>() != null && entity.DataModule.Get<ActionRuntimeData>().IsShortMoveInput,
                ConditionCommand.LostMove => entity.InputProvider != null && !entity.InputProvider.HasMovementInput(),
                ConditionCommand.SwitchOutPending => entity.DataModule?.Get<SwitchRuntimeData>() != null && entity.DataModule.Get<SwitchRuntimeData>().IsSwitchOutPending,
                _ => false
            };
        }
    }

    [Serializable]
    public class ActionRoute
    {
        [Header("Execution Target")]
        public ExecuteTarget ExecuteType = ExecuteTarget.Action;

        [ShowIf("ExecuteType", ExecuteTarget.Action)]
        public ActionConfigAsset ExecuteAction;

        [ShowIf("ExecuteType", ExecuteTarget.Action)]
        [Tooltip("是否需要校验下一个动作在配表中配置的释放条件（如能量/耐力要求等）及执行消耗。默认为 true；若为 false 则无需校验条件且不扣除配表消耗，可直接释放。")]
        public bool ValidateSkillRequirement = true;

        [ShowIf("ExecuteType", ExecuteTarget.Event)]
        public ExecuteEvent RouteExecuteEvent;

        [Header("Execution")]
        public int Priority;

        [Tooltip("-1表示使用下个动作自身设定的混合时间，>=0则强制覆盖混合时间。")]
        public float CrossfadeOverride = -1f;

        [Header("Trigger Strategy")]
        [SerializeReference, SubclassSelector]
        public IRouteTrigger TriggerStrategy;

        [Header("Extra Conditions")]
        [SerializeReference, SubclassSelector]
        public List<ITransitionCondition> ExtraConditions = new();

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (TriggerStrategy == null) return false;

            if (!TriggerStrategy.Evaluate(command, windowTag, actor, timing))
                return false;

            // 新增：如果这是由 DirectAsset 触发的，必须保证它请求的动作正是本路由指向的动作
            if (command != null && command.Payload is DirectAssetPayload directPayload)
            {
                if (ExecuteType == ExecuteTarget.Action && ExecuteAction != null)
                {
                    if (directPayload.TargetAsset != ExecuteAction)
                        return false;
                }
            }

            if (ExtraConditions != null && ExtraConditions.Count > 0)
            {
                bool conditionResult = CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor);
                if (!conditionResult && ExecuteAction != null && ExecuteAction.Name.Contains("Attack"))
                {
                    Debug.Log($"<color=orange>[RouteTrace] {ExecuteAction.Name} 的 ExtraConditions 检查未通过！</color>");
                }
                if (!conditionResult) return false;
            }

            return CheckSkillRequire(actor);
        }

        public bool IsInvalid()
        {
            if(ExecuteType == ExecuteTarget.None
            || (ExecuteType == ExecuteTarget.Action && ExecuteAction == null)
            || (ExecuteType == ExecuteTarget.Event && RouteExecuteEvent == ExecuteEvent.None))
            {
                return false;
            }
            return true;
        }




        public bool CheckSkillRequire(RoleEntity actor)
        {
            if (!ValidateSkillRequirement) return true;
            if (ExecuteAction == null || ExecuteAction.ID <= 0) return true;
            if (actor?.StatusModule?.Attributes == null) return true;
            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(ExecuteAction.ID);
            if (skillConfig == null) return true;

            if (skillConfig.Condition != null)
            {
                foreach (var cond in skillConfig.Condition)
                {
                    float currentVal = actor.StatusModule.Attributes.GetCurrent((AttributeId)cond.AttrId);
                    bool pass = cond.Op switch
                    {
                        cfg.ZZZ.CompareOp.GreaterEqual => currentVal >= cond.Value,
                        cfg.ZZZ.CompareOp.Greater => currentVal > cond.Value,
                        cfg.ZZZ.CompareOp.LessEqual => currentVal <= cond.Value,
                        cfg.ZZZ.CompareOp.Less => currentVal < cond.Value,
                        cfg.ZZZ.CompareOp.Equal => Mathf.Approximately(currentVal, cond.Value),
                        _ => true
                    };
                    if (!pass) return false;
                }
            }
            return true;
        }

        public void ConsumeSkillCost(RoleEntity actor)
        {
            if (!ValidateSkillRequirement) return;
            if (ExecuteAction == null || ExecuteAction.ID <= 0) return;
            if (actor?.StatusModule?.Attributes == null) return;

            var skillConfig = ConfigManager.Instance.Tables.TbSkill.GetOrDefault(ExecuteAction.ID);
            if (skillConfig == null || skillConfig.Cost == null) return;

            foreach (var cost in skillConfig.Cost)
            {
                if (cost.Amount > 0)
                {
                    actor.StatusModule.Attributes.Modify((AttributeId)cost.AttrId, -cost.Amount);
                    Debug.Log($"<color=cyan>[SkillCost] {actor.name} 使用技能 {ExecuteAction.ID} 消耗了 {cost.Amount} 点 {cost.AttrId}</color>");
                }
            }
        }
    }
}
