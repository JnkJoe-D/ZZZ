using System;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("战斗轨道", "#E57F33", "Animation.EventMarker", 3)]
    public class CombatTrack : TrackBase
    {
        public CombatTrack()
        {
            trackName = "战斗轨道";
            trackType = "CombatTrack";
        }

        public override TrackBase Clone()
        {
            CombatTrack clone = new CombatTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
