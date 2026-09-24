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
        [ActionProperty("打击模式")]
        public HitMode hitMode = HitMode.Once;

        [ActionProperty("多段次数")]
        [ATShowIf("hitMode", HitMode.Times)]
        public int multiHitCount = 3;

        [ActionProperty("多段总时长")]
        [ATShowIf("hitMode", HitMode.Times)]
        public float multiHitDuration = 0.2f;

        // ── 打击反馈 ──
        [ActionProperty("启用顿帧")]
        public bool enableHitStop = false;

        [ActionProperty("顿帧时长(秒)")]
        [ATShowIf("enableHitStop", true)]
        public float hitStopDuration = 0.05f;

        [ActionProperty("顿帧倍率")]
        [ATShowIf("enableHitStop", true)]
        public float hitStopScale = 0f;

        [ActionProperty("受击硬直时长(秒)")]
        public float hitStunDuration = 0.3f;

        // ── 受击特效 ──
        [ActionProperty("受击特效")]
        public GameObject hitVFXPrefab;

        [ActionAssetReference("hitVFXPrefab")][HideInInspector]
        public ActionAssetReference hitVFXRef = new ActionAssetReference();

        [ActionProperty("受击特效高度")]
        public float hitVFXHeight = 1.0f;

        [ActionProperty("受击特效预览偏移")]
        public Vector2 hitVFXPreviewOffsetXZ = Vector2.zero;

        [ActionProperty("受击特效缩放")]
        public Vector3 hitVFXScale = Vector3.one;

        [ActionProperty("受击特效是否跟随目标")]
        public bool followTarget = true;

        // ── 受击音效 ──
        [ActionProperty("受击音效")]
        public UnityEngine.AudioClip hitAudioClip;

        [ActionAssetReference("hitAudioClip")][HideInInspector]
        public ActionAssetReference hitAudioRef = new ActionAssetReference();

        // ── 命中效果 ──
        [ActionProperty("命中效果 ID")]
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
                hitVFXRef = new ActionAssetReference(this.hitVFXRef.guid, this.hitVFXRef.assetName, this.hitVFXRef.assetPath),
                hitVFXHeight = this.hitVFXHeight,
                hitVFXPreviewOffsetXZ = this.hitVFXPreviewOffsetXZ,
                hitVFXScale = this.hitVFXScale,
                followTarget = this.followTarget,
                hitAudioClip = this.hitAudioClip,
                hitAudioRef = new ActionAssetReference(this.hitAudioRef.guid, this.hitAudioRef.assetName, this.hitAudioRef.assetPath),
                hitEffectId = this.hitEffectId,
            };
        }
    }
}
