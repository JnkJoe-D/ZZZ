using UnityEngine;

namespace ATEditor
{
    public enum HitMode { Once, Times }

    /// <summary>
    /// 技能伤害检测接口
    /// 战斗系统需实现此接口，用于接收 SkillEditor 的空间检测结果
    /// </summary>
    public interface IHitHandler
    {
        void OnHitDetect(HitData hitData);
    }

    public struct HitData
    {
        public GameObject deployer;
        public Vector3 hitBoxCenter;
        public Collider[] targetsCollilders;

        // ★ 新：命中效果配置 ID（查表用）
        public int hitEffectId;

        // 受击方向控制
        public HitDirectionMode hitDirectionMode;
        public Vector2 customHitDirection;
        public Vector3 customWorldDirection;

        // 命中模式
        public HitMode hitMode;
        public int multiHitCount;
        public float multiHitDuration;

        // 打击反馈参数（来自 DetectConfig）
        public bool enableHitStop;
        public float hitStopDuration;
        public float hitStopScale;
        public GameObject hitVFXPrefab;
        public float hitVFXHeight;
        public UnityEngine.AudioClip hitAudioClip;
        public float hitStunDuration;
        public Vector3 hitVFXScale;
        public bool followTarget;
    }
}
