using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(CombatTrack), "生成物")]
    public class SpawnClip : ClipBase
    {
        [Header("资源设置")]
        [ActionProperty("生成预制体")]
        public GameObject prefab;

        [ActionAssetReference("prefab")]
        public ActionAssetReference prefabRef = new ActionAssetReference();

        [ActionProperty("事件透传标识")]
        public string eventTag = "Spawn_Default";

        [ActionProperty("目标标签")]
        public string[] targetTags = new string[0];

        [Header("生成与挂载设置")]
        [ActionProperty("挂载设置")]
        public TransformBindConfig bindConfig = new TransformBindConfig();

        [Header("生命周期设置")]
        [ActionProperty("生命周期")]
        public LifecycleConfig lifecycleConfig = new LifecycleConfig();

        [Header("移动与轨迹设置")]
        [ActionProperty("移动设置")]
        public ProjectileMovementConfig movementConfig = new ProjectileMovementConfig();

        [Header("攻击检测设置 (可选)")]
        [ActionProperty("启用攻击检测")]
        public bool enableAttackDetection = false;

        [ActionProperty("检测盒范围")]
        [ATShowIf("enableAttackDetection", true)]
        public HitBoxScopeConfig hitBoxScope = new HitBoxScopeConfig();

        [ActionProperty("攻击检测策略")]
        [ATShowIf("enableAttackDetection", true)]
        public AttackDetectionPolicy attackPolicy = new AttackDetectionPolicy();

        public SpawnClip()
        {
            clipName = "Spawn Clip";
            duration = 0.5f;
            bindConfig.followTarget = false; // 生成物默认脱离挂点自由运动
            bindConfig.followMode = HitBoxFollowMode.None;
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
                eventTag = this.eventTag,
                targetTags = (this.targetTags != null) ? (string[])this.targetTags.Clone() : new string[0],

                bindConfig = this.bindConfig?.Clone() ?? new TransformBindConfig(),
                lifecycleConfig = this.lifecycleConfig?.Clone() ?? new LifecycleConfig(),
                movementConfig = this.movementConfig?.Clone() ?? new ProjectileMovementConfig(),

                enableAttackDetection = this.enableAttackDetection,
                hitBoxScope = this.hitBoxScope?.Clone() ?? new HitBoxScopeConfig(),
                attackPolicy = this.attackPolicy?.Clone() ?? new AttackDetectionPolicy()
            };
        }
    }
}
