using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(CombatTrack), "打击")]
    public class HitClip : ClipBase
    {
        // ── 检测盒与范围定义 ──
        [Header("检测盒与范围")]
        [ActionProperty("检测盒范围")]
        public HitBoxScopeConfig hitBoxScope = new HitBoxScopeConfig();

        // ── 打击判定与效果策略 ──
        [Header("判定规则与表现")]
        [ActionProperty("攻击检测策略")]
        public AttackDetectionPolicy attackPolicy = new AttackDetectionPolicy();

        // --- 便捷代理属性 (方便快速访问，保持代码精炼并支持双向读写) ---
        public HitBoxShape shape
        {
            get => hitBoxScope?.shape;
            set { if (hitBoxScope != null) hitBoxScope.shape = value; }
        }
        public BindPoint bindPoint
        {
            get => hitBoxScope != null ? hitBoxScope.bindPoint : BindPoint.LogicRoot;
            set { if (hitBoxScope != null) hitBoxScope.bindPoint = value; }
        }
        public string customBoneName
        {
            get => hitBoxScope != null ? hitBoxScope.customBoneName : "";
            set { if (hitBoxScope != null) hitBoxScope.customBoneName = value; }
        }
        public Vector3 positionOffset
        {
            get => hitBoxScope != null ? hitBoxScope.positionOffset : Vector3.zero;
            set { if (hitBoxScope != null) hitBoxScope.positionOffset = value; }
        }
        public Vector3 rotationOffset
        {
            get => hitBoxScope != null ? hitBoxScope.rotationOffset : Vector3.zero;
            set { if (hitBoxScope != null) hitBoxScope.rotationOffset = value; }
        }
        public HitBoxFollowMode hitBoxFollowMode
        {
            get => hitBoxScope != null ? hitBoxScope.hitBoxFollowMode : HitBoxFollowMode.PositionOnly;
            set { if (hitBoxScope != null) hitBoxScope.hitBoxFollowMode = value; }
        }
        public bool showHitBoxGizmos
        {
            get => hitBoxScope != null && hitBoxScope.showHitBoxGizmos;
            set { if (hitBoxScope != null) hitBoxScope.showHitBoxGizmos = value; }
        }

        public Frequency detectFrequency
        {
            get => attackPolicy != null ? attackPolicy.detectFrequency : Frequency.Once;
            set { if (attackPolicy != null) attackPolicy.detectFrequency = value; }
        }
        public int times
        {
            get => attackPolicy != null ? attackPolicy.times : 1;
            set { if (attackPolicy != null) attackPolicy.times = value; }
        }
        public int maxHitTargets
        {
            get => attackPolicy != null ? attackPolicy.maxHitTargets : 0;
            set { if (attackPolicy != null) attackPolicy.maxHitTargets = value; }
        }
        public TargetSortMode targetSortMode
        {
            get => attackPolicy != null ? attackPolicy.targetSortMode : TargetSortMode.Closest;
            set { if (attackPolicy != null) attackPolicy.targetSortMode = value; }
        }
        public HitDirectionMode hitDirectionMode
        {
            get => attackPolicy != null ? attackPolicy.hitDirectionMode : HitDirectionMode.AttackerToTarget;
            set { if (attackPolicy != null) attackPolicy.hitDirectionMode = value; }
        }
        public Vector2 customHitDirection
        {
            get => attackPolicy != null ? attackPolicy.customHitDirection : new Vector2(0, 1);
            set { if (attackPolicy != null) attackPolicy.customHitDirection = value; }
        }
        public LayerMask hitLayerMask
        {
            get => attackPolicy != null ? attackPolicy.hitLayerMask : (LayerMask)(-1);
            set { if (attackPolicy != null) attackPolicy.hitLayerMask = value; }
        }
        public bool isSelfImpacted
        {
            get => attackPolicy != null && attackPolicy.isSelfImpacted;
            set { if (attackPolicy != null) attackPolicy.isSelfImpacted = value; }
        }
        public DetectConfig[] detects
        {
            get => attackPolicy?.detects;
            set { if (attackPolicy != null) attackPolicy.detects = value; }
        }

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
                var d = attackPolicy?.detects;
                if (d == null || d.Length == 0) return null;
                int idx = Mathf.Clamp(selectedDetectIndex, 0, d.Length - 1);
                return d[idx];
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

                hitBoxScope = this.hitBoxScope?.Clone() ?? new HitBoxScopeConfig(),
                attackPolicy = this.attackPolicy?.Clone() ?? new AttackDetectionPolicy(),

                activeVFXHandleType = this.activeVFXHandleType,
                selectedDetectIndex = this.selectedDetectIndex
            };
        }
    }
}
