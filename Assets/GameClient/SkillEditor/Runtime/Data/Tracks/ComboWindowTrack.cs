using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("派生窗口(Combo)", typeof(ComboWindowClip), "#4CAF50", "d_FilterByLabel", 6)]
    public class ComboWindowTrack : TrackBase
    {
        public ComboWindowTrack()
        {
            trackName = "连击派生窗口";
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
