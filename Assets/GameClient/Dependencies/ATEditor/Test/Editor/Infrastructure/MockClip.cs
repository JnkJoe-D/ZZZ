using System;
using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>
    /// 测试专用 ClipBase 派生类
    /// 支持直接设定任意时间区间（包括 0 时长与反向区间），绕过 Application.isPlaying 限制
    /// </summary>
    [Serializable]
    public class MockClip : ClipBase
    {
        public string CustomTag = "";

        public MockClip()
        {
            clipName = "MockClip";
            startTime = 0f;
            duration = 1.0f;
        }

        public MockClip(float start, float dur, string name = "MockClip")
        {
            clipName = name;
            SetTimeRange(start, dur);
        }

        /// <summary>
        /// 显式设定时间范围，支持 0 时长（瞬态片段）
        /// </summary>
        public void SetTimeRange(float start, float dur)
        {
            this.startTime = Mathf.Max(0f, start);
            this.duration = Mathf.Max(0f, dur);
        }

        public override ClipBase Clone()
        {
            var clone = new MockClip();
            clone.clipName = clipName;
            clone.startTime = startTime;
            clone.duration = duration;
            clone.CustomTag = CustomTag;
            clone.isEnabled = isEnabled;
            return clone;
        }
    }
}
