using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由输入条件接口
    /// </summary>
    public interface IRawInputCondition
    {
        bool Check(RoleEntity actor);
    }

    [Serializable]
    [SubclassDisplayName("是否有原始移动输入")]
    public class MoveInputCondition : IRawInputCondition
    {
        public bool Inverse = false;
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return !Inverse && (actor.InputProvider != null && actor.InputProvider.HasRawMoveInput());
        }
    }

    [Serializable]
    [SubclassDisplayName("是否是原始移动短输入")]
    public class ShortMoveInputCondition : IRawInputCondition
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
    public class LostMoveInputCondition : IRawInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && !actor.InputProvider.HasMoveInput();
        }
    }
}
