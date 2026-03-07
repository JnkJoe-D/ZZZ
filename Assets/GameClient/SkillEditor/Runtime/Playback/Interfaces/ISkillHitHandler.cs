using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能伤害检测接口
    /// 战斗系统需实现此接口，用于接收 SkillEditor 的空间检测结果
    /// </summary>
    public interface ISkillHitHandler
    {
        /// <summary>
        /// 技能触发了区域伤害检测
        /// </summary>
        /// <param name="targets">命中的碰撞体数组</param>
        /// <param name="eventTag">配置在 Timeline 上的技能标识（用于查表）</param>
        /// <param name="clipData">触发检测的 Clip 完整数据，可用于获取更多上下文</param>
        /// <param name="actionTags">目标筛选，执行不同的技能效果</param>
        void OnHitDetect(HitData hitData);
    }
    public struct HitData
    {
        public GameObject deployer;
        public Collider[] targets;
        public string eventTag;
        public string[] actionTags;
    }
}
