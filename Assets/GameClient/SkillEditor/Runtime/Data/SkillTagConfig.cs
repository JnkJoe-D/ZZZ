using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    [CreateAssetMenu(fileName = "SkillTagConfig", menuName = "SkillEditor/TagConfig", order = 200)]
    public class SkillTagConfig : ScriptableObject
    {
        [Tooltip("配置技能系统中所有可用的目标标签")]
        public List<string> availableTargetTags = new List<string>()
        {
            "Enemy",
            "Ally",
            "Self",
            "Friendly",
            "NPC"
        };

        [Tooltip("配置技能系统中所有可用的效果标签")]
        public List<string> availableEventTags = new List<string>()
        {
            "Hit_Default",
            "Hit_Light",
            "Hit_Heavy",
            "Hit_Knockback",
            "Hit_Launch"
        };
    }
}
