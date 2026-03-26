using System;

namespace SkillEditor
{
    public enum MotionWindowVisualOffsetMode
    {
        None,
        X,
        Z,
        XZ
    }
    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "视觉偏移")]
    public class VisualOffsetClip : ClipBase
    {
        [SkillProperty("视觉偏移轴")]
        public MotionWindowVisualOffsetMode visualOffsetMode = MotionWindowVisualOffsetMode.XZ;

        public VisualOffsetClip()
        {
            clipName = "视觉偏移";
            duration = 0.3f;
        }

        public override ClipBase Clone()
        {
            return new VisualOffsetClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                visualOffsetMode = visualOffsetMode,
            };
        }
    }
}
