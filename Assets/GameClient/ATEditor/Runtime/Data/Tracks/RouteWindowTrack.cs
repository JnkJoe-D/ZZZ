using System;

namespace ATEditor
{
    [Serializable]
    [TrackDefinition("输入派生窗口轨道", "#4CAF50", "d_FilterByLabel", 6)]
    public class RouteWindowTrack : TrackBase
    {
        public RouteWindowTrack()
        {
            trackName = "输入派生窗口轨道";
            trackType = "RouteWindowTrack";
        }

        public override TrackBase Clone()
        {
            RouteWindowTrack clone = new RouteWindowTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
