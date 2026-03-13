using System;
using UnityEngine;

namespace SkillEditor
{
    public enum ComboWindowType
    {
        None = 100,
        Buffer= 0,    // 只能预输入，不即时执行
        Execute=1,   // 既能即时执行，也可接收预定的预输入
        Fallback=2   // 后摇垃圾时间，打断重置起手
    }

    [Serializable]
    public class ComboWindowClip : ClipBase
    {
        [Header("Combo Window Settings")]
        [SkillProperty("窗口类型")]
        [Tooltip("Buffer:只接受预输入; Execute:允许立即派生; Fallback:不可派生且任何按键直接转基础连段")]
        public ComboWindowType windowType = ComboWindowType.Execute;

        [SkillProperty("派生窗口标签")]
        [Tooltip("输入一个Tag，比如 Normal 或 Delayed。当时间轴处于此Clip时，角色会获得该派生标签的鉴权。")]
        public string comboTag = "Normal";

        public override float Duration { get => duration; set => duration = value; }

        public ComboWindowClip()
        {
            clipName = "派生窗口";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new ComboWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                windowType = this.windowType,
                comboTag = this.comboTag
            };
        }
    }
}
