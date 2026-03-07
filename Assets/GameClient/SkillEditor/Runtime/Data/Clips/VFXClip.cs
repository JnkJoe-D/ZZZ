using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class VFXClip : ClipBase
    {
        [Header("资源")]
        [SkillProperty("特效预制体")]
        public GameObject effectPrefab;
        
        [SerializeField][HideInInspector]
        public string prefabGuid;
        [SerializeField][HideInInspector]
        public string prefabAssetName;
        [SerializeField][HideInInspector]
        public string prefabAssetPath;

        [Header("挂点设置")]
        [SkillProperty("挂载位置")]
        public BindPoint bindPoint = BindPoint.Root;

        [SkillProperty("自定义骨骼名")]
        public string customBoneName;

        [SkillProperty("跟随挂点")]
        public bool followTarget = true;

        [Header("偏移调整")]
        [SkillProperty("位置偏移")]
        public Vector3 positionOffset;
        
        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset;

        [SkillProperty("缩放比例")]
        public Vector3 scale = Vector3.one;

        [Header("生命周期")]
        [SkillProperty("跟随片段结束")]
        public bool destroyOnEnd = true;

        [SkillProperty("结束时停止发射")]
        public bool stopEmissionOnEnd = false;

        public VFXClip()
        {
            clipName = "VFX Clip";
            duration = 1.0f;
            scale = Vector3.one;
            bindPoint = BindPoint.Root;
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
                prefabGuid = this.prefabGuid,
                prefabAssetName = this.prefabAssetName,
                prefabAssetPath = this.prefabAssetPath,
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                followTarget = this.followTarget,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                scale = this.scale,
                destroyOnEnd = this.destroyOnEnd,
                stopEmissionOnEnd = this.stopEmissionOnEnd
            };
        }
    }
}
