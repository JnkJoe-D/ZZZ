using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 轨道分组（树状结构中的第二层节点）
    /// 直接持有并管理 Track 列表
    /// </summary>
    [Serializable]
    public class Group
    {
        [SerializeField]
        public string groupId;           // 唯一ID
        
        [SerializeField]
        public string groupName;         // 分组名称
        
        [SerializeField]
        public bool isCollapsed;         // 折叠状态
        
        [SerializeField]
        public bool isEnabled;           // 是否启用

        [SerializeField]
        public bool isLocked;            // 是否锁定
        
        /// <summary>
        /// 直接持有的轨道列表（树状结构核心）
        /// </summary>
        [SerializeReference]
        public List<TrackBase> tracks = new List<TrackBase>();
        
        public Group(string name)
        {
            groupId = Guid.NewGuid().ToString();
            groupName = name;
            isCollapsed = false;
            isEnabled = true;
            isLocked = false;
            tracks = new List<TrackBase>();
        }

        /// <summary>
        /// 添加轨道到此分组
        /// </summary>
        public T AddTrack<T>() where T : TrackBase, new()
        {
            T track = new T();
            tracks.Add(track);
            return track;
        }

        /// <summary>
        /// 从此分组移除轨道
        /// </summary>
        public void RemoveTrack(TrackBase track)
        {
            tracks.Remove(track);
        }

        /// <summary>
        /// 深拷贝分组（不含轨道，轨道在粘贴逻辑中单独处理）
        /// </summary>
        public Group Clone()
        {
            Group clone = new Group(this.groupName);
            clone.isCollapsed = this.isCollapsed;
            clone.isEnabled = this.isEnabled;
            clone.isLocked = this.isLocked;
            // 注意：tracks 在粘贴逻辑中单独深拷贝
            return clone;
        }

        /// <summary>
        /// 深拷贝分组（包含轨道的完整拷贝）
        /// </summary>
        public Group DeepClone()
        {
            Group clone = Clone();
            clone.tracks = new List<TrackBase>();
            foreach (var track in tracks)
            {
                clone.tracks.Add(track.Clone());
            }
            return clone;
        }
    }
}
