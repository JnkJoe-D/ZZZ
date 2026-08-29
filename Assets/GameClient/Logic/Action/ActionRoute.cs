using System;
using System.Collections.Generic;
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
        [SerializeReference, SubclassSelector]
        public IRouteInputCondition InputCondition;

        public bool Inverse = false;

        public bool Evaluate(RoleEntity actor)
        {
            switch (Category)
            {
                case ModifierCategory.None:
                    return true;
                case ModifierCategory.Condition:
                    bool conditionResult = InputCondition != null && InputCondition.Check(actor);
                    return Inverse ? !conditionResult : conditionResult;
                case ModifierCategory.KeyState:
                    if (actor == null || !actor.IsControlActive || actor.InputProvider == null)
                        return false;
                    bool isHeld = actor.InputProvider.IsHeld((int)RequiredKey);
                    return Inverse ? !isHeld : isHeld;
                default:
                    return true;
            }
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

        public bool Evaluate(CharacterCommand command, string windowTag, RoleEntity actor, ISkillCostHandler skillHandler, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (TriggerStrategy == null) return false;

            if (!TriggerStrategy.Evaluate(command, windowTag, actor, timing))
                return false;

            //  如果这是由 DirectAsset 触发的，必须保证它请求的动作正是本路由指向的动作
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

            return CheckSkillRequire(actor, skillHandler);
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




        public bool CheckSkillRequire(RoleEntity actor, ISkillCostHandler skillHandler)
        {
            if (!ValidateSkillRequirement) return true;
            return skillHandler == null || skillHandler.CheckSkillRequirement(ExecuteAction, actor);
        }

        public void ConsumeSkillCost(RoleEntity actor, ISkillCostHandler skillHandler)
        {
            if (!ValidateSkillRequirement) return;
            skillHandler?.ConsumeSkillCost(ExecuteAction, actor);
        }
    }
}
