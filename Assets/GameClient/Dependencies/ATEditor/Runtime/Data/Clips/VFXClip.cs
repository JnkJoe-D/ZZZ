using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(VFXTrack), "特效")]
    public class VFXClip : ClipBase
    {
        [Header("资源")]
        [ActionProperty("特效预制体")]
        public GameObject effectPrefab;
        
        [ActionAssetReference("effectPrefab")]
        public ActionAssetReference vfxRef = new ActionAssetReference();

        [Header("挂点设置")]
        [ActionProperty("挂载位置")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [ActionProperty("自定义骨骼名")]
        public string customBoneName;

        [ActionProperty("跟随挂点")]
        public bool followTarget = true;

        [Header("偏移调整")]
        [ActionProperty("位置偏移")]
        public Vector3 positionOffset;
        
        [ActionProperty("旋转偏移")]
        public Vector3 rotationOffset;

        [ActionProperty("缩放比例")]
        public Vector3 scale = Vector3.one;

        [Header("生命周期")]
        [ActionProperty("跟随片段结束")]
        public bool destroyOnEnd = true;

        [ActionProperty("结束时停止发射粒子")]
        public bool stopEmissionOnEnd = false;

        // --- 编辑器辅助 ---
        public enum VFXHandleType { None, Position, Rotation, Scale }
        [NonSerialized]
        [HideInInspector]
        public VFXHandleType activeHandleType = VFXHandleType.None;

        public VFXClip()
        {
            clipName = "VFX Clip";
            duration = 1.0f;
            scale = Vector3.one;
            bindPoint = BindPoint.LogicRoot;
            destroyOnEnd = true;
            followTarget = true;
        }

        public override ClipBase Clone()
        {
            return new VFXClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                effectPrefab = this.effectPrefab,
                vfxRef = new ActionAssetReference(this.vfxRef.guid, this.vfxRef.assetName, this.vfxRef.assetPath),
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                followTarget = this.followTarget,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                scale = this.scale,
                destroyOnEnd = this.destroyOnEnd,
                stopEmissionOnEnd = this.stopEmissionOnEnd,
                activeHandleType = this.activeHandleType
            };
        }
    }
}
