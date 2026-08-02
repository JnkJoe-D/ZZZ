using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 单次检测的独立配置。每次碰撞检测成功后，按此配置决定打击模式和命中效果。
    /// HitClip 持有 DetectConfig[]，支持按检测次数差异化配置。
    /// </summary>
    [Serializable]
    public class DetectConfig
    {
        // ── 打击模式 ──
        [SkillProperty("打击模式")]
        public HitMode hitMode = HitMode.Once;

        [SkillProperty("多段次数")]
        [ShowIf("hitMode", HitMode.Times)]
        public int multiHitCount = 3;

        [SkillProperty("多段总时长")]
        [ShowIf("hitMode", HitMode.Times)]
        public float multiHitDuration = 0.2f;

        // ── 打击反馈 ──
        [SkillProperty("启用顿帧")]
        public bool enableHitStop = false;

        [SkillProperty("顿帧时长(秒)")]
        [ShowIf("enableHitStop", true)]
        public float hitStopDuration = 0.05f;

        [SkillProperty("顿帧倍率")]
        [ShowIf("enableHitStop", true)]
        public float hitStopScale = 0f;

        [SkillProperty("受击硬直时长(秒)")]
        public float hitStunDuration = 0.3f;

        // ── 受击特效 ──
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

        // ── 受击音效 ──
        [SkillProperty("受击音效")]
        public AudioClip hitAudioClip;

        [SkillAssetReference("hitAudioClip")][HideInInspector]
        public SkillAssetReference hitAudioRef = new SkillAssetReference();

        // ── 命中效果 ──
        [SkillProperty("命中效果 ID")]
        public int hitEffectId = 0;

        public DetectConfig Clone()
        {
            return new DetectConfig
            {
                hitMode = this.hitMode,
                multiHitCount = this.multiHitCount,
                multiHitDuration = this.multiHitDuration,
                enableHitStop = this.enableHitStop,
                hitStopDuration = this.hitStopDuration,
                hitStopScale = this.hitStopScale,
                hitStunDuration = this.hitStunDuration,
                hitVFXPrefab = this.hitVFXPrefab,
                hitVFXRef = new SkillAssetReference(this.hitVFXRef.guid, this.hitVFXRef.assetName, this.hitVFXRef.assetPath),
                hitVFXHeight = this.hitVFXHeight,
                hitVFXPreviewOffsetXZ = this.hitVFXPreviewOffsetXZ,
                hitVFXScale = this.hitVFXScale,
                followTarget = this.followTarget,
                hitAudioClip = this.hitAudioClip,
                hitAudioRef = new SkillAssetReference(this.hitAudioRef.guid, this.hitAudioRef.assetName, this.hitAudioRef.assetPath),
                hitEffectId = this.hitEffectId
            };
        }
    }
}
