using System;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("物理轨道", "#3F51B5", "d_Rigidbody Icon", 7)]
    public class PhysicsTrack : TrackBase
    {
        public PhysicsTrack()
        {
            trackName = "物理轨道";
            trackType = "PhysicsTrack";
        }

        public override TrackBase Clone()
        {
            PhysicsTrack clone = new PhysicsTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
