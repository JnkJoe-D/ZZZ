using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 统一招架防御窗口片段 (隶属于战斗轨道 CombatTrack)
    /// 在此片段持续时间内，角色身体处于招架迎击状态 (IsParrying=true)。
    /// 怪物打击盒打入时直接触发拼刀、顿帧与受击反制。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CombatTrack), "招架防御窗口")]
    public class ParryWindowClip : ClipBase
    {
        [Tooltip("在此窗口持续时间内，角色身体处于招架迎击状态。怪物打击盒打入时触发拼刀。")]
        public float WindowDuration => duration;

        public override ClipBase Clone()
        {
            return new ParryWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled
            };
        }
    }
}
