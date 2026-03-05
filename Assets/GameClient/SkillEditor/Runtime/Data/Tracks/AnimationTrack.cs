using System;
using cfg;

namespace SkillEditor
{
    [Serializable]
    [TrackDefinition("动画轨道", typeof(SkillAnimationClip), "#33B24C", "Animation.Record", 0)]
    public class AnimationTrack : TrackBase
    {
        public AnimationTrack()
        {
            trackName = "动画轨道";
            trackType = "AnimationTrack";
        }
        /// <summary>
        /// 是否主轨道，主轨道用于播放，其他轨道仅供预览和编辑
        /// </summary>
        [SkillProperty("主轨道")]
        public bool isMasterTrack  = false; 
        public UnityEngine.Vector3 offsetPos; // 预览时重置角色位置
        public UnityEngine.Vector3 offsetRot; // 预览时重置角色旋转

        public override bool CanOverlap => true;
        public override bool CanPlay => isMasterTrack;
        public override TrackBase Clone()
        {
            AnimationTrack clone = new AnimationTrack();
            CloneBaseProperties(clone);
            clone.offsetPos = this.offsetPos;
            clone.offsetRot = this.offsetRot;
            //isMasterTrack 由外部设置,类似ToggleGroup，不在 CloneBaseProperties 中复制
            return clone;
        }
    }
}
