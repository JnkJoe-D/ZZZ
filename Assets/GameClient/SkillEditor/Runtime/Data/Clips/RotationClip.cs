using System;
using UnityEngine;

namespace SkillEditor
{
    public enum RotationReference
    {
        Input,                      // 世界坐标方向
        InputWithCamera,            // 和当前移动状态里的转向方式一样
        Target,                     // 没目标则不转
        TargetThenInput,           
        TargetThenInputWithCamera
    }

    public enum RotationMode
    {
        Interpolated,               // 插值
        Immediate                  // 立即
    }

    public enum UpdateFrequency
    {
        OnceAtEnter,                // 进入时执行一次
        Continuous                 // 连续更新
    }

    [Serializable]
    [ClipDefinition(typeof(TransformTrack), "旋转片段")]
    public class RotationClip : ClipBase
    {
        [SkillProperty("参考方向")]
        public RotationReference referenceDirection = RotationReference.Input;

        [SkillProperty("旋转方式")]
        public RotationMode rotationMode = RotationMode.Interpolated;

        [SkillProperty("更新频率")]
        public UpdateFrequency updateFrequency = UpdateFrequency.Continuous;

        public RotationClip()
        {
            clipName = "Rotation Clip";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new RotationClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                referenceDirection = this.referenceDirection,
                rotationMode = this.rotationMode,
                updateFrequency = this.updateFrequency
            };
        }
    }
}
