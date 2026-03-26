using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("音效轨道", "#CCB233", "AudioSource Icon", 2)]
    public class AudioTrack : TrackBase
    {
        public AudioTrack()
        {
            trackName = "音效轨道";
            trackType = "AudioTrack";
        }

        public override bool CanOverlap => true;

        public override TrackBase Clone()
        {
            AudioTrack clone = new AudioTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
