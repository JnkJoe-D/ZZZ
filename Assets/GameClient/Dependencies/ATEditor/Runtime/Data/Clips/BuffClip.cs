using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 时间轴 Buff 生命周期管理模式
    /// </summary>
    public enum BuffClipLifetimeMode
    {
        /// <summary>
        /// 完全由时间轴片段区间管理（默认模式）：
        /// 进入片段时添加 Buff，离开片段时主动移除 Buff；
        /// 若动作被切招、受击或强制打断，强力安全注销清理（绝对防泄漏）。
        /// 典型应用：闪避判定帧区间、动作挥刀无敌帧、蓄力霸体帧。
        /// </summary>
        ManageByClip = 0,

        /// <summary>
        /// 仅作为施加源触发：
        /// 进入片段时施加 Buff，离开片段不主动移除，生命周期脱离动作，由 Buff 自身的 Duration 与全局时钟独立倒计时。
        /// 典型应用：换人起手无敌、暴击药水、狂暴 Buff。
        /// </summary>
        TriggerInstant = 1,
    }

    /// <summary>
    /// 时间轴 Buff 片段。
    /// 遵循单向依赖规范：只依赖 UnityEngine 与 ATEditor 基础抽象，绝不直接引用业务层具体的 BuffDefAsset 类。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(StatusTrack), "增益状态 (Buff)")]
    public class BuffClip : ClipBase
    {
        [Tooltip("Buff 配置 ID（对应 Luban 配表 TbBuff 的 id，如 9000=纯无敌, 9001=极限闪避）")]
        public int buffId = 9001;

        [Tooltip("生命周期管理模式")]
        public BuffClipLifetimeMode lifetimeMode = BuffClipLifetimeMode.ManageByClip;

        public override ClipBase Clone()
        {
            return new BuffClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                buffId = this.buffId,
                lifetimeMode = this.lifetimeMode
            };
        }
    }
}
