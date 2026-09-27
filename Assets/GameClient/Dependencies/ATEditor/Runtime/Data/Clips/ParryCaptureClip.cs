using System;
using UnityEngine;

namespace ATEditor
{
    public enum ParryCaptureTriggerTiming
    {
        Instant = 10,
        OnExit = 20
    }
    [Serializable]
    public class ParryCaptureData
    {
        [SerializeField]
        [ActionProperty("触发时机")]
        public ParryCaptureTriggerTiming triggerTiming = ParryCaptureTriggerTiming.Instant;

        public ParryCaptureData Clone()
        {
            return new ParryCaptureData
            {
                triggerTiming = this.triggerTiming
            };
        }
    }

    [Serializable]
    [ClipDefinition(typeof(EventTrack), "招架捕获窗口")]
    public class ParryCaptureClip : ClipBase
    {
        [SerializeField]
        [ActionProperty("捕获数据")]
        public ParryCaptureData data = new ParryCaptureData();

        public override ClipBase Clone()
        {
            return new ParryCaptureClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                data = this.data?.Clone()
            };
        }
    }
}
