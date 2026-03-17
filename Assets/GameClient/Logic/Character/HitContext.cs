using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// GameClient 层的命中上下文。
    /// 在 SkillHitHandler 中由 HitData 包装而来，包含更丰富的业务信息。
    /// </summary>
    public struct HitContext
    {
        // --- 核心实体 ---
        public CharacterEntity attacker;
        public CharacterEntity victim;

        // --- 命中效果列表（源自 HitClip → HitData） ---
        public SkillEditor.HitEffectEntry[] hitEffects;

        // --- 打击反馈参数（源自 HitClip → HitData） ---
        public bool enableHitStop;
        public float hitStopDuration;
        public GameObject hitVFXPrefab;
        public float hitVFXHeight;
        public Vector3 hitVFXScale;
        public bool hitVFXFollowTarget;
        public AudioClip hitAudioClip;
        public float hitStunDuration;

        // --- GameClient 层计算 ---
        public Vector3 hitPoint;       // 近似碰撞点
        public Vector3 hitDirection;   // 攻击方向 (attacker → victim)
    }
}
