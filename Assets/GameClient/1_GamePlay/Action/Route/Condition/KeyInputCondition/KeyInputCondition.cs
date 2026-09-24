using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    [Serializable]
    [SubclassDisplayName("是否有原始移动输入")]
    public sealed class MoveInputCondition : IKeyInputCondition
    {
        public bool Inverse = false;
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return !Inverse && (actor.InputProvider != null && actor.InputProvider.HasRawMoveInput());
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
}
