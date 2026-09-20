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
    [Serializable]
    [SubclassDisplayName("是否有移动输入(阻尼)")]
    public sealed class HasMovementInputCondition : ITransitionCondition
    {
        public bool Expected = true;

        public bool Check(RoleEntity actor)
        {
            bool hasMovementInput = actor?.InputProvider != null && actor.InputProvider.HasMoveInput();
            return hasMovementInput == Expected;
        }
    }
    [Serializable]
    [SubclassDisplayName("动作开始后经过时间")]
    public sealed class TimeSinceActionStartCondition : ITransitionCondition
    {
        public float Threshold = 0.2f;

        public ComparisonMode Mode = ComparisonMode.LessThan;

        public bool Check(RoleEntity actor)
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

        public bool Check(RoleEntity actor)
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

        public bool Check(RoleEntity actor)
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

    public enum MovementDirectionType
    {
        [InspectorName("后退")]
        Backward = 0,
        [InspectorName("前进")]
        Forward = 1,
        [InspectorName("左向")]
        Left = 2,
        [InspectorName("右向")]
        Right = 3,
    }

    /// <summary>
    /// 移动输入方向条件。用于 ActionRoute.ExtraConditions。
    /// 例如：后拉摇杆 (S 键) 派生后撤投刀 (Back)。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("移动输入方向条件")]
    public sealed class MovementDirectionCondition : ITransitionCondition
    {
        [Tooltip("期望的移动输入方向。")]
        public MovementDirectionType Direction = MovementDirectionType.Backward;

        [Tooltip("判定公差角度（范围 15~90 度，默认 60 度扇形）。")]
        [Range(15f, 90f)]
        public float AngleTolerance = 60f;

        [Tooltip("反转判定结果。")]
        public bool Inverse = false;

        public bool Check(RoleEntity actor)
        {
            if (actor?.InputProvider == null) return Inverse;

            Vector2 rawInput = actor.InputProvider.GetMovementDirection();
            if (rawInput.sqrMagnitude < 0.04f)
            {
                return Inverse;
            }

            // rawInput: x = Horizontal (A/D), y = Vertical (W/S)
            // y < 0 为后拉/后退, y > 0 为推前, x < 0 为左, x > 0 为右
            Vector2 targetDir = Direction switch
            {
                MovementDirectionType.Backward => new Vector2(0f, -1f),
                MovementDirectionType.Forward => new Vector2(0f, 1f),
                MovementDirectionType.Left => new Vector2(-1f, 0f),
                MovementDirectionType.Right => new Vector2(1f, 0f),
                _ => Vector2.zero
            };

            float angle = Vector2.Angle(rawInput.normalized, targetDir);
            bool matched = angle <= AngleTolerance;

            return Inverse ? !matched : matched;
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

        public bool Check(RoleEntity actor)
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
    public sealed class SwitchOutPendingCondition : ITransitionCondition
    {
        public bool Check(RoleEntity actor)
        {
            return actor?.DataModule?.Get<SwitchRuntimeData>() != null && actor.DataModule.Get<SwitchRuntimeData>().IsSwitchOutPending;
        }
    }

    [Serializable]
    [SubclassDisplayName("战斗预警判定")]
    public sealed class CombatWarningCondition : ITransitionCondition
    {
        [Tooltip("预警信号类型：Yellow_Parryable (黄光可招架) / Red_Unparryable (红光不可招架)")]
        public ATEditor.WarningSignalType WarningType = ATEditor.WarningSignalType.Yellow_Parryable;

        [Tooltip("是否必须处于怪物的实际攻击威胁覆盖域内 (CoverageArea)。避险切人路由建议勾选，避免在安全区切人误触发避险闪避")]
        public bool RequireInCoverageArea = false;

        public bool Check(RoleEntity actor)
        {
            var marker = CombatWarningManager.GetValidWarning(actor, WarningType);
            if (marker == null) return false;

            // 若要求处于怪物的实际攻击威胁覆盖域内
            if (RequireInCoverageArea)
            {
                bool inDanger = false;
                if (marker.CoverageShape != null)
                {
                    inDanger = marker.IsPositionInCoverage(actor.transform.position);
                }
                else if (marker.Attacker != null)
                {
                    // 兜底：若未配置复杂 CoverageShape，只要在半程探测距离内视为处于危险区
                    float fallbackDist = marker.DetectionRadius > 0 ? marker.DetectionRadius * 0.5f : CombatWarningManager.DefaultDetectionRadius * 0.5f;
                    Vector3 toActor = actor.transform.position - marker.Attacker.transform.position;
                    toActor.y = 0;
                    inDanger = toActor.sqrMagnitude <= fallbackDist * fallbackDist;
                }

                if (!inDanger) return false;
            }

            if (actor.DataModule?.Get<ActionRuntimeData>() != null)
            {
                actor.DataModule.Get<ActionRuntimeData>().Set(nameof(ActionRuntimeData.MatchedWarningMarker), marker);
            }
            return true;
        }
    }

    [Serializable]
    [SubclassDisplayName("是否处于极限视界")]
    public sealed class IsInBulletTimeCondition : ITransitionCondition
    {
        [Tooltip("反转结果：勾选后表示'不在子弹时间中为真'")]
        public bool Inverse = false;

        [Tooltip("是否必须由当前角色自身触发")]
        public bool RequireSelfInstigated = true;

        public bool Check(RoleEntity actor)
        {
            var evadeData = actor?.DataModule?.Get<EvadeRuntimeData>();
            bool isActive = evadeData != null && evadeData.IsInBulletTimeWindow;

            return Inverse ? !isActive : isActive;
        }
    }

    [Serializable]
    [SubclassDisplayName("招架强度")]
    public sealed class ParryWeightCondition : ITransitionCondition
    {
        [Tooltip("期望匹配的招架强度级别")]
        public ATEditor.ParryWeight ExpectedWeight = ATEditor.ParryWeight.Heavy;

        public bool Check(RoleEntity actor)
        {
            var parryData = actor?.DataModule?.Get<ParryRuntimeData>();
            if (parryData == null) return false;
            if (!parryData.ParrySucceeded) return false; // 确保只在招架成功后才匹配，避免在非招架情况下触发路由
            parryData.Set(nameof(parryData.ParrySucceeded), false); // 只要检查过一次就清掉，避免重复触发
            return parryData.LastParryWeight == ExpectedWeight;
        }
    }
}