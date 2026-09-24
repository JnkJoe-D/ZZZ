using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(SpawnTrack), "生成")]
    public class SpawnClip : ClipBase
    {
        [Header("Spawn Settings")]
        [ActionProperty("预制体")]
        public GameObject prefab;

        [ActionAssetReference("prefab")]
        public ActionAssetReference prefabRef = new ActionAssetReference();

        [ActionProperty("中断时销毁 (被动打断)")]
        public bool destroyOnInterrupt = false;

        [ActionProperty("事件标签 (透传给投射物)")]
        public string eventTag = "Spawn_Default";

        [ActionProperty("目标标签")]
        public string[] targetTags = new string[0]; // 例如: ["Enemy", "Heal"]

        [Header("Transform Config")]
        [ActionProperty("生成绑定点")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [ActionProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [ActionProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [ActionProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        [ActionProperty("出生后是否脱离父节点")]
        public bool detach = true;

        public SpawnClip()
        {
            clipName = "Spawn Clip";
            duration = 0.1f;
        }

        public override ClipBase Clone()
        {
            return new SpawnClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,

                prefab = this.prefab,
                prefabRef = new ActionAssetReference(this.prefabRef.guid, this.prefabRef.assetName, this.prefabRef.assetPath),
                destroyOnInterrupt = this.destroyOnInterrupt,
                eventTag = this.eventTag,
                targetTags = (this.targetTags != null) ? (string[])this.targetTags.Clone() : new string[0],

                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                detach = this.detach
            };
        }
    }
}
