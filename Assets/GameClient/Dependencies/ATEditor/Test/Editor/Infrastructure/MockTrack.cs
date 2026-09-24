using System;
using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>
    /// 测试专用 TrackBase 派生类
    /// 允许无限制重叠排布片段，方便自由构造各类时序测试用例
    /// </summary>
    [Serializable]
    public class MockTrack : TrackBase
    {
        public override bool CanOverlap => true;

        public MockTrack(string name = "MockTrack")
        {
            trackName = name;
            trackType = nameof(MockTrack);
            isEnabled = true;
        }

        public override TrackBase Clone()
        {
            var clone = new MockTrack(trackName);
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
