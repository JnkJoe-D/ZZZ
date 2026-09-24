using System;
using Game.Framework;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(RouteWindowTrack), "连招窗口")]
    public class RouteWindowClip : ClipBase
    {
        [Header("路由窗口")]
        [ActionProperty("窗口类型")]
        [SerializeReference, SubclassSelector]
        public RouteWindow routewindow;

        public override float Duration
        {
            get => duration;
            set => duration = value;
        }

        public RouteWindowClip()
        {
            clipName = "Route Window";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new RouteWindowClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                routewindow = routewindow?.Clone()
            };
        }
    }
}
