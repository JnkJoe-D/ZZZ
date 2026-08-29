using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(EventTrack), "招架/支援瞬移空间修正")]
    public class AssistTeleportClip : ClipBase
    {
        [Tooltip("触发时会将角色瞬间拉至被匹配攻击预警者的身前")]
        public string Description = "触发支援瞬移";

        public override ClipBase Clone()
        {
            return new AssistTeleportClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                Description = this.Description
            };
        }
    }
}
