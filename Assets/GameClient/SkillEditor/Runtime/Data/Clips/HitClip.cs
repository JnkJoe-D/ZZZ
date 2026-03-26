using System;
using UnityEngine;

namespace SkillEditor
{
    [Serializable]
    [ClipDefinition(typeof(HitTrack), "打击")]
    public class HitClip : ClipBase, ISerializationCallbackReceiver
    {
        [SkillProperty("命中效果列表")]
        public HitEffectEntry[] hitEffects = new HitEffectEntry[0];

        [SkillProperty("检测频率")]
        public Frequency detectFrequency = Frequency.Once;

        [SkillProperty("检测次数")][ShowIf("detectFrequency", Frequency.Times)]
        public int times = 1;
        [SkillProperty("最大命中数 (0为不限)")]
        public int maxHitTargets = 0;

        [SkillProperty("选择策略")]
        public TargetSortMode targetSortMode = TargetSortMode.Closest;
 
        [SkillProperty("碰撞检测层级 (LayerMask)")]
        public LayerMask hitLayerMask = -1; // 默认 everything

        [SerializeField, HideInInspector]
        private int serializedHitLayerMask = -1;
        [SkillProperty("是否影响自身")]
        public bool isSelfImpacted = false;
        
        // --- 检测盒 ---
        [SkillProperty("检测盒")]
        public HitBoxShape shape = new HitBoxShape();
        // --- 编辑器辅助 ---
        [NonSerialized]
        [SkillProperty("检测盒Gizmos")]
        public bool showHitBoxGizmos = false;

        [SkillProperty("检测盒是否跟随绑定点")]
        public bool isHitBoxFollowBindPoint = true;

        [SkillProperty("检测盒绑定点")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [SkillProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [SkillProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        // --- 受击 ---
        [SkillProperty("受击方式")]
        public HitMode hitMode = HitMode.Once;

        [SkillProperty("受击次数")]
        [ShowIf("hitMode", HitMode.Times)]
        public int multiHitCount = 3;

        [SkillProperty("多段受击总时长")]
        [ShowIf("hitMode", HitMode.Times)]
        public float multiHitDuration = 0.2f;

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
                detectFrequency = this.detectFrequency,
                times = this.times,
                maxHitTargets = this.maxHitTargets,
                targetSortMode = this.targetSortMode,
                hitMode = this.hitMode,
                multiHitCount = this.multiHitCount,
                multiHitDuration = this.multiHitDuration,
                hitLayerMask = this.hitLayerMask,

                shape = this.shape.Clone(),

                isHitBoxFollowBindPoint = this.isHitBoxFollowBindPoint,
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
