using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    [Serializable]
    [SubclassDisplayName("是否有原始移动输入")]
    public sealed class MoveInputCondition : IKeyInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && actor.InputProvider.HasRawMoveInput();
        }
    }
    [Serializable]
    [SubclassDisplayName("是否有移动输入(阻尼)")]
    public sealed class MoveInputCondition2 : IKeyInputCondition
    {
        public bool Expected = true;
        public bool Check(RoleEntity actor)
        {
            if (!(actor is RoleEntity roleEntity)) return false;
            bool hasMovementInput = roleEntity?.InputProvider != null && roleEntity.InputProvider.HasMoveInput();
            return hasMovementInput == Expected;
        }
    }
    [Serializable]
    [SubclassDisplayName("是否是原始移动短输入")]
    public sealed class ShortMoveInputCondition : IKeyInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;

            // 优先依据动作自身播放流逝时长进行高内聚的闭环计算
            if (actor.ActionPlayer != null && actor.Config is RoleConfigAsset roleConfig)
            {
                // 输入相关的时间判定，使用 Time.time 作为参考
                float currentTime = Time.time;
                float elapsed = currentTime - actor.ActionPlayer.ActionStartTime;
                return elapsed <= roleConfig.InputConfig.MoveShortInputThreshold;
            }

            // 兜底支持从 DataModule 获取
            return actor.DataModule?.Get<ActionRuntimeData>() != null && actor.DataModule.Get<ActionRuntimeData>().IsShortMoveInput;
        }
    }
    [Serializable]
    [SubclassDisplayName("移动输入向量条件")]
    public sealed class Vector2MoveInputCondition : IKeyInputCondition
    {
        [Range(-1, 1)]
        public float XMin;
        [Range(-1, 1)]
        public float XMax;
        [Range(-1, 1)]
        public float YMin;
        [Range(-1, 1)]
        public float YMax;
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;

            Vector2 moveDir = actor.InputProvider?.GetMovementDirection() ?? Vector2.zero;

            return moveDir.x >= XMin && moveDir.x <= XMax && moveDir.y >= YMin && moveDir.y <= YMax;
        }
    }

    [Serializable]
    public sealed class LostMoveInputCondition : IKeyInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && !actor.InputProvider.HasMoveInput();
        }
    }

    /// <summary>
    /// 180度大幅度转向/掉头输入判定条件
    /// 利用当前原生输入（Raw Input）与输入残留缓存（Residual Input）的夹角计算，
    /// 精准识别玩家在奔跑中瞬间反向推摇杆或反向按键的掉头意图。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("180度转向输入条件")]
    public sealed class MoveTurnAroundCondition : IKeyInputCondition
    {
        [Tooltip("最小反向夹角（度），两向量夹角大于等于此阈值时判定为转向（180度掉头推荐 135~150 度）")]
        [Range(60f, 180f)]
        public float MinAngle = 135f;

        [Tooltip("是否要求当前必须有原生物理按键按下")]
        public bool RequireRawInput = true;

        [Tooltip("是否要求此前必须有有效输入残留（防止从完全静止起步时被误判为掉头）")]
        public bool RequireResidualInput = true;

        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            var provider = actor.InputProvider;
            if (provider == null) return false;

            if (RequireRawInput && !provider.HasRawMoveInput()) return false;
            if (RequireResidualInput && !provider.HasResidualMoveInput()) return false;

            Vector2 rawDir = provider.GetRawMovementDirection();
            Vector2 residualDir = provider.GetResidualMovementDirection();

            if (rawDir.sqrMagnitude < 0.001f || residualDir.sqrMagnitude < 0.001f)
            {
                return false;
            }

            float angle = Vector2.Angle(residualDir, rawDir);
            bool matched = angle >= MinAngle;
            return matched;
        }
    }

    /// <summary>
    /// 移动输入向量变化量 (Delta) 条件
    /// 计算原生输入向量与残留缓存向量的差值 (Raw - Residual) 长度
    /// </summary>
    [Serializable]
    [SubclassDisplayName("移动输入变化量条件")]
    public sealed class MoveInputDeltaCondition : IKeyInputCondition
    {
        [Tooltip("最小变化向量长度阈值（例如 180 度反向时差值最大可达 2.0）")]
        [Range(0.1f, 2f)]
        public float MinDeltaMagnitude = 1.2f;
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            var provider = actor.InputProvider;
            if (provider == null) return false;

            Vector2 delta = provider.GetRawMovementDirection() - provider.GetResidualMovementDirection();
            bool matched = delta.magnitude >= MinDeltaMagnitude;
            return matched;
        }
    }
}
