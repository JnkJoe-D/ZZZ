using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SkillEditor.Editor
{
    /// <summary>
    /// Timeline 视图
    /// 职责：渲染（标尺/轨道/片段/框选/吸附线）+ 事件路由
    /// 交互逻辑委托给 TimelineClipInteraction
    /// 剪贴板操作委托给 TimelineClipOperations
    /// 坐标转换/吸附委托给 TimelineCoordinates
    /// </summary>
    public class TimelineView
    {
        private SkillEditorWindow window;
        private SkillEditorState state;
        private SkillEditorEvents events;

        // 子模块
        private TimelineCoordinates coords;
        private TimelineClipOperations clipOps;
        private TimelineClipInteraction clipInteraction;

        // 常量 (已移至 SkillEditorStyles)

        // 框选
        private bool isBoxSelecting = false;
        private Vector2 boxSelectStart;
        private Vector2 boxSelectEnd;

        // 平移导航
        private bool isPanning = false;
        private Vector2 panningStartMousePos;
        private float panningStartScrollX;
        private float panningStartScrollY;

        public TimelineView(SkillEditorWindow window, SkillEditorState state, SkillEditorEvents events)
        {
            this.window = window;
            this.state = state;
            this.events = events;

            coords = new TimelineCoordinates(state);
            clipOps = new TimelineClipOperations(window, state, events, coords);
            clipInteraction = new TimelineClipInteraction(window, state, events, coords, clipOps);
        }

        public void DoGUI(Rect rect)
        {
            float maxTime = 30f;
            float contentWidth = Mathf.Max(rect.width, coords.TimeToPixel(maxTime));
            float scrollbarHeight = 0f; // 隐藏滚动轴视觉
            float viewHeight = rect.height - scrollbarHeight;

            // 1. 优先处理视口平移导航
            HandlePanning(rect);

            // 2. 绘制轨道区域（带裁剪，防止溢出到标尺）
            Rect tracksGroupRect = new Rect(0, SkillEditorStyles.TIME_RULER_HEIGHT, rect.width, viewHeight - SkillEditorStyles.TIME_RULER_HEIGHT);
            GUI.BeginGroup(tracksGroupRect);
            {
                DrawTracksArea(new Rect(0, -SkillEditorStyles.TIME_RULER_HEIGHT, rect.width, viewHeight));
            }
            GUI.EndGroup();

            // 3. 绘制置顶 UI 部件
            DrawTimeRuler(new Rect(0, 0, rect.width, viewHeight));
            DrawTimeIndicator(new Rect(0, 0, rect.width, viewHeight));
            DrawSnapLine(rect);

            // 4. 隐藏横向滚动条绘制
            /*
            if (scrollbarHeight > 0)
            {
                Rect scrollbarRect = new Rect(0, viewHeight, rect.width, scrollbarHeight);
                state.scrollOffset = GUI.HorizontalScrollbar(scrollbarRect, state.scrollOffset, rect.width, 0, contentWidth);
            }
            */



            // 处理键盘快捷键与常规鼠标事件
            clipInteraction.HandleShortcuts();
            HandleMouseEvents(rect);

            // 绘制框选框
            if (isBoxSelecting)
            {
                Rect selectionRect = coords.GetRectFromPoints(boxSelectStart, boxSelectEnd);
                EditorGUI.DrawRect(selectionRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
                Handles.BeginGUI();
                Handles.color = new Color(0.8f, 0.8f, 1f, 0.8f);
                Handles.DrawSolidRectangleWithOutline(selectionRect, new Color(0,0,0,0), new Color(1, 1, 1, 0.5f));
                Handles.EndGUI();
            }
        }

        #region 绘制方法

        /// <summary>
        /// 绘制时间标尺
        /// </summary>
        private void DrawTimeRuler(Rect rect)
        {
            Rect rulerRect = new Rect(0, 0, rect.width, SkillEditorStyles.TIME_RULER_HEIGHT);
            EditorGUI.DrawRect(rulerRect, new Color(0.25f, 0.25f, 0.25f));

            float visibleTimeStart = Mathf.Max(0, coords.PhysXToTime(0));
            float visibleTimeEnd = coords.PhysXToTime(rect.width);

            coords.CalculateRulerLevels(out float majorStep, out float subStep, out float gridStep, out bool isFrameIndexMode);

            float unitToTimeScale = isFrameIndexMode ? (1f / state.frameRate) : 1f;
            float loopStep = gridStep;

            float visibleUnitsStart = visibleTimeStart / unitToTimeScale;
            float visibleUnitsEnd = visibleTimeEnd / unitToTimeScale;

            float startUnit = Mathf.Floor(visibleUnitsStart / loopStep) * loopStep;

            int maxLoop = 1000;
            int currentLoop = 0;

            for (float u = startUnit; u <= visibleUnitsEnd + loopStep; u += loopStep)
            {
                if (currentLoop++ > maxLoop) break;
                if (u < 0) continue;

                float time = u * unitToTimeScale;
                float xPos = coords.TimeToPhysX(time);

                if (xPos < -50 || xPos > rect.width + 50) continue;

                bool isMajor = (coords.IsMultiple(u, majorStep));
                bool isSub = (coords.IsMultiple(u, subStep));

                if (isMajor)
                {
                    EditorGUI.DrawRect(new Rect(xPos, SkillEditorStyles.TIME_RULER_HEIGHT - 18, 1, 18), new Color(0.6f, 0.6f, 0.6f));

                    string text;
                    if (isFrameIndexMode)
                        text = Mathf.RoundToInt(u).ToString();
                    else
                        text = (u % 1.0f < 0.001f) ? u.ToString("F0") + "s" : u.ToString("F2").TrimEnd('0').TrimEnd('.') + "s";

                    GUIStyle textStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.UpperCenter,
                        normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                    };
                    GUI.Label(new Rect(xPos - 30, 2, 60, 15), text, textStyle);
                }
                else if (isSub)
                {
                    EditorGUI.DrawRect(new Rect(xPos, SkillEditorStyles.TIME_RULER_HEIGHT - 10, 1, 10), new Color(0.5f, 0.5f, 0.5f));
                }
                else
                {
                    EditorGUI.DrawRect(new Rect(xPos, SkillEditorStyles.TIME_RULER_HEIGHT - 5, 1, 5), new Color(0.35f, 0.35f, 0.35f));
                }
            }
        }

        /// <summary>
        /// 绘制轨道区域（支持分组和折叠）
        /// </summary>
        private void DrawTracksArea(Rect rect)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;




            float yOffset = 0;
            int trackIndex = 0;

            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.1607843f, 0.1607843f, 0.1607843f));

            float maxEndTime = 0;
            if (timeline.Groups != null)
            {
                float scrollOffset = state.verticalScrollOffset;
                float viewportHeight = rect.height;

                for (int i = 0; i < timeline.Groups.Count; i++)
                {
                    var group = timeline.Groups[i];

                    bool isGroupVisible = (yOffset + SkillEditorStyles.GROUP_HEIGHT > scrollOffset && yOffset < scrollOffset + viewportHeight);
                    if (isGroupVisible)
                    {
                        float drawY = yOffset - scrollOffset;
                        Rect groupRect = new Rect(0, drawY, rect.width, SkillEditorStyles.GROUP_HEIGHT);

                        EditorGUI.DrawRect(groupRect, new Color(0.13f, 0.13f, 0.13f));

                        if (groupRect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                            {
                                clipInteraction.SelectGroup(group);
                                Event.current.Use();
                            }
                        }

                        EditorGUI.DrawRect(new Rect(groupRect.x, groupRect.y + SkillEditorStyles.GROUP_HEIGHT - 1, groupRect.width, 1), new Color(0.1f, 0.1f, 0.1f));
                    }

                    yOffset += SkillEditorStyles.GROUP_HEIGHT;

                    if (!group.isCollapsed && group.tracks != null)
                    {
                        for (int j = 0; j < group.tracks.Count; j++)
                        {
                            TrackBase track = group.tracks[j];
                            if (track != null)
                            {
                                if(track.isEnabled)
                                {
                                    foreach (var clip in track.clips)
                                    {
                                        if(!clip.isEnabled)continue;
                                        maxEndTime = Mathf.Max(maxEndTime, clip.StartTime + clip.Duration);
                                    }
                                }

                                float trackHeight = SkillEditorStyles.TRACK_HEIGHT;
                                bool isTrackVisible = (yOffset + trackHeight > scrollOffset && yOffset < scrollOffset + viewportHeight);
                                if (isTrackVisible)
                                {
                                    float drawY = yOffset - scrollOffset;
                                    Rect trackRect = new Rect(0, drawY, rect.width, trackHeight);
                                    DrawTrackBackground(new Color(0.15f, 0.2f, 0.3f), new Color(0.18f, 0.23f, 0.33f),
                                        trackRect, trackIndex, track == clipInteraction.DragHoveredTrack, !group.isEnabled || !track.isEnabled);
                                    DrawClipsOnTrack(track, trackRect);
                                }
                                yOffset += trackHeight;
                                trackIndex++;
                            }
                        }
                    }
                }
            }

            DrawTimelineEndLine(rect, maxEndTime);
        }

        /// <summary>
        /// 绘制时间轴末端指示线
        /// </summary>
        private void DrawTimelineEndLine(Rect rect, float maxEndTime)
        {
            if (maxEndTime <= 0) return;

            float x = coords.TimeToPhysX(maxEndTime);
            if (x < TimelineCoordinates.TIMELINE_START_OFFSET || x > rect.width) return;

            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.5f, 1.0f, 0.8f);
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, rect.height, 0));
            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制轨道背景
        /// </summary>
        private void DrawTrackBackground(Color color1, Color color2, Rect trackRect, int index, bool isHighlighted = false, bool isDisabled = false)
        {
            Color bgColor = (index % 2 == 0) ? color1 : color2;

            if (isDisabled)
            {
                bgColor = Color.Lerp(bgColor, Color.black, 0.5f);
            }
            EditorGUI.DrawRect(trackRect, bgColor);

            if (isHighlighted)
            {
                EditorGUI.DrawRect(trackRect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
            }

            Color borderColor = new Color(0.1f, 0.1f, 0.1f);
            EditorGUI.DrawRect(new Rect(trackRect.x, trackRect.y + trackRect.height - 1, trackRect.width, 1), borderColor);
        }

        /// <summary>
        /// 绘制轨道上的片段
        /// </summary>
        private void DrawClipsOnTrack(TrackBase track, Rect trackRect)
        {
            if (track.clips == null || track.clips.Count == 0) return;



            for (int i = 0; i < track.clips.Count; i++)
            {
                ClipBase clip = track.clips[i];
                float clipStartX = coords.TimeToPhysX(clip.StartTime);
                float clipWidth = clip.Duration * state.zoom;
                if (clipStartX + clipWidth < 0 || clipStartX > trackRect.width) continue;

                Rect clipRect = new Rect(trackRect.x + clipStartX, trackRect.y + SkillEditorStyles.CLIP_MARGIN_TOP, clipWidth, SkillEditorStyles.CLIP_HEIGHT);
                
                // Get precomputed variables for Drawer
                Color clipColor = clipInteraction.GetClipColor(track.trackType);

                // Compute truncated name for Drawer
                float blendInW = clip.SupportsBlending ? (clip.BlendInDuration * state.zoom) : 0;
                float blendOutW = clip.SupportsBlending ? (clip.BlendOutDuration * state.zoom) : 0;
                Rect textRect = new Rect(clipRect.x + blendInW, clipRect.y, Mathf.Max(0, clipRect.width - blendInW - blendOutW), clipRect.height);

                string displayName = "";
                if (textRect.width > 15)
                {
                    GUIStyle clipStyle = new GUIStyle(EditorStyles.whiteLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        padding = new RectOffset(2, 2, 0, 0),
                        clipping = TextClipping.Clip
                    };
                    displayName = clipInteraction.GetTruncatedText(clip.clipName, textRect.width, clipStyle);
                }

                // Delegate rendering to Drawer
                ClipDrawer drawer = ClipDrawerFactory.GetDrawerInstance(clip);
                drawer.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);

                // Interaction injection remains in TimelineView
                clipInteraction.HandleClipInteraction(clip, clipRect);
            }
        }



        /// <summary>
        /// 绘制吸附线
        /// </summary>
        private void DrawSnapLine(Rect rect)
        {
            if (clipInteraction.CurrentSnapTime < 0 || clipInteraction.CurrentDragMode == TimelineClipInteraction.DragMode.None) return;

            float x = coords.TimeToPhysX(clipInteraction.CurrentSnapTime);
            if (x < TimelineCoordinates.TIMELINE_START_OFFSET || x > rect.width) return;

            Color snapColor = new Color(1f, 1f, 0f, 0.6f);

            for (float y = SkillEditorStyles.TIME_RULER_HEIGHT; y < rect.height; y += 10)
            {
                EditorGUI.DrawRect(new Rect(x - 1, y, 2, 5), snapColor);
            }
        }

        /// <summary>
        /// 绘制时间指针
        /// </summary>
        private void DrawTimeIndicator(Rect rect)
        {
            if (!state.ShouldShowIndicator) return;

            float x = coords.TimeToPhysX(state.timeIndicator);
            if (x < TimelineCoordinates.TIMELINE_START_OFFSET || x > rect.width) return;

            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, rect.height, 0));
            Handles.EndGUI();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理视口平移导航
        /// </summary>
        private void HandlePanning(Rect rect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && (e.button == 1 || e.button == 2) && e.mousePosition.y >= SkillEditorStyles.TIME_RULER_HEIGHT)
            {
                isPanning = true;
                panningStartMousePos = e.mousePosition;
                panningStartScrollX = state.scrollOffset;
                panningStartScrollY = state.verticalScrollOffset;
                e.Use();
            }

            if (e.type == EventType.MouseDrag && isPanning)
            {
                Vector2 delta = e.mousePosition - panningStartMousePos;
                state.scrollOffset = Mathf.Max(0, panningStartScrollX - delta.x);

                float totalHeight = state.CalculateTotalHeight();
                float maxVerticalScroll = Mathf.Max(0, totalHeight - rect.height);
                state.verticalScrollOffset = Mathf.Clamp(panningStartScrollY - delta.y, 0, maxVerticalScroll);

                e.Use();
                window.RefreshWindow();
            }

            if (e.type == EventType.MouseUp && isPanning)
            {
                float dist = Vector2.Distance(e.mousePosition, panningStartMousePos);
                if (e.button == 1 && dist < 5f)
                {
                    // 不 Use，交给 HandleMouseEvents 处理右键菜单
                }
                else
                {
                    e.Use();
                }
                isPanning = false;
            }
        }

        /// <summary>
        /// 处理右键上下文菜单
        /// </summary>
        private void ProcessContextClick(Vector2 mousePosition)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;



            float vScroll = state.verticalScrollOffset;
            float vMouseY = mousePosition.y - SkillEditorStyles.TIME_RULER_HEIGHT + vScroll;

            float currentVirtualY = 0;

            if (timeline.Groups != null)
            {
                for (int i = 0; i < timeline.Groups.Count; i++)
                {
                    var group = timeline.Groups[i];
                    currentVirtualY += SkillEditorStyles.GROUP_HEIGHT;

                    if (!group.isCollapsed && group.tracks != null)
                    {
                        for (int j = 0; j < group.tracks.Count; j++)
                        {
                            TrackBase track = group.tracks[j];
                            if (track != null)
                            {
                                if (vMouseY >= currentVirtualY && vMouseY < currentVirtualY + SkillEditorStyles.TRACK_HEIGHT)
                                {
                                    ClipBase clickedClip = null;
                                    if (track.clips != null)
                                    {
                                        for (int k = 0; k < track.clips.Count; k++)
                                        {
                                            var clip = track.clips[k];
                                            float clipStartX = coords.TimeToPhysX(clip.StartTime);
                                            float clipWidth = clip.Duration * state.zoom;
                                            float drawY = currentVirtualY - vScroll + SkillEditorStyles.TIME_RULER_HEIGHT;
                                            Rect clipRect = new Rect(clipStartX, drawY + 2, clipWidth, 36);

                                            if (clipRect.Contains(mousePosition))
                                            {
                                                clickedClip = clip;
                                                break;
                                            }
                                        }
                                    }

                                    state.pasteTargetTrack = track;
                                    state.pasteTargetTime = coords.PhysXToTime(mousePosition.x);

                                    if (clickedClip != null)
                                    {
                                        clipInteraction.ShowClipContextMenu(clickedClip);
                                    }
                                    else
                                    {
                                        clipInteraction.ShowAddClipMenu(track, mousePosition.x);
                                    }
                                    return;
                                }
                                currentVirtualY += SkillEditorStyles.TRACK_HEIGHT;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理鼠标事件
        /// </summary>
        private void HandleMouseEvents(Rect rect)
        {
            Event e = Event.current;

            // 处理刻度尺点击/拖拽（移动时间指针）
            if (!isBoxSelecting && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.mousePosition.y < SkillEditorStyles.TIME_RULER_HEIGHT)
            {
                if(state.previewTarget==null)
                {
                    Debug.LogWarning(Lan.PreviewTargetWarning);
                    return;
                } 
                state.isPreviewing = true;
                state.isStopped = false;
                float newTime = coords.PhysXToTime(e.mousePosition.x);
                newTime = Mathf.Max(0, coords.SnapTime(newTime));
                window.SeekPreview(newTime); // 播放器预览跳转
                events.OnRepaintRequest?.Invoke();
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                state.isPreviewing = false;
            }

            // 优先处理鼠标点击（轨道、片段交互）
            if (e.type == EventType.MouseDown && e.mousePosition.y >= SkillEditorStyles.TIME_RULER_HEIGHT)
            {
                SkillTimeline timeline = state.currentTimeline;
                if (timeline == null) return;

    
    
                bool clickedSomething = false;

                float vScroll = state.verticalScrollOffset;
                float vMouseY = e.mousePosition.y - SkillEditorStyles.TIME_RULER_HEIGHT + vScroll;
                float currentVirtualY = 0;

                if (timeline.Groups != null)
                {
                    for (int i = 0; i < timeline.Groups.Count; i++)
                    {
                        var group = timeline.Groups[i];
                        currentVirtualY += SkillEditorStyles.GROUP_HEIGHT;

                        if (!group.isCollapsed && group.tracks != null)
                        {
                            for (int j = 0; j < group.tracks.Count; j++)
                            {
                                TrackBase track = group.tracks[j];
                                if (track != null)
                                {
                                    if (vMouseY >= currentVirtualY && vMouseY < currentVirtualY + SkillEditorStyles.TRACK_HEIGHT)
                                    {
                                        ClipBase clickedClip = null;
                                        if (track.clips != null)
                                        {
                                            for (int k = 0; k < track.clips.Count; k++)
                                            {
                                                var clip = track.clips[k];
                                                float clipStartX = coords.TimeToPhysX(clip.StartTime);
                                                float clipWidth = clip.Duration * state.zoom;
                                                float drawY = currentVirtualY - vScroll + SkillEditorStyles.TIME_RULER_HEIGHT;
                                                Rect clipRect = new Rect(clipStartX, drawY + 5, clipWidth, 30);

                                                if (clipRect.Contains(e.mousePosition))
                                                {
                                                    clickedClip = clip;
                                                    break;
                                                }
                                            }
                                        }

                                        if (clickedClip != null)
                                        {
                                            clickedSomething = true;

                                            state.pasteTargetTrack = track;
                                            state.pasteTargetTime = coords.PhysXToTime(e.mousePosition.x);
                                        }
                                        else if (e.button == 0)
                                        {
                                            clipInteraction.SelectTrack(track);
                                            if (!e.control)
                                            {
                                                state.selectedClips.Clear();
                                            }
                                            state.selectedTrack = track;
                                            events.NotifySelectionChanged();

                                            state.pasteTargetTrack = track;
                                            state.pasteTargetTime = coords.PhysXToTime(e.mousePosition.x);

                                            isBoxSelecting = true;
                                            boxSelectStart = e.mousePosition;
                                            boxSelectEnd = e.mousePosition;

                                            clickedSomething = true;
                                            e.Use();
                                        }
                                    }

                                    if (clickedSomething) break;
                                    currentVirtualY += SkillEditorStyles.TRACK_HEIGHT;
                                }
                            }
                            if (clickedSomething) break;
                        }
                    }
                }

                // 没点击到任何东西，左键开始框选
                if (!clickedSomething && e.button == 0)
                {
                    if (!e.control)
                    {
                        state.ClearSelection();
                        Selection.activeObject = null;
                    }
                    isBoxSelecting = true;
                    boxSelectStart = e.mousePosition;
                    boxSelectEnd = e.mousePosition;
                    e.Use();
                }
            }

            // 框选拖拽
            if (e.type == EventType.MouseDrag && isBoxSelecting)
            {
                boxSelectEnd = e.mousePosition;
                window.RefreshWindow();
                e.Use();
            }

            // 框选结束
            if (e.type == EventType.MouseUp && isBoxSelecting)
            {
                float dragDist = Vector2.Distance(boxSelectStart, boxSelectEnd);
                bool isRealDrag = dragDist > 2.0f;

                Rect selectionRect = coords.GetRectFromPoints(boxSelectStart, boxSelectEnd);

                SkillTimeline timeline = state.currentTimeline;
                if (timeline != null && isRealDrag)
                {
                    state.selectedTrack = null;

                    if (!e.control)
                    {
                        state.selectedClips.Clear();
                    }

        
        
                    float yOffset = SkillEditorStyles.TIME_RULER_HEIGHT;

                    System.Action<TrackBase> checkBoxTrack = (track) => {
                        Rect trackRect = new Rect(0, yOffset, rect.width, SkillEditorStyles.TRACK_HEIGHT);
                        foreach (var clip in track.clips)
                        {
                            float clipStartX = coords.TimeToPhysX(clip.StartTime);
                            float clipWidth = clip.Duration * state.zoom;
                            Rect clipRect = new Rect(trackRect.x + clipStartX, trackRect.y + 5, clipWidth, 30);

                            if (selectionRect.Overlaps(clipRect))
                            {
                                if (!state.selectedClips.Contains(clip))
                                {
                                    state.selectedClips.Add(clip);
                                }
                            }
                        }
                        yOffset += SkillEditorStyles.TRACK_HEIGHT;
                    };

                    if (timeline.Groups != null)
                    {
                        foreach (var group in timeline.Groups)
                        {
                            yOffset += SkillEditorStyles.GROUP_HEIGHT;
                            if (!group.isCollapsed && group.tracks != null)
                            {
                                foreach (var track in group.tracks)
                                {
                                    if (track != null) checkBoxTrack(track);
                                }
                            }
                        }
                    }

                    Debug.Log($"[技能编辑器] 框选结束，选中了 {state.selectedClips.Count} 个片段");
                }

                isBoxSelecting = false;
                events.NotifySelectionChanged();
                e.Use();
            }

            // 右键菜单
            if (e.type == EventType.MouseUp && e.button == 1)
            {
                ProcessContextClick(e.mousePosition);
                e.Use();
            }

            // Delete 键
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                clipOps.DeleteSelectedClip();
                e.Use();
            }

            // Ctrl+A 全选
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.A && (e.control || e.command))
            {
                SkillTimeline timeline = state.currentTimeline;
                if (timeline != null)
                {
                    state.selectedClips.Clear();

                    foreach (var track in timeline.AllTracks)
                    {
                        foreach (var clip in track.clips)
                        {
                            state.selectedClips.Add(clip);
                        }
                    }

                    Debug.Log($"[技能编辑器] 全选了 {state.selectedClips.Count} 个片段");
                    events.NotifySelectionChanged();
                }
                e.Use();
            }

            // Ctrl+C 复制
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && (e.control || e.command))
            {
                clipOps.CopySelectedClips();
                e.Use();
            }

            // Ctrl+V 粘贴
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.V && (e.control || e.command))
            {
                clipOps.PasteClips();
                e.Use();
            }

            // 鼠标滚轮缩放
            if (e.type == EventType.ScrollWheel && (e.control || e.command))
            {
                float mouseTime = coords.PhysXToTime(e.mousePosition.x);

                float zoomStep = Mathf.Max(5f, state.zoom * 0.15f);
                float zoomDelta = Mathf.Sign(-e.delta.y) * zoomStep;

                float newZoom = Mathf.Clamp(state.zoom + zoomDelta, SkillEditorStyles.MIN_ZOOM, SkillEditorStyles.MAX_ZOOM);
                state.zoom = newZoom;

                state.scrollOffset = (mouseTime * state.zoom + TimelineCoordinates.TIMELINE_START_OFFSET) - e.mousePosition.x;
                state.scrollOffset = Mathf.Max(0, state.scrollOffset);

                e.Use();
                events.OnRepaintRequest?.Invoke();
            }

            // 鼠标滚轮水平平移 (Shift)
            if (e.type == EventType.ScrollWheel && e.shift)
            {
                // 有些鼠标系统会自动将 Shift+Scroll 映射为 delta.x，这里兼容处理
                float delta = Mathf.Abs(e.delta.x) > 0.01f ? e.delta.x : e.delta.y;
                float scrollSensitivity = 20f;
                state.scrollOffset += delta * scrollSensitivity;
                state.scrollOffset = Mathf.Max(0, state.scrollOffset);
                e.Use();
                events.OnRepaintRequest?.Invoke();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取当前缩放级别
        /// </summary>
        public float GetZoom()
        {
            return coords.GetZoom();
        }

        /// <summary>
        /// 重置缩放倍数
        /// </summary>
        public void ResetZoom()
        {
            state.ResetView();
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 清除片段选中状态（由外部调用）
        /// </summary>
        public void ClearClipSelection()
        {
            clipInteraction.ClearClipSelection();
        }

        /// <summary>
        /// 失去焦点时的回调
        /// </summary>
        public void OnLostFocus()
        {
            clipInteraction.ClearClipSelection();
        }

        #endregion
    }
}
