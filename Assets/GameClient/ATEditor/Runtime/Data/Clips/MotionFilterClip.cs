using System;
using UnityEngine;

namespace ATEditor
{
    public enum MotionWindowLocalDeltaFilterMode
    {
        None,
        ZeroLocalX,
        ZeroLocalZ,
        ZeroLocalXZ
    }
    public enum RootMotionCollisionMode
    {
        DefaultSlide,
        StopAtObstacle,
        IgnorePreCheck
    }

    [Serializable]
    [ClipDefinition(typeof(MotionWindowTrack), "位移窗口")]
    public class MotionFilterClip : ClipBase
    {
        [SkillProperty("局部向轴变化过滤")]
        public MotionWindowLocalDeltaFilterMode localDeltaFilterMode = MotionWindowLocalDeltaFilterMode.None;
        [SkillProperty("物理约束碰撞策略")]
        public RootMotionCollisionMode collisionMode = RootMotionCollisionMode.DefaultSlide;
        [SkillProperty("障碍物层级")]
        public LayerMask obstacleMask = ~0;

        public MotionFilterClip()
        {
            clipName = "位移窗口";
            duration = 0.3f;
        }

        public override ClipBase Clone()
        {
            return new MotionFilterClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = clipName,
                startTime = startTime,
                duration = duration,
                isEnabled = isEnabled,
                localDeltaFilterMode = localDeltaFilterMode,
                collisionMode = collisionMode,
                obstacleMask = obstacleMask,
            };
        }
    }
}
