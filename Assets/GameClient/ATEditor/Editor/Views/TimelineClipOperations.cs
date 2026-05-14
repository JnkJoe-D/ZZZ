using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace ATEditor.Editor
{
    /// <summary>
    /// 时间轴片段操作类
    /// 负责复制、粘贴、删除、添加、克隆等剪贴板和数据操作
    /// </summary>
    public class TimelineClipOperations
    {
        private ATEditorWindow window;
        private ATEditorState state;
        private ATEditorEvents events;
        private TimelineCoordinates coords;

        public TimelineClipOperations(ATEditorWindow window, ATEditorState state, ATEditorEvents events, TimelineCoordinates coords)
        {
            this.window = window;
            this.state = state;
            this.events = events;
            this.coords = coords;
        }

        #region 删除操作

        /// <summary>
        /// 删除单个片段，供右键菜单调用。
        /// </summary>
        public void OnDeleteClip(ClipBase clip)
        {
            if (clip == null) return;

            // 如果右击的片段不在当前选中列表中，先将它设为唯一选中项。
            if (!state.selectedClips.Contains(clip))
            {
                state.selectedClips.Clear();
                state.selectedClips.Add(clip);
                events.NotifySelectionChanged();
            }

            DeleteSelectedClips();
        }

        /// <summary>
        /// 批量删除当前选中的所有片段。
        /// </summary>
        public void DeleteSelectedClips()
        {
            if (state.selectedClips.Count == 0) return;

            window.RecordUndo("批量删除片段");
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            List<ClipBase> toDelete = new List<ClipBase>(state.selectedClips);
            foreach (var clip in toDelete)
            {
                foreach (var track in timeline.AllTracks)
                {
                    if (track.clips.Remove(clip)) break;
                }

                state.selectedClips.Remove(clip);
            }

            if (state.selectedClips.Count == 0) Selection.activeObject = null;
            events.NotifySelectionChanged();
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 删除选中的片段，供快捷键和交互逻辑调用。
        /// </summary>
        public void DeleteSelectedClip()
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            List<ClipBase> clipsToDelete = new List<ClipBase>(state.selectedClips);
            if (clipsToDelete.Count == 0) return;

            window.RecordUndo($"删除 {clipsToDelete.Count} 个片段");

            foreach (var track in timeline.AllTracks)
            {
                foreach (var clip in clipsToDelete)
                {
                    track.clips.Remove(clip);
                }
            }

            Debug.Log($"[动作时间轴编辑器] 已删除 {clipsToDelete.Count} 个片段");

            state.selectedClips.Clear();
            Selection.activeObject = null;
            events.NotifySelectionChanged();
        }

        #endregion

        #region 复制操作

        /// <summary>
        /// 复制当前选中的片段，支持多选。
        /// </summary>
        public void CopySelectedClips()
        {
            if (state.selectedClips.Count == 0) return;

            state.copiedClipsData.Clear();
            foreach (var clip in state.selectedClips)
            {
                TrackBase track = FindTrackContainingClip(clip);
                if (track == null) continue;

                state.copiedClipsData.Add(new ATEditorState.CopiedClipData
                {
                    clip = clip.Clone(),
                    sourceTrackId = track.trackId,
                    sourceTrackIndex = state.currentTimeline.GetAllTracksList().IndexOf(track)
                });
            }

            Debug.Log($"[动作时间轴编辑器] 已复制 {state.copiedClipsData.Count} 个片段");
        }

        /// <summary>
        /// 复制片段回调。若该片段已在多选中，则复制当前全部选中片段。
        /// </summary>
        public void OnCopyClip(ClipBase clip)
        {
            if (clip == null) return;

            if (state.selectedClips.Contains(clip))
            {
                CopySelectedClips();
            }
            else
            {
                state.selectedClips.Clear();
                state.selectedClips.Add(clip);
                CopySelectedClips();
            }
        }

        #endregion

        #region 粘贴操作

        /// <summary>
        /// 批量粘贴片段。
        /// </summary>
        public void PasteClips()
        {
            if (state.copiedClipsData.Count == 0 || state.pasteTargetTrack == null) return;

            window.RecordUndo($"粘贴 {state.copiedClipsData.Count} 个片段");

            // 以最早的开始时间作为锚点，整体平移到目标时间。
            float minStartTime = float.MaxValue;
            foreach (var data in state.copiedClipsData)
            {
                if (data.clip.StartTime < minStartTime) minStartTime = data.clip.StartTime;
            }

            float timeOffset = state.pasteTargetTime - minStartTime;
            List<ClipBase> pastedClips = new List<ClipBase>();

            foreach (var data in state.copiedClipsData)
            {
                TrackBase targetTrack = state.copiedClipsData.Count == 1
                    ? state.pasteTargetTrack
                    : state.GetTrackById(data.sourceTrackId);

                if (targetTrack == null) continue;

                ClipBase newClip = data.clip.Clone();
                newClip.clipId = Guid.NewGuid().ToString();

                // 粘贴出的片段不继承自动融合时长，避免批量复制后出现意外混合。
                newClip.BlendInDuration = 0;
                newClip.BlendOutDuration = 0;
                newClip.StartTime = Mathf.Max(0, data.clip.StartTime + timeOffset);

                // 自动挪到当前轨道中可放置的位置，避免重叠。
                newClip.StartTime = coords.FindNextAvailableTime(targetTrack, newClip.StartTime, newClip.Duration);

                targetTrack.clips.Add(newClip);
                pastedClips.Add(newClip);
            }

            state.selectedClips.Clear();
            state.selectedClips.AddRange(pastedClips);
            events.NotifySelectionChanged();

            Debug.Log($"[动作时间轴编辑器] 已粘贴 {pastedClips.Count} 个片段");
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 粘贴片段回调。
        /// </summary>
        public void OnPasteClip()
        {
            PasteClips();
        }

        #endregion

        #region 克隆操作

        /// <summary>
        /// 克隆片段，通常由 Ctrl+D 调用。
        /// </summary>
        public void DuplicateClip(ClipBase sourceClip)
        {
            TrackBase track = FindTrackContainingClip(sourceClip);
            if (track == null) return;

            window.RecordUndo("克隆片段");

            ClipBase newClip = sourceClip.Clone();
            newClip.clipId = Guid.NewGuid().ToString();
            newClip.StartTime = sourceClip.StartTime + sourceClip.Duration;

            track.clips.Add(newClip);

            state.selectedClips.Clear();
            state.selectedClips.Add(newClip);

            Debug.Log($"[动作时间轴编辑器] 已克隆片段: {newClip.clipName}");
            events.OnRepaintRequest?.Invoke();
        }

        #endregion

        #region 添加操作

        /// <summary>
        /// 向轨道添加新片段。
        /// </summary>
        public void OnAddClip(TrackBase track, float startTime, Type clipType = null)
        {
            window.RecordUndo("添加片段");

            ClipBase newClip = CreateClipForTrack(track, clipType);
            if (newClip == null)
            {
                Debug.LogError($"[动作时间轴编辑器] 无法为轨道类型 {track.trackType} 创建片段");
                return;
            }

            newClip.StartTime = startTime;
            newClip.Duration = 1.0f;

            string clipDisplayName = TrackRegistry.GetClipDisplayName(newClip.GetType());
            newClip.clipName = $"{clipDisplayName}片段";

            track.clips.Add(newClip);

            Debug.Log($"[动作时间轴编辑器] 在 {startTime:F2}s 处添加片段: {newClip.clipName}");
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 根据轨道类型创建默认片段。
        /// </summary>
        public ClipBase CreateClipForTrack(TrackBase track, Type clipType = null)
        {
            if (track == null) return null;

            if (clipType == null)
            {
                var types = TrackRegistry.GetClipTypes(track.GetType());
                if (types != null && types.Length > 0)
                {
                    clipType = types[0];
                }
            }

            if (clipType != null)
            {
                try
                {
                    return (ClipBase)Activator.CreateInstance(clipType);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[动作时间轴编辑器] 创建片段失败: {clipType.Name}, 错误: {e.Message}");
                }
            }

            return null;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据片段类型获取对应的轨道类型。
        /// </summary>
        public string GetTrackTypeForClip(ClipBase clip)
        {
            if (clip == null) return null;
            return TrackRegistry.GetTrackTypeByClipType(clip.GetType());
        }

        /// <summary>
        /// 检查片段是否与轨道类型兼容。
        /// </summary>
        public bool IsClipCompatibleWithTrack(ClipBase clip, TrackBase track)
        {
            if (clip == null || track == null) return false;
            string clipTrackType = GetTrackTypeForClip(clip);
            return track.trackType == clipTrackType;
        }

        /// <summary>
        /// 查找包含指定片段的轨道。
        /// </summary>
        public TrackBase FindTrackContainingClip(ClipBase clip)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return null;

            foreach (var track in timeline.AllTracks)
            {
                if (track.clips.Contains(clip)) return track;
            }

            return null;
        }

        #endregion
    }
}
