using System;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 曲线采样模式
    /// </summary>
    public enum CurveSampleMode
    {
        /// <summary>
        /// 归一化采样：曲线 X 轴 0~1，按 Clip 时长比例映射
        /// </summary>
        NormalizedTime,

        /// <summary>
        /// 绝对时间采样：曲线 X 轴 = 真实秒数
        /// </summary>
        AbsoluteTime
    }

    /// <summary>
    /// 相机轨道片段数据
    /// 通过 AnimationCurve 驱动 Cinemachine Dolly Camera 的 PathPosition
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "轨道相机")]
    public class CameraDollyClip : ClipBase
    {
        [SkillProperty("相机编号")]
        public int cameraId = 0;

        [SkillProperty("路径曲线")]
        public AnimationCurve pathCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SkillProperty("采样模式")]
        public CurveSampleMode sampleMode = CurveSampleMode.NormalizedTime;

        public CameraDollyClip()
        {
            clipName = "Camera Clip";
            duration = 2.0f;
        }

        public override ClipBase Clone()
        {
            return new CameraDollyClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,

                cameraId = this.cameraId,
                pathCurve = new AnimationCurve(this.pathCurve.keys),
                sampleMode = this.sampleMode
            };
        }
    }
}
