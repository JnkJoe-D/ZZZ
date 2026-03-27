using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("输入派生窗口轨道", "#4CAF50", "d_FilterByLabel", 6)]
    public class ComboWindowTrack : TrackBase
    {
        public ComboWindowTrack()
        {
            trackName = "输入派生窗口轨道";
            trackType = "ComboWindowTrack";
        }

        public override TrackBase Clone()
        {
            ComboWindowTrack clone = new ComboWindowTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
