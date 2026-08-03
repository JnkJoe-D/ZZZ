using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(HitTrack), "打击")]
    public class HitClip : ClipBase, ISerializationCallbackReceiver
    {
        // ── 检测策略 ──
        [SkillProperty("检测频率")]
        public Frequency detectFrequency = Frequency.Once;

        [SkillProperty("检测次数")][ShowIf("detectFrequency", Frequency.Times)]
        public int times = 1;

        [SkillProperty("最大命中数")]
        public int maxHitTargets = 0;

        [SkillProperty("选择策略")]
        public TargetSortMode targetSortMode = TargetSortMode.Closest;

        [SkillProperty("受击方向模式")]
        public HitDirectionMode hitDirectionMode = HitDirectionMode.AttackerToTarget;

        [SkillProperty("相对受击方向")]
        [ShowIf("hitDirectionMode", HitDirectionMode.OnEnterCustomRelative)]
        public Vector2 customHitDirection = new Vector2(0, 1);
 
        [SkillProperty("碰撞检测层级")]
        public LayerMask hitLayerMask = -1;

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

        [SkillProperty("检测盒跟随模式")]
        public HitBoxFollowMode hitBoxFollowMode = HitBoxFollowMode.PositionOnly;

        [SkillProperty("检测盒绑定点")]
        public BindPoint bindPoint = BindPoint.LogicRoot;

        [SkillProperty("自定义骨骼名称")]
        public string customBoneName = "";

        [SkillProperty("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;

        [SkillProperty("旋转偏移")]
        public Vector3 rotationOffset = Vector3.zero;

        // ── ★ 核心：嵌套的检测配置列表 ──
        [SkillProperty("检测配置")]
        public DetectConfig[] detects = new DetectConfig[] { new DetectConfig() };

        // --- 编辑器辅助 ---
        public enum HitVFXHandleType { None, Position, Scale }
        
        [NonSerialized][HideInInspector]
        public HitVFXHandleType activeVFXHandleType = HitVFXHandleType.None;

        /// <summary>当前在编辑器中选中的 DetectConfig 索引（用于 SceneGUI 预览）</summary>
        [NonSerialized][HideInInspector]
        public int selectedDetectIndex = 0;

        public HitClip()
        {
            clipName = "Damage Clip";
            duration = 0.5f;
        }

        /// <summary>获取当前选中的 DetectConfig（安全访问）</summary>
        public DetectConfig SelectedDetect
        {
            get
            {
                if (detects == null || detects.Length == 0) return null;
                int idx = Mathf.Clamp(selectedDetectIndex, 0, detects.Length - 1);
                return detects[idx];
            }
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
                
                detectFrequency = this.detectFrequency,
                times = this.times,
                maxHitTargets = this.maxHitTargets,
                targetSortMode = this.targetSortMode,
                hitDirectionMode = this.hitDirectionMode,
                customHitDirection = this.customHitDirection,
                hitLayerMask = this.hitLayerMask,

                shape = this.shape.Clone(),

                hitBoxFollowMode = this.hitBoxFollowMode,
                bindPoint = this.bindPoint,
                customBoneName = this.customBoneName,
                positionOffset = this.positionOffset,
                rotationOffset = this.rotationOffset,

                detects = CloneDetects(this.detects),

                activeVFXHandleType = this.activeVFXHandleType,
                showHitBoxGizmos = this.showHitBoxGizmos
            };
        }

        private static DetectConfig[] CloneDetects(DetectConfig[] source)
        {
            if (source == null || source.Length == 0) return new DetectConfig[] { new DetectConfig() };
            var result = new DetectConfig[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i]?.Clone() ?? new DetectConfig();
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
