using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    public class HitClip : ClipBase, ISerializationCallbackReceiver
    {
        [Header("Detection Strategy")]
        [SkillProperty("命中效果列表")]
        public HitEffectEntry[] hitEffects = new HitEffectEntry[0];

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
        // --- 编辑器辅助 ---
        [NonSerialized]
        [SkillProperty("检测盒Gizmos")]
        public bool showHitBoxGizmos = false;

        [Header("检测盒")]
        public HitBoxShape shape = new HitBoxShape();

        [SkillProperty("检测盒绑定点")]
        public BindPoint bindPoint = BindPoint.Root;

        [SkillProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [SkillProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        [Header("打击反馈")]
        [SkillProperty("启用顿帧")]
        public bool enableHitStop = false;

        [SkillProperty("顿帧时长(秒)")]
        public float hitStopDuration = 0.05f;

        [SkillProperty("受击特效")]
        public GameObject hitVFXPrefab;

        [SkillAssetReference("hitVFXPrefab")][HideInInspector]
        public SkillAssetReference hitVFXRef = new SkillAssetReference();

        [SkillProperty("受击特效高度 (Y)")]
        public float hitVFXHeight = 1.0f;

        [SkillProperty("受击特效预览偏移 (XZ)")]
        public Vector2 hitVFXPreviewOffsetXZ = Vector2.zero;

        [SkillProperty("受击特效缩放")]
        public Vector3 hitVFXScale = Vector3.one;

        [SkillProperty("受击特效是否跟随目标")]
        public bool followTarget = true;

        [SkillProperty("受击音效")]
        public AudioClip hitAudioClip;

        [SkillAssetReference("hitAudioClip")][HideInInspector]
        public SkillAssetReference hitAudioRef = new SkillAssetReference();

        [SkillProperty("受击硬直时长(秒)")]
        public float hitStunDuration = 0.3f;

        // --- 编辑器辅助 ---
        public enum HitVFXHandleType { None, Position, Scale }
        
        [NonSerialized][HideInInspector]
        public HitVFXHandleType activeVFXHandleType = HitVFXHandleType.None;

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
                
                hitEffects = CloneHitEffects(this.hitEffects),
                hitFrequency = this.hitFrequency,
                checkInterval = this.checkInterval,
                maxHitTargets = this.maxHitTargets,
                targetSortMode = this.targetSortMode,
                hitLayerMask = this.hitLayerMask,

                shape = this.shape.Clone(),

                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,
                enableHitStop = this.enableHitStop,
                hitStopDuration = this.hitStopDuration,
                hitVFXPrefab = this.hitVFXPrefab,
                hitVFXRef = new SkillAssetReference(this.hitVFXRef.guid, this.hitVFXRef.assetName, this.hitVFXRef.assetPath),
                hitVFXHeight = this.hitVFXHeight,
                hitVFXPreviewOffsetXZ = this.hitVFXPreviewOffsetXZ,
                hitVFXScale = this.hitVFXScale,
                followTarget = this.followTarget,
                hitAudioClip = this.hitAudioClip,
                hitAudioRef = new SkillAssetReference(this.hitAudioRef.guid, this.hitAudioRef.assetName, this.hitAudioRef.assetPath),
                hitStunDuration = this.hitStunDuration,
                activeVFXHandleType = this.activeVFXHandleType,
                showHitBoxGizmos = this.showHitBoxGizmos
            };
        }

        private static HitEffectEntry[] CloneHitEffects(HitEffectEntry[] source)
        {
            if (source == null || source.Length == 0) return new HitEffectEntry[0];
            var result = new HitEffectEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i]?.Clone() ?? new HitEffectEntry();
            }
            return result;
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
