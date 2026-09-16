using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 拼刀契约生命周期片段。
    /// 挂载在怪物出招动作的事件轨道 (EventTrack) 上，定义该招式与玩家对峙拼刀的权威生命周期。
    /// 当该片段自然播完 (OnExit) 或怪物被打断 (OnDisable) 时，单方面权威注销该怪物的拼刀契约。
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "拼刀契约生命周期")]
    public class ParryContractClip : ClipBase
    {
        public override ClipBase Clone()
        {
            return new ParryContractClip
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
