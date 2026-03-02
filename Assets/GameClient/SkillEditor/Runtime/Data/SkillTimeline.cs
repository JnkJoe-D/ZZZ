using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能 Timeline 数据（树状结构根节点）
    /// 层级：SkillTimeline → Group[] → Track[] → Clip[]
    /// </summary>
    [Serializable]
    public class SkillTimeline : ScriptableObject
    {
        // 技能基本信息
        public int skillId;
        public string skillName = "新技能";

        // Timeline 参数
        private float duration = 0f;           // 持续时间（秒）
        public bool isLoop = false;             // 是否循环播放

        // 唯一的子节点容器
        public List<Group> groups = new List<Group>();
        public float Duration => duration;
        #region 便捷访问器

        /// <summary>
        /// 获取所有轨道的扁平列表（只读便捷访问器）
        /// 遍历 groups → tracks
        /// </summary>
        public IEnumerable<TrackBase> AllTracks
        {
            get
            {
                if (groups == null) yield break;
                foreach (var group in groups)
                {
                    if (group.tracks == null) continue;
                    foreach (var track in group.tracks)
                    {
                        yield return track;
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有轨道的列表（实体化副本，用于需要索引的场景）
        /// </summary>
        public List<TrackBase> GetAllTracksList()
        {
            return AllTracks.ToList();
        }

        #endregion

        #region 分组操作

        /// <summary>
        /// 添加分组
        /// </summary>
        public Group AddGroup(string name)
        {
            Group group = new Group(name);
            groups.Add(group);
            return group;
        }

        /// <summary>
        /// 删除分组（连同其下所有轨道一起删除）
        /// </summary>
        public void RemoveGroup(Group group)
        {
            groups.Remove(group);
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 查找包含指定轨道的分组
        /// </summary>
        public Group FindGroupContainingTrack(TrackBase track)
        {
            if (groups == null || track == null) return null;
            foreach (var group in groups)
            {
                if (group.tracks != null && group.tracks.Contains(track))
                {
                    return group;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定类型的轨道
        /// </summary>
        public List<T> GetTracks<T>() where T : TrackBase
        {
            List<T> result = new List<T>();
            foreach (var track in AllTracks)
            {
                if (track is T t)
                {
                    result.Add(t);
                }
            }
            return result;
        }

        /// <summary>
        /// 重新计算 Timeline 总时长
        /// </summary>
        public void RecalculateDuration()
        {
            float maxTime = 0f;
            foreach (var track in AllTracks)
            {
                if (track.clips != null && track.isEnabled)
                {
                    foreach (var clip in track.clips)
                    {
                        if (clip != null && clip.isEnabled)
                        {
                            float endTime = clip.StartTime + clip.Duration;
                            if (endTime > maxTime) maxTime = endTime;
                        }
                    }
                }
            }
            // 至少保持 .1秒或者现在的 maxTime
            duration = Mathf.Max(0.1f, maxTime); 
        }

        #endregion
    }
}
