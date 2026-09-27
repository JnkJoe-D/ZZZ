using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 受控相机目标范围（目前两者对应同一个 camerabasepoint，未来支持分离控制）
    /// </summary>
    public enum CameraTargetScope
    {
        [InspectorName("同时控制 (Follow & LookAt)")]
        Both = 0,

        [InspectorName("仅跟随目标 (Follow Only)")]
        FollowOnly = 1,

        [InspectorName("仅注视目标 (LookAt Only)")]
        LookAtOnly = 2
    }

    /// <summary>
    /// 轴向过滤与位移运算的基准参考空间
    /// </summary>
    public enum CameraTargetSpace
    {
        [InspectorName("角色局部坐标 (推荐)")]
        CharacterLocal = 0,

        [InspectorName("世界绝对坐标")]
        World = 1
    }

    /// <summary>
    /// 相机目标跟随片段：在技能期间动态控制相机 Follow/LookAt 锚点（CameraBasePoint）
    /// 在指定轴向上跟随目标骨骼，并提供进出过渡曲线与平滑阻尼。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "相机目标跟随")]
    public class CameraTargetClip : ClipBase
    {
        [Header("受控相机目标")]
        [ActionProperty("作用目标")]
        public CameraTargetScope targetScope = CameraTargetScope.Both;

        #region 1. 骨骼跟随源
        [Header("骨骼跟随源")]
        [ActionProperty("目标骨骼")]
        public BindPoint bindPoint = BindPoint.Bip001_Head;

        [ActionProperty("自定义骨骼名")]
        public string customBoneName = "";

        [ActionProperty("参考空间")]
        public CameraTargetSpace referenceSpace = CameraTargetSpace.CharacterLocal;

        [ActionProperty("位置附加偏移")]
        public Vector3 positionOffset = Vector3.zero;
        #endregion

        #region 2. 轴向同步过滤 (Axis Constraints)
        [Header("同步轴向过滤")]
        public bool syncX = true;
        public bool syncY = true;
        public bool syncZ = true;
        #endregion

        public override bool SupportsBlending => true;

        #region 3. 进出过渡与曲线权重
        [Header("过渡与曲线配置")]
        [ActionProperty("进入过渡曲线")]
        public AnimationCurve blendInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [ActionProperty("退出过渡曲线")]
        public AnimationCurve blendOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        #endregion

        #region 4. 平滑跟随与阻尼 (防骨骼抖动抽搐)
        [Header("运动平滑阻尼")]
        [ActionProperty("启用平滑阻尼")]
        public bool enableDamping = true;

        [ActionProperty("平滑滞后时间 (s)")]
        [Range(0.01f, 0.5f)]
        public float smoothTime = 0.08f;
        #endregion

        #region 5. 退出与打断恢复策略
        [Header("恢复策略")]
        [ActionProperty("退出时恢复原样")]
        public bool restoreOnExit = true;

        [ActionProperty("打断时恢复原样")]
        public bool restoreOnStop = true;

        [ActionProperty("打断平滑回弹时间 (s)")]
        [Range(0.05f, 1f)]
        public float interruptRestoreDuration = 0.2f;
        #endregion

        public CameraTargetClip()
        {
            clipName = "相机目标跟随";
            duration = 1.0f;
            blendInDuration = 0.15f;
            blendOutDuration = 0.2f;
        }

        public override ClipBase Clone()
        {
            return new CameraTargetClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                blendInDuration = this.blendInDuration,
                blendOutDuration = this.blendOutDuration,
                targetScope = this.targetScope,
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                referenceSpace = this.referenceSpace,
                positionOffset = this.positionOffset,
                syncX = this.syncX,
                syncY = this.syncY,
                syncZ = this.syncZ,
                blendInCurve = this.blendInCurve != null ? new AnimationCurve(this.blendInCurve.keys) : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                blendOutCurve = this.blendOutCurve != null ? new AnimationCurve(this.blendOutCurve.keys) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
                enableDamping = this.enableDamping,
                smoothTime = this.smoothTime,
                restoreOnExit = this.restoreOnExit,
                restoreOnStop = this.restoreOnStop,
                interruptRestoreDuration = this.interruptRestoreDuration
            };
        }
    }
}
