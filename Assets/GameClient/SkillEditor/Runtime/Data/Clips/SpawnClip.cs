using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    [ClipDefinition(typeof(SpawnTrack), "生成")]
    public class SpawnClip : ClipBase
    {
        [Header("Spawn Settings")]
        [SkillProperty("预制体")]
        public GameObject prefab;

        [SkillAssetReference("prefab")]
        public SkillAssetReference prefabRef = new SkillAssetReference();

        [SkillProperty("中断时销毁 (被动打断)")]
        public bool destroyOnInterrupt = false;

        [SkillProperty("事件标签 (透传给投射物)")]
        public string eventTag = "Spawn_Default";

        [SkillProperty("目标标签")]
        public string[] targetTags = new string[0]; // 例如: ["Enemy", "Heal"]

        [Header("Transform Config")]
        [SkillProperty("生成绑定点")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [SkillProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [SkillProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        [SkillProperty("出生后是否脱离父节点")]
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
                prefabRef = new SkillAssetReference(this.prefabRef.guid, this.prefabRef.assetName, this.prefabRef.assetPath),
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
