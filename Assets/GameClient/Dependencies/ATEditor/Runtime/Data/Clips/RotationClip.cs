using System;
using UnityEngine;

namespace ATEditor
{
    public enum RotationReference
    {
        Input,                      // 世界坐标方向
        InputWithCamera,            // 基于相机的局部
        Target,                     // 没目标则不转
        TargetThenInput,           
        TargetThenInputWithCamera,
        Camera,                    // 相机方向
    }

    public enum RotationMode
    {
        Interpolated,               // 插值
        Immediate                  // 立即
    }

    public enum UpdateFrequency
    {
        OnceAtEnter,                
        OnceAtExit,
        Continuous                 
    }

    [Serializable]
    [ClipDefinition(typeof(TransformTrack), "旋转")]
    public class RotationClip : ClipBase
    {
        [ActionProperty("参考方向")]
        public RotationReference referenceDirection = RotationReference.Input;

        [ActionProperty("旋转方式")]
        public RotationMode rotationMode = RotationMode.Interpolated;

        [ActionProperty("更新频率")]
        public UpdateFrequency updateFrequency = UpdateFrequency.Continuous;

        [ActionProperty("本地旋转偏移")]
        public Vector3 localRotationOffset;

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
                updateFrequency = this.updateFrequency,
                localRotationOffset = this.localRotationOffset
            };
        }
    }
}
