using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(RouteWindowTrack), "连招窗口")]
    public class RouteWindowClip : ClipBase
    {
        [Header("路由窗口")]
        [SkillProperty("窗口标签")]
        [Tooltip("用于标识连招窗口的标签，便于在技能逻辑中进行匹配和触发")]
        public string comboTag = "";

        public override float Duration
        {
            get => duration;
            set => duration = value;
        }

        public RouteWindowClip()
        {
            clipName = "Route Window";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new RouteWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                comboTag = comboTag
            };
        }
    }
}
