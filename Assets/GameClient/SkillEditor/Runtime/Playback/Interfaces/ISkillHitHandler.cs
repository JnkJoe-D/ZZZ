using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能伤害检测接口
    /// 战斗系统需实现此接口，用于接收 SkillEditor 的空间检测结果
    /// </summary>
    public interface ISkillHitHandler
    {
        void OnHitDetect(HitData hitData);
    }
    public struct HitData
    {
        public GameObject deployer;
        public Collider[] targetsCollilders;
        public HitEffectEntry[] hitEffects;
        // 打击反馈参数（来自 HitClip）
        public bool enableHitStop;
        public float hitStopDuration;
        public GameObject hitVFXPrefab;
        public float hitVFXHeight;
        public AudioClip hitAudioClip;
        public float hitStunDuration;
        public Vector3 hitVFXScale;
        public bool followTarget;
    }
}
