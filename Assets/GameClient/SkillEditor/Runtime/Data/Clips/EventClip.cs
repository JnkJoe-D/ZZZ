using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class SkillEventParam
    {
        [SkillProperty("参数名")]
        public string key = "";
        
        [SkillProperty("字符串值")]
        public string stringValue = "";
        
        [SkillProperty("浮点数值")]
        public float floatValue = 0f;
        
        [SkillProperty("整数值")]
        public int intValue = 0;
        
        public SkillEventParam Clone()
        {
            return new SkillEventParam
            {
                key = this.key,
                stringValue = this.stringValue,
                floatValue = this.floatValue,
                intValue = this.intValue
            };
        }
    }

    [Serializable]
    public class EventClip : ClipBase
    {
        [Header("Event Settings")]
        [SkillProperty("事件名")]
        public string eventName = "Event_Default";

        // 由于 SkillProperty 目前可能不支持复杂的 List 嵌套结构，
        // 这里暂时使用标准序列化。或者等后续实现自定义 Drawer。
        public List<SkillEventParam> parameters = new List<SkillEventParam>();
        public override float Duration { get => duration; set => duration=0.1f; }
        public EventClip()
        {
            clipName = "Event Clip";
            duration = 0.1f;
        }

        public override ClipBase Clone()
        {
            var clone = new EventClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                eventName = this.eventName
            };

            foreach (var p in this.parameters)
            {
                clone.parameters.Add(p.Clone());
            }

            return clone;
        }
    }
}
