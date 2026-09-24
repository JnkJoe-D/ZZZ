using System;
using System.Collections.Generic;
using Game.Framework;
using UnityEngine;


namespace Game.GamePlay
{
    public enum RouteEventType
    {
        [InspectorName("无")]
        None = 0,
        [InspectorName("切入")]
        SwitchIn = 10,
        [InspectorName("切出")]
        SwitchOut = 20,
        [InspectorName("开始招架支援")]
        ParryAidStart = 30,
        [InspectorName("招架支援成功")]
        ParryAidSucceed = 40,
        [InspectorName(" 无点数避险切入")]
        FallbackEvasionIn = 35,
        [InspectorName("开始闪避支援")]
        EvasionAidStart = 50,
        [InspectorName("开始连携技")]
        ChainAttack = 60,
        [InspectorName("快速支援")]
        QuickAid = 70,
        [InspectorName("移动输入归零(阻尼)")]
        LostMoveInput = 80,
        [InspectorName("移动输入归零")]
        LostMoveInputRaw = 85,
    }

    public enum ModifierCategory
    {
        None = 0,
        KeyState = 10,
        Condition = 20
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
        [InspectorName("时间轴回卷")]
        TimelineRewind = 20,
        [InspectorName("设置时间轴跳跃标记")]
        TimelineSkip = 30,
        [InspectorName("招架支援开始")]
        ParryAidStart = 40,
        [InspectorName("避险切人开始 (点数不足)")]
        FallbackEvasionStart = 45,
        [InspectorName("闪避支援开始")]
        EvasionAidStart = 50,
        [InspectorName("连携技开始")]
        ChainAttackStart = 60,
        [InspectorName("快速支援开始")]
        QuickAidStart = 70,
    }

    [Serializable]
    public class RouteModifierCheck
    {
        public ModifierCategory Category = ModifierCategory.None;

        [ShowIf("Category", ModifierCategory.KeyState)]
        public HardwareInputType RequiredKey;

        [ShowIf("Category", ModifierCategory.Condition)]
        [SerializeReference, SubclassSelector]
        public IKeyInputCondition InputCondition;

        public bool Inverse = false;

        public bool Evaluate(CharacterEntity actor)
        {
            if(!(actor is RoleEntity roleEntity)) return false;
            switch (Category)
            {
                case ModifierCategory.None:
                    return true;
                case ModifierCategory.Condition:
                    bool conditionResult = InputCondition != null && InputCondition.Check(roleEntity);
                    return Inverse ? !conditionResult : conditionResult;
                case ModifierCategory.KeyState:
                    if (actor == null || !roleEntity.IsControlActive || roleEntity.InputProvider == null)
                        return false;
                    bool isHeld = roleEntity.InputProvider.IsHeld((int)RequiredKey);
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

        public bool Evaluate(CharacterCommand command, ATEditor.RouteWindow activeWindow, CharacterEntity actor, ISkillCostHandler skillHandler, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            if (TriggerStrategy == null) return false;

            if (!TriggerStrategy.Evaluate(command, activeWindow, actor, timing))
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
                if (!CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor))
                    return false;
            }

            return CheckSkillRequire(actor, skillHandler);
        }

        /// <summary>
        /// 校验当前路由配置是否有效（目标合法且不为空）。
        /// </summary>
        public bool IsValid()
        {
            if (ExecuteType == ExecuteTarget.None) return false;
            if (ExecuteType == ExecuteTarget.Action && ExecuteAction == null) return false;
            if (ExecuteType == ExecuteTarget.Event && RouteExecuteEvent == ExecuteEvent.None) return false;
            return true;
        }

        [Obsolete("Use IsValid() instead. Note that IsInvalid now correctly returns true when invalid.", false)]
        public bool IsInvalid() => !IsValid();




        public bool CheckSkillRequire(CharacterEntity actor, ISkillCostHandler skillHandler)
        {
            if (!ValidateSkillRequirement) return true;
            return skillHandler == null || skillHandler.CheckSkillRequirement(ExecuteAction, actor);
        }

        public void ConsumeSkillCost(CharacterEntity actor, ISkillCostHandler skillHandler)
        {
            if (!ValidateSkillRequirement) return;
            skillHandler?.ConsumeSkillCost(ExecuteAction, actor);
        }

        /// <summary>
        /// 路由被最终确认提交后调用，执行各 ExtraCondition 中延迟的一次性副作用
        /// （例如清除招架成功标记等只能消费一次的状态）。
        /// 应在 ConsumeSkillCost 之后紧跟调用。
        /// </summary>
        public void CommitSideEffects(CharacterEntity actor)
        {
            if (ExtraConditions == null || ExtraConditions.Count == 0) return;
            for (int i = 0; i < ExtraConditions.Count; i++)
            {
                ExtraConditions[i]?.OnCommit(actor);
            }
        }
    }
}
