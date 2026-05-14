using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(ComboWindowTrack), "Combo Window")]
    public class ComboWindowClip : ClipBase
    {
        [Header("Combo Window")]
        [SkillProperty("Window Tag")]
        [Tooltip("Tag used by route matching while this window is active.")]
        public string comboTag = "";

        public override float Duration
        {
            get => duration;
            set => duration = value;
        }

        public ComboWindowClip()
        {
            clipName = "Combo Window";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new ComboWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                comboTag = comboTag
            };
        }
    }
}
