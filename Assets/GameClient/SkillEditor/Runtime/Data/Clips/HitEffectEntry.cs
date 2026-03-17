using System;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 单条命中效果条目。定义"影响谁"（targetTags）和"施加什么效果"（eventTag）。
    /// HitClip 中持有该类的数组，一条 HitClip 可以对不同目标施加不同效果。
    /// </summary>
    [Serializable]
    public class HitEffectEntry
    {
        [SkillProperty("效果标签")]
        public string eventTag = "Hit_Default";

        [SkillProperty("目标标签")]
        public string[] targetTags = new string[0];

        public HitEffectEntry Clone()
        {
            return new HitEffectEntry
            {
                eventTag = this.eventTag,
                targetTags = this.targetTags != null ? (string[])this.targetTags.Clone() : new string[0]
            };
        }
    }
}
