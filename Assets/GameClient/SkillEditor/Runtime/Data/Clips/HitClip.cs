using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class HitClip : ClipBase, ISerializationCallbackReceiver
    {
        [Header("Detection Strategy")]
        [SkillProperty("事件标签 (EventTag)")]
        public string eventTag = "Hit_Default";

        [SkillProperty("目标标签")]
        public string[] targetTags = new string[0]; 

        [SkillProperty("命中频率")]
        public HitFrequency hitFrequency = HitFrequency.Once;

        [SkillProperty("检测间隔(秒)")]
        public float checkInterval = 0.5f;

        [SkillProperty("最大命中数 (0为不限)")]
        public int maxHitTargets = 0;

        [SkillProperty("选择策略")]
        public TargetSortMode targetSortMode = TargetSortMode.Closest;

        [Header("Physics Config")]
        [SkillProperty("碰撞检测层级 (LayerMask)")]
        public LayerMask hitLayerMask = -1; // 默认 everything

        [SerializeField, HideInInspector]
        private int serializedHitLayerMask = -1;
        [SkillProperty("是否影响自身")]
        public bool isSelfImpacted = false;

        [Header("Shape Config")]
        public HitBoxShape shape = new HitBoxShape();

        [Header("Transform Config")]
        [SkillProperty("绑定点")]
        public BindPoint bindPoint = BindPoint.Root;

        [SkillProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [SkillProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        public HitClip()
        {
            clipName = "Damage Clip";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new HitClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                
                eventTag = this.eventTag,
                targetTags = (this.targetTags != null) ? (string[])this.targetTags.Clone() : new string[0],
                hitFrequency = this.hitFrequency,
                checkInterval = this.checkInterval,
                maxHitTargets = this.maxHitTargets,
                targetSortMode = this.targetSortMode,
                hitLayerMask = this.hitLayerMask,

                shape = this.shape.Clone(),

                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset
            };
        }

        public void OnBeforeSerialize()
        {
            serializedHitLayerMask = hitLayerMask.value;
        }

        public void OnAfterDeserialize()
        {
            hitLayerMask.value = serializedHitLayerMask;
        }
    }
}
