using System;
using Game.Framework;
using Game.GamePlay;
using UnityEngine;

namespace Game.GamePlay
{
    public enum ComparisonMode
    {
        LessThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// 角色专属过渡条件基类：仅对 RoleEntity 生效，对非角色实体直接安全返回 false。
    /// </summary>
    [Serializable]
    [ConditionScope(ConditionScope.Role)]
    public abstract class RoleConditionBase : ITransitionCondition
    {
        public bool Check(CharacterEntity actor)
        {
            if (actor is not RoleEntity role) return false;
            return CheckRole(role);
        }

        protected abstract bool CheckRole(RoleEntity role);

        public virtual void OnCommit(CharacterEntity actor)
        {
            if (actor is RoleEntity role) OnCommitRole(role);
        }

        protected virtual void OnCommitRole(RoleEntity role) { }
    }

    /// <summary>
    /// 怪物专属过渡条件基类：仅对 MonsterEntity 生效，对非怪物实体直接安全返回 false。
    /// </summary>
    [Serializable]
    [ConditionScope(ConditionScope.Monster)]
    public abstract class MonsterConditionBase : ITransitionCondition
    {
        public bool Check(CharacterEntity actor)
        {
            if (actor is not MonsterEntity monster) return false;
            return CheckMonster(monster);
        }

        protected abstract bool CheckMonster(MonsterEntity monster);

        public virtual void OnCommit(CharacterEntity actor)
        {
            if (actor is MonsterEntity monster) OnCommitMonster(monster);
        }

        protected virtual void OnCommitMonster(MonsterEntity monster) { }
    }

    [Serializable]
    [SubclassDisplayName("是否有移动输入(阻尼)")]
    public sealed class HasMovementInputCondition : RoleConditionBase
    {
        public bool Expected = true;

        protected override bool CheckRole(RoleEntity role)
        {
            bool hasMovementInput = role.InputProvider != null && role.InputProvider.HasMoveInput();
            return hasMovementInput == Expected;
        }
    }
    [Serializable]
    [SubclassDisplayName("动作开始后经过时间")]
    public sealed class TimeSinceActionStartCondition : ITransitionCondition
    {
        public float Threshold = 0.2f;

        public ComparisonMode Mode = ComparisonMode.LessThan;

        public bool Check(CharacterEntity actor)
        {
            if (actor?.ActionPlayer == null)
            {
                return false;
            }
            // 输入相关的时间判定，使用 Time.time 作为参考
            float currentTime = Time.time;
            float elapsed = currentTime - actor.ActionPlayer.ActionStartTime;
            return Mode == ComparisonMode.LessThan ? elapsed < Threshold : elapsed >= Threshold;
        }
    }

    /// <summary>
    /// 索敌范围内是否有目标。用于 ActionRoute.ExtraConditions。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("是否有锁定目标 (HasTarget)")]
    public sealed class HasTargetCondition : ITransitionCondition
    {
        [Tooltip("反转结果：勾选后变为'没有锁定目标时为真'。")]
        public bool Inverse;

        public bool Check(CharacterEntity actor)
        {
            // 如果实体没有 TargetFinder 组件，默认找不到
            if (actor?.TargetFinder == null)
            {
                return Inverse;
            }

            // TargetFinder 只要能拿到对象就算有 Target
            bool hasTarget = actor.TargetFinder.GetTarget() != null;
            
            return Inverse ? !hasTarget : hasTarget;
        }
    }

    public enum DistanceComparisonMode
    {
        [InspectorName("小于指定距离 (LessThan)")]
        LessThan = 0,
        [InspectorName("大于等于指定距离 (GreaterThanOrEqual)")]
        GreaterThanOrEqual = 1,
        [InspectorName("区间内 [Min, Max] (Between)")]
        Between = 2,
        [InspectorName("区间外 (Outside)")]
        Outside = 3
    }

    /// <summary>
    /// 与锁定目标的距离条件。用于 ActionRoute.ExtraConditions。
    /// 支持单阈值比较 (小于/大于等于) 或区间判断 (Between [Min, Max])。
    /// 例如：后撤远可配置为 Between [2.5m, 6.0m]，当距离超出 6.0m 远投极限时不转到后撤远。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("目标距离条件 (TargetDistance)")]
    public sealed class TargetDistanceCondition : ITransitionCondition
    {
        [Tooltip("比较模式：小于、大于等于、区间内 [Min, Max]、区间外")]
        public DistanceComparisonMode Mode = DistanceComparisonMode.LessThan;

        [Tooltip("距离阈值 / 区间下限 Min（米）。例如 2.5 米。")]
        public float DistanceThreshold = 2.5f;

        [ShowIf("Mode", DistanceComparisonMode.Between, DistanceComparisonMode.Outside)]
        [Tooltip("区间上限 Max（米）。仅在模式为 Between 或 Outside 时生效。")]
        public float MaxDistance = 6.0f;

        [Tooltip("当没有锁定目标时的判定行为：若为 true 则无目标时视为满足条件；若为 false 则无目标时距离视为无穷大。")]
        public bool TreatNoTargetAsMatch = false;

        public bool Check(CharacterEntity actor)
        {
            Transform target = actor?.TargetFinder?.GetTarget();
            if (target == null)
            {
                if (TreatNoTargetAsMatch) return true;
                return Mode == DistanceComparisonMode.GreaterThanOrEqual || Mode == DistanceComparisonMode.Outside;
            }

            float dist = Vector3.Distance(actor.transform.position, target.position);
            return Mode switch
            {
                DistanceComparisonMode.LessThan => dist < DistanceThreshold,
                DistanceComparisonMode.GreaterThanOrEqual => dist >= DistanceThreshold,
                DistanceComparisonMode.Between => dist >= DistanceThreshold && dist <= MaxDistance,
                DistanceComparisonMode.Outside => dist < DistanceThreshold || dist > MaxDistance,
                _ => false
            };
        }
    }

