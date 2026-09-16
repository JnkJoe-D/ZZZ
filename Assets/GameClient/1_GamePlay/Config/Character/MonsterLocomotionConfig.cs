using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
namespace Game.GamePlay
{
    /// <summary>
    /// 周旋走位方向枚举。
    /// </summary>
    public enum StrafeDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }

    /// <summary>
    /// 怪物移动决策意图枚举。
    /// 用于行为树与移动代理层的高层决策追踪，仅在决策改变时下发动作指令，防止每帧重播卡死在第一帧。
    /// </summary>
    public enum MonsterLocomotionIntent
    {
        None,
        Stop,
        Run,
        StrafeForward,
        StrafeBackward,
        StrafeLeft,
        StrafeRight
    }

    /// <summary>
    /// 怪物移动表现配置资产。
    /// 集中管理奔跑（RunStart/Loop/End）与全向走位步态（WalkF/B/L/R）的动作资产。
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterLocomotionConfig", menuName = "Config/Role/Monster Locomotion Config")]
    public class MonsterLocomotionConfig : GameConfigAsset
    {
        [Header("奔跑动作表现 (Run)")]
        [Tooltip("奔跑起步/冲刺动作")]
        public ActionConfigAsset RunStart;

        [Tooltip("奔跑循环动作")]
        public ActionConfigAsset RunLoop;

        [Tooltip("奔跑急停/刹车动作")]
        public ActionConfigAsset RunEnd;

        [Header("全向走位动作表现 (Walk)")]
        [Tooltip("慢速前压迈步动作")]
        public ActionConfigAsset WalkF;

        [Tooltip("战术后撤迈步动作")]
        public ActionConfigAsset WalkB;

        [Tooltip("左侧向环绕横移动作")]
        public ActionConfigAsset WalkL;

        [Tooltip("右侧向环绕横移动作")]
        public ActionConfigAsset WalkR;

        /// <summary>
        /// 收集所有配置的动作资产，用于统一预热与资源依赖解析。
        /// </summary>
        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (RunStart != null) yield return RunStart;
            if (RunLoop != null) yield return RunLoop;
            if (RunEnd != null) yield return RunEnd;
            if (WalkF != null) yield return WalkF;
            if (WalkB != null) yield return WalkB;
            if (WalkL != null) yield return WalkL;
            if (WalkR != null) yield return WalkR;
        }
    }
}
