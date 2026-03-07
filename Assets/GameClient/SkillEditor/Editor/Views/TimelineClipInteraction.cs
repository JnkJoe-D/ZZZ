using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 时间轴片段交互类
    /// 负责片段点击、拖拽（移动/缩放/跨轨道）、选择、快捷键等交互逻辑
    /// </summary>
    public class TimelineClipInteraction
    {
        private SkillEditorWindow window;
        private SkillEditorState state;
        private SkillEditorEvents events;
        private TimelineCoordinates coords;
        private TimelineClipOperations clipOps;

        // 常量
        private const float EDGE_RESIZE_WIDTH = 8f;

        // 拖拽状态
        public enum DragMode
        {
            None,
            MoveClip,
            ResizeLeft,
            ResizeRight,
            CrossTrackDrag,
            BlendIn,
            BlendOut
        }

        public DragMode CurrentDragMode { get; private set; } = DragMode.None;

        private ClipBase draggingClip = null;
        private float dragStartMouseX = 0f;
        private float dragStartClipTime = 0f;
        private float dragStartClipDuration = 0f;

        // 跨轨道拖拽与多选同步
        public TrackBase DragHoveredTrack { get; private set; } = null;
        private TrackBase dragSourceTrack = null;

        private struct SelectedClipInitialState
        {
            public ClipBase clip;
            public float initialStartTime;
            public TrackBase initialTrack;
            public int trackIndexOffset;
        }
        private List<SelectedClipInitialState> selectedClipsInitialStates = new List<SelectedClipInitialState>();

        // 吸附
        public float CurrentSnapTime { get; private set; } = -1f;

        public TimelineClipInteraction(SkillEditorWindow window, SkillEditorState state, SkillEditorEvents events,
            TimelineCoordinates coords, TimelineClipOperations clipOps)
        {
            this.window = window;
            this.state = state;
            this.events = events;
            this.coords = coords;
            this.clipOps = clipOps;
        }

        #region 选择

        /// <summary>
        /// 选中片段（支持Ctrl多选）
        /// </summary>
        public void SelectClip(ClipBase clip)
        {
            Event e = Event.current;
            bool isCtrlPressed = (e != null && e.control);
            state.isTimelineSelected = false;

            if (isCtrlPressed)
            {
                if (state.selectedClips.Contains(clip))
                {
                    state.selectedClips.Remove(clip);
                    if (state.selectedClips.Count == 0)
                    {
                        Selection.activeObject = null;
                    }
                }
                else
                {
                    state.selectedClips.Add(clip);
                    SkillTimeline timeline = state.currentTimeline;
                    ClipObject clipObj = ClipObject.Create(clip, timeline);
                    Selection.activeObject = clipObj;
                }
            }
            else
            {
                if (!state.selectedClips.Contains(clip))
                {
                    state.selectedClips.Clear();
                    state.selectedClips.Add(clip);
                }

                state.selectedTrack = null;
                state.selectedGroup = null;

                SkillTimeline timeline = state.currentTimeline;
                ClipObject clipObj = ClipObject.Create(clip, timeline);
                Selection.activeObject = clipObj;
            }

            events.NotifySelectionChanged();
        }

        /// <summary>
        /// 清除片段选中状态
        /// </summary>
        public void ClearClipSelection()
        {
            state.selectedClips.Clear();
            events.NotifySelectionChanged();
        }

        /// <summary>
        /// 选中轨道
        /// </summary>
        public void SelectTrack(TrackBase track)
        {
            state.isTimelineSelected = false;
            state.selectedGroup = null;
            state.selectedClips.Clear();
            state.selectedTrack = track;

            state.pasteTargetTrack = track;

            SkillTimeline timeline = state.currentTimeline;
            TrackObject trackObj = TrackObject.Create(track, timeline);
            Selection.activeObject = trackObj;
        }

        /// <summary>
        /// 选中分组
        /// </summary>
        public void SelectGroup(Group group)
        {
            state.ClearSelection();
            state.selectedGroup = group;
            events.NotifySelectionChanged();

            SkillTimeline timeline = state.currentTimeline;
            GroupObject groupObj = GroupObject.Create(group, timeline);
            Selection.activeObject = groupObj;
        }

        #endregion

        #region 快捷键处理

        /// <summary>
        /// 处理快捷键
        /// </summary>
        public void HandleShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.D)
            {
                if (state.selectedClips.Count == 1)
                {
                    clipOps.DuplicateClip(state.selectedClips[0]);
                    e.Use();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                if (state.selectedClips.Count > 0)
                {
                    clipOps.DeleteSelectedClips();
                    e.Use();
                }
            }
        }

        #endregion

        #region 片段交互（核心拖拽逻辑）

        /// <summary>
        /// 处理片段交互（点击、拖拽）
        /// </summary>
        public void HandleClipInteraction(ClipBase clip, Rect clipRect)
        {
            Event e = Event.current;

            // 右键菜单
            if (e.type == EventType.ContextClick && clipRect.Contains(e.mousePosition))
            {
                ShowClipContextMenu(clip);
                e.Use();
                return;
            }

            // 开始拖拽
            if (e.type == EventType.MouseDown && e.button == 0 && clipRect.Contains(e.mousePosition))
            {
                float localX = e.mousePosition.x - clipRect.x;

                SelectClip(clip);

                if (localX < EDGE_RESIZE_WIDTH)
                {
                    CurrentDragMode = DragMode.ResizeLeft;
                }
                else if (localX > clipRect.width - EDGE_RESIZE_WIDTH)
                {
                    CurrentDragMode = DragMode.ResizeRight;
                }
                else if (clip.SupportsBlending && Mathf.Abs(localX - (clip.BlendInDuration * state.zoom)) < EDGE_RESIZE_WIDTH)
                {
                    CurrentDragMode = DragMode.BlendIn;
                }
                else if (clip.SupportsBlending && Mathf.Abs(localX - (clipRect.width - clip.BlendOutDuration * state.zoom)) < EDGE_RESIZE_WIDTH)
                {
                    CurrentDragMode = DragMode.BlendOut;
                }
                else
                {
                    CurrentDragMode = DragMode.MoveClip;
                    dragSourceTrack = clipOps.FindTrackContainingClip(clip);
                }

                draggingClip = clip;
                dragStartMouseX = e.mousePosition.x;
                dragStartClipTime = clip.StartTime;
                dragStartClipDuration = clip.Duration;

                // 记录所有选中片段的初始状态
                selectedClipsInitialStates.Clear();
                if (state.selectedClips.Contains(clip))
                {
                    var allTracks = state.currentTimeline.GetAllTracksList();
                    int mainTrackIndex = allTracks.IndexOf(dragSourceTrack);
                    foreach (var c in state.selectedClips)
                    {
                        TrackBase track = clipOps.FindTrackContainingClip(c);
                        int trackIndex = allTracks.IndexOf(track);
                        selectedClipsInitialStates.Add(new SelectedClipInitialState
                        {
                            clip = c,
                            initialStartTime = c.StartTime,
                            initialTrack = track,
                            trackIndexOffset = trackIndex - mainTrackIndex
                        });
                    }
                }

                window.RecordUndo("移动/调整片段");
                e.Use();
            }

            // 拖拽中
            if (e.type == EventType.MouseDrag && CurrentDragMode != DragMode.None && draggingClip != null)
            {
                float deltaX = e.mousePosition.x - dragStartMouseX;
                float deltaTime = coords.PixelToTime(deltaX);

                SkillTimeline timeline = state.currentTimeline;
                TrackBase currentTrack = null;
                if (timeline != null)
                {
                    foreach (var track in timeline.AllTracks)
                    {
                        if (track.clips.Contains(draggingClip))
                        {
                            currentTrack = track;
                            break;
                        }
                    }
                }

                bool hasSnapped = false;

                if (CurrentDragMode == DragMode.MoveClip || CurrentDragMode == DragMode.CrossTrackDrag)
                {
                    // 检查跨轨道拖拽
                    float virtualMouseY = e.mousePosition.y + state.verticalScrollOffset;
                    TrackBase hoveredTrack = GetTrackAtPosition(virtualMouseY);
                    DragHoveredTrack = hoveredTrack;

                    if (hoveredTrack != null && dragSourceTrack != null && hoveredTrack != dragSourceTrack)
                    {
                        bool allCompatible = true;
                        var allTracks = state.currentTimeline.GetAllTracksList();
                        int targetMainIndex = allTracks.IndexOf(hoveredTrack);

                        foreach (var initState in selectedClipsInitialStates)
                        {
                            int targetIdx = targetMainIndex + initState.trackIndexOffset;
                            if (targetIdx < 0 || targetIdx >= allTracks.Count)
                            {
                                allCompatible = false;
                                break;
                            }

                            TrackBase targetTrack = allTracks[targetIdx];
                            if (!clipOps.IsClipCompatibleWithTrack(initState.clip, targetTrack))
                            {
                                allCompatible = false;
                                break;
                            }
                        }

                        if (allCompatible)
                        {
                            if (CurrentDragMode != DragMode.CrossTrackDrag)
                            {
                                CurrentDragMode = DragMode.CrossTrackDrag;
                                Debug.Log($"[技能编辑器] 进入多选跨轨道拖拽模式: {hoveredTrack.trackName}");
                            }
                        }
                        else
                        {
                            DragHoveredTrack = null;
                        }
                    }
                    else if (hoveredTrack == dragSourceTrack)
                    {
                        if (CurrentDragMode == DragMode.CrossTrackDrag)
                        {
                            CurrentDragMode = DragMode.MoveClip;
                        }
                    }

                    // 计算新位置（左右边界双重吸附）
                    float targetStartTime = dragStartClipTime + deltaTime;
                    float targetEndTime = targetStartTime + draggingClip.Duration;

                    bool snappedStart, snappedEnd;
                    float distStart, distEnd;

                    float snappedStartVal = coords.SnapTime(targetStartTime, draggingClip, out snappedStart, out distStart);
                    float snappedEndVal = coords.SnapTime(targetEndTime, draggingClip, out snappedEnd, out distEnd);

                    float finalStartTime = targetStartTime;
                    bool usedEndSnap = false;
                    hasSnapped = false;

                    if (snappedStart)
                    {
                        finalStartTime = snappedStartVal;
                        hasSnapped = true;

                        if (snappedEnd && distEnd < distStart - 2.0f)
                        {
                            finalStartTime = snappedEndVal - draggingClip.Duration;
                            usedEndSnap = true;
                        }
                    }
                    else if (snappedEnd)
                    {
                        finalStartTime = snappedEndVal - draggingClip.Duration;
                        hasSnapped = true;
                        usedEndSnap = true;
                    }

                    float newTime = Mathf.Max(0, finalStartTime);

                    if (hasSnapped)
                    {
                        CurrentSnapTime = usedEndSnap ? newTime + draggingClip.Duration : newTime;
                    }
                    else
                    {
                        CurrentSnapTime = -1f;
                    }

                    // 同轨道重叠检测
                    if (CurrentDragMode == DragMode.MoveClip && currentTrack != null && !coords.AllowsOverlap(currentTrack))
                    {
                        if (coords.HasOverlap(currentTrack, newTime, draggingClip.Duration, draggingClip))
                        {
                            float fixedTime = newTime;
                            float minDiff = float.MaxValue;
                            bool foundFix = false;

                            foreach (var cl in currentTrack.clips)
                            {
                                if (cl == draggingClip) continue;

                                if (!(newTime + draggingClip.Duration <= cl.StartTime || newTime >= cl.EndTime))
                                {
                                    float leftOption = cl.StartTime - draggingClip.Duration;
                                    float rightOption = cl.EndTime;

                                    if (leftOption >= 0 && !coords.HasOverlap(currentTrack, leftOption, draggingClip.Duration, draggingClip))
                                    {
                                        float diff = Mathf.Abs(leftOption - newTime);
                                        if (diff < minDiff)
                                        {
                                            minDiff = diff;
                                            fixedTime = leftOption;
                                            foundFix = true;
                                        }
                                    }

                                    if (!coords.HasOverlap(currentTrack, rightOption, draggingClip.Duration, draggingClip))
                                    {
                                        float diff = Mathf.Abs(rightOption - newTime);
                                        if (diff < minDiff)
                                        {
                                            minDiff = diff;
                                            fixedTime = rightOption;
                                            foundFix = true;
                                        }
                                    }
                                }
                            }

                            if (foundFix)
                            {
                                newTime = fixedTime;
                                CurrentSnapTime = newTime;
                            }
                            else
                            {
                                if (!coords.HasOverlap(currentTrack, draggingClip.StartTime, draggingClip.Duration, draggingClip))
                                {
                                    newTime = draggingClip.StartTime;
                                }
                                else
                                {
                                    newTime = dragStartClipTime;
                                }
                                CurrentSnapTime = -1f;
                            }
                        }
                    }

                    draggingClip.StartTime = newTime;

                    // 同步移动其它选中片段
                    if (selectedClipsInitialStates.Count > 1)
                    {
                        float delta = draggingClip.StartTime - dragStartClipTime;
                        foreach (var initState in selectedClipsInitialStates)
                        {
                            if (initState.clip == draggingClip) continue;
                            initState.clip.StartTime = Mathf.Max(0, initState.initialStartTime + delta);
                        }
                    }

                    if (!hasSnapped) CurrentSnapTime = -1f;

                    // 自动融合处理
                    if (currentTrack != null) coords.AutoResolveBlending(currentTrack, draggingClip);
                    foreach (var s in selectedClipsInitialStates)
                    {
                        TrackBase track = clipOps.FindTrackContainingClip(s.clip);
                        if (track != null) coords.AutoResolveBlending(track, s.clip);
                    }
                }
                else if (CurrentDragMode == DragMode.ResizeLeft)
                {
                    float distStart;
                    float snappedTime = coords.SnapTime(dragStartClipTime + deltaTime, draggingClip, out hasSnapped, out distStart);
                    float newStartTime = Mathf.Max(0, snappedTime);
                    CurrentSnapTime = hasSnapped ? newStartTime : -1f;

                    float timeDiff = newStartTime - dragStartClipTime;
                    float newDuration = Mathf.Max(0.1f, dragStartClipDuration - timeDiff);

                    if (currentTrack != null && !coords.AllowsOverlap(currentTrack))
                    {
                        if (coords.HasOverlap(currentTrack, newStartTime, newDuration, draggingClip))
                        {
                            CurrentSnapTime = -1f;
                            e.Use();
                            return;
                        }
                    }

                    draggingClip.StartTime = newStartTime;
                    draggingClip.Duration = newDuration;
                    CurrentSnapTime = hasSnapped ? draggingClip.StartTime : -1f;

                    if (currentTrack != null) coords.AutoResolveBlending(currentTrack, draggingClip);
                }
                else if (CurrentDragMode == DragMode.ResizeRight)
                {
                    float targetEndTime = dragStartClipTime + dragStartClipDuration + deltaTime;
                    float distEnd;
                    float snappedEndTime = coords.SnapTime(targetEndTime, draggingClip, out hasSnapped, out distEnd);
                    float adjustedDuration = Mathf.Max(0.1f, snappedEndTime - draggingClip.StartTime);
                    CurrentSnapTime = hasSnapped ? snappedEndTime : -1f;

                    if (currentTrack != null && !coords.AllowsOverlap(currentTrack))
                    {
                        if (coords.HasOverlap(currentTrack, draggingClip.StartTime, adjustedDuration, draggingClip))
                        {
                            CurrentSnapTime = -1f;
                            e.Use();
                            return;
                        }
                    }

                    draggingClip.Duration = adjustedDuration;
                    CurrentSnapTime = hasSnapped ? (draggingClip.StartTime + draggingClip.Duration) : -1f;

                    if (currentTrack != null) coords.AutoResolveBlending(currentTrack, draggingClip);
                }
                else if (CurrentDragMode == DragMode.BlendIn)
                {
                    float newBlendIn = (e.mousePosition.x - clipRect.x) / state.zoom;
                    draggingClip.BlendInDuration = Mathf.Clamp(newBlendIn, 0, draggingClip.Duration);
                }
                else if (CurrentDragMode == DragMode.BlendOut)
                {
                    float newBlendOut = (clipRect.x + clipRect.width - e.mousePosition.x) / state.zoom;
                    draggingClip.BlendOutDuration = Mathf.Clamp(newBlendOut, 0, draggingClip.Duration);
                }

                events.OnRepaintRequest?.Invoke();
                e.Use();
            }

            // 结束拖拽
            if (e.type == EventType.MouseUp && e.button == 0 && CurrentDragMode != DragMode.None)
            {
                if (CurrentDragMode == DragMode.CrossTrackDrag)
                {
                    window.RecordUndo("跨轨道移动片段");

                    if (DragHoveredTrack != null)
                    {
                        var allTracks = state.currentTimeline.GetAllTracksList();
                        int targetMainIndex = allTracks.IndexOf(DragHoveredTrack);

                        foreach (var initState in selectedClipsInitialStates)
                        {
                            initState.initialTrack.clips.Remove(initState.clip);
                        }

                        foreach (var initState in selectedClipsInitialStates)
                        {
                            int targetIdx = targetMainIndex + initState.trackIndexOffset;
                            TrackBase targetTrack = allTracks[targetIdx];

                            if (!coords.AllowsOverlap(targetTrack))
                            {
                                initState.clip.StartTime = coords.FindNextAvailableTime(targetTrack, initState.clip.StartTime, initState.clip.Duration);
                            }

                            targetTrack.clips.Add(initState.clip);
                            coords.AutoResolveBlending(targetTrack, initState.clip);
                        }

                        Debug.Log($"[技能编辑器] 多选片段移动完成，主片段迁移至: {DragHoveredTrack.trackName}");
                    }
                    else
                    {
                        Debug.LogWarning("[技能编辑器] 跨轨道移动取消：目标位置无效。");
                    }

                    ResetDragState();
                    events.OnRepaintRequest?.Invoke();
                    e.Use();
                    return;
                }

                ResetDragState();
                e.Use();
            }

            // 更新鼠标光标
            if (clipRect.Contains(e.mousePosition) && CurrentDragMode == DragMode.None)
            {
                float localX = e.mousePosition.x - clipRect.x;
                if (localX < EDGE_RESIZE_WIDTH || localX > clipRect.width - EDGE_RESIZE_WIDTH)
                {
                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.ResizeHorizontal);
                }
                else if (clip.SupportsBlending && (Mathf.Abs(localX - (clip.BlendInDuration * state.zoom)) < EDGE_RESIZE_WIDTH ||
                         Mathf.Abs(localX - (clipRect.width - clip.BlendOutDuration * state.zoom)) < EDGE_RESIZE_WIDTH))
                {
                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.ResizeHorizontal);
                }
                else
                {
                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.MoveArrow);
                }
            }
        }

        #endregion

        #region 上下文菜单

        /// <summary>
        /// 显示片段右键菜单
        /// </summary>
        public void ShowClipContextMenu(ClipBase clip)
        {
            bool isInSelection = state.selectedClips.Contains(clip);
            bool isMultiSelect = isInSelection && state.selectedClips.Count > 1;

            GenericMenu menu = new GenericMenu();

            if (isMultiSelect)
            {
                string operationName = clip.isEnabled ? "禁用" : "启用";
                menu.AddItem(new GUIContent($"{operationName} {state.selectedClips.Count} 个片段"), false, () =>
                {
                    window.RecordUndo("批量启用/禁用片段");
                    bool targetState = !clip.isEnabled;
                    foreach (var c in state.selectedClips) c.isEnabled = targetState;
                    events.OnRepaintRequest?.Invoke();
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent($"删除 {state.selectedClips.Count} 个片段 (Delete)"), false, () => clipOps.DeleteSelectedClips());
                menu.AddSeparator("");
                menu.AddItem(new GUIContent($"复制 {state.selectedClips.Count} 个片段 (Ctrl+C)"), false, () => clipOps.CopySelectedClips());
            }
            else
            {
                menu.AddItem(new GUIContent(clip.isEnabled ? "禁用片段" : "启用片段"), false, () =>
                {
                    window.RecordUndo("切换片段状态");
                    clip.isEnabled = !clip.isEnabled;
                    window.RefreshWindow();
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("删除片段 (Delete)"), false, () => clipOps.OnDeleteClip(clip));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("复制 (Ctrl+C)"), false, () => clipOps.OnCopyClip(clip));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 显示添加片段菜单
        /// </summary>
        public void ShowAddClipMenu(TrackBase track, float mouseX)
        {
            float clickTime = coords.PhysXToTime(mouseX);
            clickTime = Mathf.Max(0, clickTime);

            state.pasteTargetTrack = track;
            state.pasteTargetTime = clickTime;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("添加片段"), false, () => clipOps.OnAddClip(track, clickTime));

            menu.AddSeparator("");

            if (state.copiedClipsData != null && state.copiedClipsData.Count > 0)
            {
                if (state.copiedClipsData.Count == 1)
                {
                    if (clipOps.IsClipCompatibleWithTrack(state.copiedClipsData[0].clip, track))
                    {
                        menu.AddItem(new GUIContent("粘贴 (Ctrl+V)"), false, () => clipOps.PasteClips());
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("粘贴 (Ctrl+V) - 类型不兼容"));
                    }
                }
                else
                {
                    bool partOfCopiedSet = false;
                    foreach (var data in state.copiedClipsData)
                    {
                        if (data.sourceTrackId == track.trackId)
                        {
                            partOfCopiedSet = true;
                            break;
                        }
                    }

                    if (partOfCopiedSet)
                    {
                        menu.AddItem(new GUIContent("粘贴 (Ctrl+V)"), false, () => clipOps.PasteClips());
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("粘贴 (Ctrl+V) - 类型不兼容"));
                    }
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴 (Ctrl+V)"));
            }

            menu.ShowAsContext();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 重置拖拽状态
        /// </summary>
        private void ResetDragState()
        {
            CurrentDragMode = DragMode.None;
            draggingClip = null;
            DragHoveredTrack = null;
            dragSourceTrack = null;
            CurrentSnapTime = -1f;
        }

        /// <summary>
        /// 获取指定虚拟 Y 位置的轨道
        /// </summary>
        private TrackBase GetTrackAtPosition(float virtualY)
        {
            SkillTimeline timeline = window.GetCurrentTimeline();
            if (timeline == null) return null;

            const float TRACK_HEIGHT = 40f;
            const float GROUP_HEIGHT = 30f;
            float yOffset = 0f;

            if (timeline.groups != null)
            {
                for (int i = 0; i < timeline.groups.Count; i++)
                {
                    var group = timeline.groups[i];
                    yOffset += GROUP_HEIGHT;
                    if (!group.isCollapsed && group.tracks != null)
                    {
                        for (int j = 0; j < group.tracks.Count; j++)
                        {
                            TrackBase track = group.tracks[j];
                            if (track != null)
                            {
                                if (virtualY >= yOffset && virtualY < yOffset + TRACK_HEIGHT)
                                    return track;
                                yOffset += TRACK_HEIGHT;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取片段颜色
        /// </summary>
        public Color GetClipColor(string trackType)
        {
            return TrackRegistry.GetTrackColor(trackType);
        }

        /// <summary>
        /// 获取截断并带省略号的文本
        /// </summary>
        public string GetTruncatedText(string originalText, float maxWidth, GUIStyle style)
        {
            if (string.IsNullOrEmpty(originalText)) return "";

            GUIContent content = new GUIContent(originalText);
            if (style.CalcSize(content).x <= maxWidth) return originalText;

            string truncated = originalText;
            while (truncated.Length > 1)
            {
                truncated = truncated.Substring(0, truncated.Length - 1);
                if (style.CalcSize(new GUIContent(truncated + "...")).x <= maxWidth)
                {
                    return truncated + "...";
                }
            }

            return "";
        }

        #endregion
    }
}