    /// <summary>
    /// 前置动作条件。用于跨动作保留派生状态。
    /// 例如：如果动作B是从动作A派生来的，那么动作B里可以配置一条通往动作C的路由，并附加该条件（要求前置动作=A），
    /// 从而防止动作B在其他情况下也派生动作C。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("前置动作条件")]
    public sealed class PreviousActionCondition : ITransitionCondition
    {
        [Tooltip("要求上一个执行的动作必须是这个。")]
        public ActionConfigAsset RequiredAction;

        [Tooltip("反转条件：勾选则表示上一个动作【不是】这个。")]
        public bool Inverse = false;

        public bool Check(CharacterEntity actor)
        {
            if (RequiredAction == null || RequiredAction.ID <= 0) return Inverse;

            var history = actor?.ActionController?.ExecutionHistory;
            // history[0] 是当前正在执行的动作，history[1] 是上一个动作
            if (history == null || history.Count < 2)
            {
                return Inverse;
            }

            bool matched = history[1].ActionId == RequiredAction.ID;
            return Inverse ? !matched : matched;
        }
    }
    [Serializable]
    [SubclassDisplayName("正在等待切出")]
    public sealed class SwitchOutPendingCondition : RoleConditionBase
    {
        protected override bool CheckRole(RoleEntity role)
        {
            return role.DataModule?.Get<SwitchRuntimeData>() != null && role.DataModule.Get<SwitchRuntimeData>().IsSwitchOutPending;
        }
    }

    [Serializable]
    [SubclassDisplayName("战斗预警判定")]
    public sealed class CombatWarningCondition : RoleConditionBase
    {
        [Tooltip("预警信号类型：Yellow_Parryable (黄光可招架) / Red_Unparryable (红光不可招架)")]
        public ATEditor.WarningSignalType WarningType = ATEditor.WarningSignalType.Yellow_Parryable;

        [Tooltip("是否必须处于怪物的实际攻击威胁覆盖域内 (CoverageArea)。避险切人路由建议勾选，避免在安全区切人误触发避险闪避")]
        public bool RequireInCoverageArea = false;

        protected override bool CheckRole(RoleEntity role)
        {
            var marker = CombatWarningManager.GetValidWarning(role, WarningType);
            if (marker == null) return false;

            // 若要求处于怪物的实际攻击威胁覆盖域内
            if (RequireInCoverageArea)
            {
                bool inDanger = false;
                if (marker.CoverageShape != null)
                {
                    inDanger = marker.IsPositionInCoverage(role.transform.position);
                }
                else if (marker.Attacker != null)
                {
                    // 兜底：若未配置复杂 CoverageShape，只要在半程探测距离内视为处于危险区
                    float fallbackDist = marker.DetectionRadius > 0 ? marker.DetectionRadius * 0.5f : CombatWarningManager.DefaultDetectionRadius * 0.5f;
                    Vector3 toActor = role.transform.position - marker.Attacker.transform.position;
                    toActor.y = 0;
                    inDanger = toActor.sqrMagnitude <= fallbackDist * fallbackDist;
                }

                if (!inDanger) return false;
            }

            if (role.DataModule?.Get<ActionRuntimeData>() != null)
            {
                role.DataModule.Get<ActionRuntimeData>().Set(nameof(ActionRuntimeData.MatchedWarningMarker), marker);
            }
            return true;
        }
    }

    [Serializable]
    [SubclassDisplayName("是否处于极限视界")]
    public sealed class IsInBulletTimeCondition : RoleConditionBase
    {
        [Tooltip("反转结果：勾选后表示'不在子弹时间中为真'")]
        public bool Inverse = false;

        [Tooltip("是否必须由当前角色自身触发")]
        public bool RequireSelfInstigated = true;

        protected override bool CheckRole(RoleEntity role)
        {
            var evadeData = role.DataModule?.Get<EvadeRuntimeData>();
            bool isActive = evadeData != null && evadeData.IsInBulletTimeWindow;

            return Inverse ? !isActive : isActive;
        }
    }

    [Serializable]
    [SubclassDisplayName("招架强度")]
    public sealed class ParryWeightCondition : RoleConditionBase
    {
        [Tooltip("期望匹配的招架强度级别")]
        public ATEditor.ParryWeight ExpectedWeight = ATEditor.ParryWeight.Heavy;

        protected override bool CheckRole(RoleEntity role)
        {
            var parryData = role.DataModule?.Get<ParryRuntimeData>();
            if (parryData == null) return false;
            // 只检查状态，不在此处清除 —— 副作用延迟到 OnCommit 执行，
            // 防止多路由 Evaluate 时第一条路由就把状态消费掉
            if (!parryData.ParrySucceeded) return false;
            return parryData.LastParryWeight == ExpectedWeight;
        }

        protected override void OnCommitRole(RoleEntity role)
        {
            // 路由被最终确认提交后，才清除一次性招架标记，确保高优先级路由能正确竞争
            var parryData = role.DataModule?.Get<ParryRuntimeData>();
            if (parryData != null)
            {
                parryData.Set(nameof(parryData.ParrySucceeded), false);
            }
        }
    }
}