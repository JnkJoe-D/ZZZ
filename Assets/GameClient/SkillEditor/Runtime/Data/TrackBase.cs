using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道基类（树状结构中的第三层节点）
    /// 直接持有并管理 Clip 列表
    /// </summary>
    [Serializable]
    public abstract class TrackBase
    {
        // 轨道唯一标识
        [HideInInspector]
        public string trackId;              // 唯一ID
        
        // 轨道基本信息
        [HideInInspector]
        public string trackType;            // 轨道类型名称
        
        [SkillProperty("轨道名称")]
        public string trackName;            // 轨道显示名称
        
        // 轨道状态
        [SkillProperty("静音")]
        public bool isMuted;                // 是否静音
        
        [SkillProperty("锁定")]
        public bool isLocked;               // 是否锁定
        [SkillProperty("隐藏")]
        public bool isHidden;               // 是否隐藏
        [SkillProperty("折叠")]
        public bool isCollapsed;            // 是否折叠
        
        [SkillProperty("启用")]
        public bool isEnabled;              // 是否启用

        // 片段列表
        [SerializeReference]
        [HideInInspector]
        public List<ClipBase> clips = new List<ClipBase>();

        /// <summary>
        /// 构造函数
        /// </summary>
        protected TrackBase()
        {
            trackId = Guid.NewGuid().ToString();
            trackType = GetType().Name;
            trackName = trackType;
            isCollapsed = false;
            isEnabled = true;
            clips = new List<ClipBase>();
        }

        /// <summary>
        /// 是否允许片段重叠
        /// </summary>
        public virtual bool CanOverlap => false;
        /// <summary>
        /// 是否允许播放，如存在多个动画轨道时，只有一个轨道能播放，其他轨道仅供预览和编辑
        /// </summary>
        public virtual bool CanPlay{get;private set;} = true;

        /// <summary>
        /// 添加片段
        /// </summary>
        public T AddClip<T>(float startTime) where T : ClipBase, new()
        {
            T clip = new T();
            clip.startTime = startTime;
            clips.Add(clip);
            return clip;
        }

        /// <summary>
        /// 删除片段
        /// </summary>
        public void RemoveClip(ClipBase clip)
        {
            clips.Remove(clip);
        }

        /// <summary>
        /// 检查片段是否重叠
        /// </summary>
        public bool CheckOverlap(ClipBase newClip)
        {
            if (CanOverlap) return true;

            foreach (var clip in clips)
            {
                if (clip == newClip) continue;
                if (newClip.startTime < clip.EndTime && newClip.EndTime > clip.startTime)
                {
                    return false; // 重叠
                }
            }
            return true;
        }

        #region 拷贝与克隆

        /// <summary>
        /// 深拷贝轨道（子类实现）
        /// </summary>
        public abstract TrackBase Clone();

        /// <summary>
        /// 拷贝基础属性和片段列表（供子类 Clone 调用）
        /// </summary>
        protected void CloneBaseProperties(TrackBase clone)
        {
            clone.trackName = this.trackName;
            clone.trackType = this.trackType;
            clone.isEnabled = this.isEnabled;
            clone.isMuted = this.isMuted;
            clone.isLocked = this.isLocked;
            clone.isHidden = this.isHidden;
            clone.isCollapsed = this.isCollapsed;

            // 深拷贝片段
            clone.clips = new List<ClipBase>();
            if (this.clips != null)
            {
                foreach (var clip in this.clips)
                {
                    clone.clips.Add(clip.Clone());
                }
            }
        }

        #endregion

    }
}
