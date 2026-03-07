using System;
using UnityEngine;

namespace Game.Logic.Character.Config
{
    /// <summary>
    /// 标准的扁平化动画组条目
    /// 被用来配置角色在不同武器下的动作集合
    /// </summary>
    [Serializable]
    public class AnimSetEntry
    {
        [Tooltip("适用于哪个角色模型 (例如: 1001=主角男, 1002=萝莉)")]
        public int RoleID;

        [Tooltip("手持什么武器类型的特化 (例如: 0=通用/空手, 1=重剑, 2=双枪)")]
        public int WeaponType;

        [Header("基础移动表现与 Locomotion 覆盖")]
        public AnimUnitConfig Idle;
        public AnimUnitConfig JogStart;
        public AnimUnitConfig Jog;
        public AnimUnitConfig JogStop;
        public AnimUnitConfig DashStart;
        public AnimUnitConfig Dash;
        public AnimUnitConfig DashStop;
        public AnimUnitConfig DodgeFront;
        public AnimUnitConfig DodgeBack;
        public AnimUnitConfig JumpStart;
        public AnimUnitConfig FallLoop;
        public AnimUnitConfig Land;

        [Header("控制手感与硬直配置 (秒)")]
        [Tooltip("触发跑停动作时，禁止角色推摇杆挪动的硬直时间")]
        [Range(0,1f)]
        public float JogStopLockTime = 0.25f;
        [Tooltip("触发冲刺急停动作时，禁止角色推摇杆挪动的长硬直时间")]
        [Range(0, 1f)]
        public float DashStopLockTime = 0.5f;
    }
}
