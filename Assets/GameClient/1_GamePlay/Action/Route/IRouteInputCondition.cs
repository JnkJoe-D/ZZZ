using System;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由输入条件接口：继承自 ITransitionCondition 以实现全系统条件契约归一化。
    /// </summary>
    public interface IRouteInputCondition : ITransitionCondition
    {
    }

    [Serializable]
    public class MoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && actor.InputProvider.HasMovementInput();
        }
    }

    [Serializable]
    public class ShortMoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;

            // 优先依据动作自身播放流逝时长进行高内聚的闭环计算
            if (actor.ActionPlayer != null && actor.Config is RoleConfigAsset roleConfig)
            {
                float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;
                float elapsed = currentTime - actor.ActionPlayer.ActionStartTime;
                return elapsed <= roleConfig.JogShortInputThreshold;
            }

            // 兜底支持从 DataModule 获取
            return actor.DataModule?.Get<ActionRuntimeData>() != null && actor.DataModule.Get<ActionRuntimeData>().IsShortMoveInput;
        }
    }

    [Serializable]
    public class LostMoveInputCondition : IRouteInputCondition
    {
        public bool Check(RoleEntity actor)
        {
            if (actor == null || !actor.IsControlActive) return false;
            return actor.InputProvider != null && !actor.InputProvider.HasMovementInput();
        }
    }
}
