using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [Obsolete("已废弃。请在 TransformTrack 上使用 MovementClip (配置 EnemyFront + Instant 瞬移) 代替。")]
    [ClipDefinition(typeof(EventTrack), "招架/支援瞬移空间修正 (已废弃)")]
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
