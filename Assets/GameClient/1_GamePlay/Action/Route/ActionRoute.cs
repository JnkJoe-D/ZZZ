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
        [InspectorName("招架支援开始")]
        ParryAidStart = 30,
        [InspectorName("招架支援")]
        ParryAid = 40,
        [InspectorName("招架支援 点数不足")]
        FallbackEvasionIn = 35,
        [InspectorName("开始闪避支援")]
        EvasionAidStart = 50,
        [InspectorName("开始连携技")]
        ChainAttack = 60,
        [InspectorName("快速支援")]
        QuickAid = 70,
        [InspectorName("移动输入归零 阻尼")]
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

    public enum RouteArbitrationTiming
    {
        [InspectorName("帧末延迟裁决")]
        Deferred = 0,
        [InspectorName("即时抢占裁决")]
        Immediate = 10,
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
        [InspectorName("避险切人开始 点数不足")]
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
        [Header("执行目标")]
        [SerializeReference]
        [Tooltip("路由执行目标列表 支持动作与事件组合 按列表顺序依次执行")]
        public List<RouteExecutionTarget> Targets = new();

        #region 目标辅助属性
        public ExecuteTarget ExecuteType
        {
            get => GetPrimaryExecuteType();
            set
            {
                if (value == ExecuteTarget.Action)
                {
                    if (GetActionTarget() == null) Targets.Add(new ActionRouteTarget());
                }
                else if (value == ExecuteTarget.Event)
                {
                    if (GetFirstEventTarget() == null) Targets.Add(new EventRouteTarget());
                }
            }
        }

        public ActionConfigAsset ExecuteAction
        {
            get => GetActionTarget()?.Action;
            set
            {
                var actionTarget = GetActionTarget();
                if (actionTarget != null)
                {
                    actionTarget.Action = value;
                }
                else if (value != null)
                {
                    Targets.Add(new ActionRouteTarget { Action = value });
                }
            }
        }

        public bool ValidateSkillRequirement
        {
            get => GetActionTarget()?.ValidateSkillRequirement ?? true;
            set
            {
                var actionTarget = GetActionTarget();
                if (actionTarget != null)
                {
                    actionTarget.ValidateSkillRequirement = value;
                }
            }
        }

        public ExecuteEvent RouteExecuteEvent
        {
            get => GetFirstEventTarget()?.RouteExecuteEvent ?? ExecuteEvent.None;
            set
            {
                var eventTarget = GetFirstEventTarget();
                if (eventTarget != null)
                {
                    eventTarget.RouteExecuteEvent = value;
                }
                else if (value != ExecuteEvent.None)
                {
                    Targets.Add(new EventRouteTarget { RouteExecuteEvent = value });
                }
            }
        }
        #endregion

        [Header("仲裁执行")]
        public int Priority;

        [Tooltip("仲裁决选时机 抢占表示压入指令瞬间立即决选 延迟表示等待当前帧所有路由评估完毕后统一在帧末决选")]
        public RouteArbitrationTiming ArbitrationTiming = RouteArbitrationTiming.Deferred;

        [Header("触发策略")]
        [SerializeReference, SubclassSelector]
        public IRouteTrigger TriggerStrategy;

        [Header("额外条件")]
        [Tooltip("多额外条件之间的逻辑关系 全部满足或任一满足")]
        public ConditionCombineMode ExtraConditionCombine = ConditionCombineMode.AllMatch_AND;

        [SerializeReference, SubclassSelector]
        public List<ITransitionCondition> ExtraConditions = new();

        public ActionRouteTarget GetActionTarget()
        {
            if (Targets == null) return null;
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i] is ActionRouteTarget actionTarget)
                {
                    return actionTarget;
                }
            }
            return null;
        }

        public EventRouteTarget GetFirstEventTarget()
        {
            if (Targets == null) return null;
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i] is EventRouteTarget eventTarget)
                {
                    return eventTarget;
                }
            }
            return null;
        }

        public ExecuteTarget GetPrimaryExecuteType()
        {
            if (Targets == null || Targets.Count == 0) return ExecuteTarget.None;
            var act = GetActionTarget();
            if (act != null && act.Action != null) return ExecuteTarget.Action;
            var evt = GetFirstEventTarget();
            if (evt != null && evt.RouteExecuteEvent != ExecuteEvent.None) return ExecuteTarget.Event;
            if (act != null) return ExecuteTarget.Action;
            return ExecuteTarget.None;
        }

        /// <summary>
        /// 保证执行目标中动作目标数量合法 若存在多个动作目标 优先保留配置了有效动作资产的项并移除多余项
        /// </summary>
        public void EnsureActionTargetIntegrity()
        {
            if (Targets == null)
            {
                Targets = new List<RouteExecutionTarget>();
                return;
            }

            int actionCount = 0;
            ActionRouteTarget primaryAction = null;

            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i] is ActionRouteTarget act)
                {
                    actionCount++;
                    if (primaryAction == null)
                    {
                        primaryAction = act;
                    }
                    else if (primaryAction.Action == null && act.Action != null)
                    {
                        primaryAction = act;
                    }
                }
            }

            if (actionCount > 1)
            {
                for (int i = Targets.Count - 1; i >= 0; i--)
                {
                    if (Targets[i] is ActionRouteTarget && Targets[i] != primaryAction)
                    {
                        Targets.RemoveAt(i);
                    }
                }
            }
        }

        public bool Evaluate(CharacterCommand command, ATEditor.RouteWindow activeWindow, CharacterEntity actor, ISkillCostHandler skillHandler, RouteSingleModifierCheckTiming timing = RouteSingleModifierCheckTiming.EveryFrameInWindow)
        {
            // 检查当前动作周期内配额是否耗尽
            if (actor?.ActionController?.RouteExecutionTracker != null &&
                actor.ActionController.RouteExecutionTracker.IsRouteConsumed(this))
            {
                return false;
            }

            if (TriggerStrategy == null) return false;

            if (!TriggerStrategy.Evaluate(command, activeWindow, actor, timing))
                return false;

            // 如果这是由直接动作请求触发的 必须保证它请求的动作正是本路由指向的动作
            if (command != null && command.Payload is DirectAssetPayload directPayload)
            {
                var actionTarget = GetActionTarget();
                if (actionTarget != null && actionTarget.Action != null)
                {
                    if (directPayload.TargetAsset != actionTarget.Action)
                        return false;
                }
            }

            if (ExtraConditions != null && ExtraConditions.Count > 0)
            {
                if (!CommandRouteEvaluator.MatchesConditions(ExtraConditions, actor, ExtraConditionCombine))
                    return false;
            }

            return CheckSkillRequire(actor, skillHandler);
        }

        /// <summary>
        /// 校验当前路由配置是否有效 至少有一个有效目标 且动作目标不能超过一个
        /// </summary>
        public bool IsValid()
        {
            if (Targets == null || Targets.Count == 0) return false;

            int validTargetCount = 0;
            int actionCount = 0;
            for (int i = 0; i < Targets.Count; i++)
            {
                var target = Targets[i];
                if (target == null) continue;

                if (target.IsValid())
                {
                    validTargetCount++;
                    if (target is ActionRouteTarget)
                    {
                        actionCount++;
                    }
                }
            }

            return validTargetCount > 0 && actionCount <= 1;
        }

        public bool CheckSkillRequire(CharacterEntity actor, ISkillCostHandler skillHandler)
        {
            var actionTarget = GetActionTarget();
            if (actionTarget == null || !actionTarget.ValidateSkillRequirement) return true;
            return skillHandler == null || skillHandler.CheckSkillRequirement(actionTarget.Action, actor);
        }

        public void ConsumeSkillCost(CharacterEntity actor, ISkillCostHandler skillHandler)
        {
            var actionTarget = GetActionTarget();
            if (actionTarget == null || !actionTarget.ValidateSkillRequirement) return;
            skillHandler?.ConsumeSkillCost(actionTarget.Action, actor);
        }

        /// <summary>
        /// 路由被最终确认提交后调用 执行各额外条件中延迟的一次性副作用
        /// 例如清除招架成功标记等只能消费一次的状态
        /// 应在技能消耗之后紧跟调用
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
