using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    [CreateAssetMenu(fileName = "ActionTagConfigAsset", menuName = "Config/Action/Action Tag Config", order = 200)]
    public class ActionTagConfigAsset : GameConfigAsset
    {
        [Tooltip("Target tags used by skills, projectiles, and hit detection.")]
        public List<string> availableTargetTags = new()
        {
            "Enemy",
            "Ally",
            "Self",
            "Friendly",
            "NPC"
        };

        [Tooltip("Event tags used by hit effects and timeline events.")]
        public List<string> availableEventTags = new()
        {
            "Hit_Default",
            "Hit_Light",
            "Hit_Heavy",
            "Hit_Knockback",
            "Hit_Launch"
        };

        [Tooltip("Window tags used by ComboWindow clips and ActionRoute requirements.")]
        public List<string> availableComboWindowTags = new()
        {
            "Execute1",
            "Buffer1",
            "BackSwing",
            "Evade",
            "EvadeToDash",
            "DashStart",
            "Recovery"
        };
    }
}
