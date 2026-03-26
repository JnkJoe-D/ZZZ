using System;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("生成轨道(Spawn)", "#4CAF50", "d_GameObject Icon", 4)]
    public class SpawnTrack : TrackBase
    {
        public SpawnTrack()
        {
            trackName = "生成轨道";
            trackType = "SpawnTrack";
        }

        public override TrackBase Clone()
        {
            SpawnTrack clone = new SpawnTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
