using System;

namespace ATEditor
{
    /// <summary>
    /// 状态增益轨道 (StatusTrack)。
    /// 专职用于动作时间轴中驱动 Buff（如极限闪避判定窗、攻击无敌帧、霸体增益）的挂载与生命周期管理。
    /// </summary>
    [Serializable]
    [TrackDefinition("状态增益轨道", "#9C27B0", "d_CustomTool", 7)]
    public class StatusTrack : TrackBase
    {
        public StatusTrack()
        {
            trackName = "状态增益轨道";
            trackType = "StatusTrack";
        }

        public override TrackBase Clone()
        {
            StatusTrack clone = new StatusTrack();
            CloneBaseProperties(clone);
            return clone;
        }
    }
}
