using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 全屏十字闪光参数配置结构体 (纯原生类型，支持 Inspector 原生序列化)
    /// </summary>
    [Serializable]
    public struct CrossFlashParameters
    {
        [Header("基础视觉 (Visual)")]
        [Tooltip("十字闪光 HDR 基础颜色")]
        public Color FlashColor;

        [Tooltip("HDR 亮度增益强度 (配合 Bloom)")]
        [Range(1f, 30f)]
        public float Intensity;

        [Tooltip("闪光固定持续播放时长 (秒)")]
        [Range(0.05f, 2.0f)]
        public float Duration;

        [Header("动态线宽与几何形态 (Geometry)")]
        [Tooltip("中心基准半线宽 (屏幕视口比例)")]
        [Range(0.0005f, 0.01f)]
        public float BaseLineWidth;

        [Tooltip("中心端最小线宽下限 (截断极细收窄)")]
        [Range(0.0002f, 0.005f)]
        public float MinLineWidth;

        [Tooltip("远端最大线宽上限 (截断最粗展开)")]
        [Range(0.002f, 0.05f)]
        public float MaxLineWidth;

        [Tooltip("线宽随距离扩散系数 (越大向边缘展开越快)")]
        [Range(0.001f, 0.1f)]
        public float WidthScaleRate;

        [Tooltip("边缘羽化柔化系数")]
        [Range(0.0005f, 0.01f)]
        public float LineSoftness;

        [Header("衰减与空间特性 (Spatial & Decay)")]
        [Tooltip("近端淡入衰减半径 (消除中心四角像素挤压，<=0 表示不衰减)")]
        [Range(0f, 0.1f)]
        public float NearFadeDistance;

        [Tooltip("是否实时跟随绑点移动 (false 为起手触发瞬间定格在世界坐标)")]
        public bool FollowTarget;

        [Tooltip("是否随时间轴全局流速缩放 (受顿帧/慢动作影响)")]
        public bool InheritEntityTimeScale;

        public static CrossFlashParameters Default => new CrossFlashParameters
        {
            FlashColor = new Color(1.0f, 0.65f, 0.05f, 1.0f),
            Intensity = 8.0f,
            Duration = 0.25f,
            BaseLineWidth = 0.002f,
            MinLineWidth = 0.0008f,
            MaxLineWidth = 0.012f,
            WidthScaleRate = 0.02f,
            LineSoftness = 0.0015f,
            NearFadeDistance = 0.02f,
            FollowTarget = false,
            InheritEntityTimeScale = true
        };
    }

    /// <summary>
    /// 全屏十字攻击预警闪光片段，隶属于 VFX 轨道
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(VFXTrack), "十字预警闪光")]
    public class CrossFlashClip : ClipBase
    {
        [Header("挂点绑定")]
        [ActionProperty("挂载位置")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [ActionProperty("自定义骨骼名")]
        public string customBoneName;

        [Header("偏移调整")]
        [ActionProperty("位置偏移")]
        public Vector3 positionOffset;

        [Header("效果参数")]
        public CrossFlashParameters parameters = CrossFlashParameters.Default;

        public CrossFlashClip()
        {
            clipName = "Cross Flash";
            Duration = 0.25f;
        }

        public override ClipBase Clone()
        {
            return new CrossFlashClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                parameters = this.parameters
            };
        }
    }
}
