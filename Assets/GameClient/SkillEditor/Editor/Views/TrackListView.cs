using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 轨道列表视图
    /// </summary>
    public class TrackListView
    {
        private SkillEditorWindow window;
        private SkillEditorState state;
        private SkillEditorEvents events;
        
        // 常量定义 (已迁移至 SkillEditorStyles)
        
        // 重命名状态
        private TrackBase renamingTrack = null;
        private Group renamingGroup = null;
        private string renamingText = "";
        private bool needsFocusOnRename = false;
        
        // 分组拖拽
        private int dropTargetIndex = -1;
        private string dropTargetGroupId = null;
        private string hoveredGroupId = null;
        private float dropIndicatorY = -1f;
        private bool isDropAfter = false;


        public TrackListView(SkillEditorWindow window, SkillEditorState state, SkillEditorEvents events)
        {
            this.window = window;
            this.state = state;
            this.events = events;
        }
        /// <summary>
        /// 失去焦点时调用
        /// </summary>
        public void OnLostFocus() 
        {
            // 点击了空白区域
            if (renamingTrack != null)
            {
                EndRenaming(true);
            }
            if (state.selectedTrack != null || state.selectedGroup != null)
            {
                state.ClearSelection();
                events.NotifySelectionChanged();
            }
        }
        /// <summary>
        /// 结束重命名状态
        /// </summary>
        /// <param name="save">是否保存更改</param>
        public void EndRenaming(bool save)
        {
            if (renamingTrack != null)
            {
                if (save)
                {
                    window.RecordUndo("重命名轨道");
                    renamingTrack.trackName = renamingText;
                }
                renamingTrack = null;
            }
            else if (renamingGroup != null)
            {
                if (save)
                {
                    window.RecordUndo("重命名分组");
                    renamingGroup.groupName = renamingText;
                }
                renamingGroup = null;
            }
            
            needsFocusOnRename = false;
            GUI.FocusControl(null);
            events.OnRepaintRequest?.Invoke();
        }
        
        public void DoGUI()
        {

            
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;
            
            Event evt = Event.current;
            
            // 1. 全局处理重命名模式的按键
            if (renamingTrack != null || renamingGroup != null)
            {
                if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                {
                    EndRenaming(true);
                    evt.Use();
                    return;
                }
                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
                {
                    EndRenaming(false);
                    evt.Use();
                    return;
                }
            }
            
            // 2. 处理快捷键 (Ctrl+D)
            HandleShortcuts();
            
            float viewportHeight = window.position.height - SkillEditorStyles.HEADER_HEIGHT;
            float scrollOffset = state.verticalScrollOffset;

            // 2. 绘制轨道列表内容（带裁剪区域）
            Rect contentGroupRect = new Rect(0, SkillEditorStyles.HEADER_HEIGHT, SkillEditorStyles.TRACK_LIST_WIDTH, viewportHeight);
            GUI.BeginGroup(contentGroupRect);
            {
                float virtualY = 0; 
                DrawTrackListInGroup(ref virtualY, scrollOffset, viewportHeight, SkillEditorStyles.TRACK_LIST_WIDTH);
                
                // 处理拖拽指示线
                if (dropIndicatorY >= 0)
                {
                    DrawDropIndicator(dropIndicatorY - scrollOffset);
                }
            }
            GUI.EndGroup();

            // 3. 处理鼠标事件
            HandleDragAndDrop(new Rect(0, SkillEditorStyles.HEADER_HEIGHT, SkillEditorStyles.TRACK_LIST_WIDTH, viewportHeight), SkillEditorStyles.HEADER_HEIGHT);

            if (evt.type == EventType.MouseDown)
            {
                float totalHeight = state.CalculateTotalHeight() - scrollOffset;
                if (evt.mousePosition.x < SkillEditorStyles.TRACK_LIST_WIDTH && evt.mousePosition.y > totalHeight)
                {
                    if (evt.button == 0)
                    {
                        OnLostFocus();
                        evt.Use();
                    }
                    else if (evt.button == 1)
                    {
                        ShowGlobalContextMenu();
                        evt.Use();
                    }
                }
            }

            // 4. 绘制标题栏
            DrawHeader(SkillEditorStyles.TRACK_LIST_WIDTH, SkillEditorStyles.HEADER_HEIGHT);
        }

        private void DrawHeader(float width, float height)
        {
            Rect headerRect = new Rect(0, 0, width, height);
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f));
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 0, 0, 0)
            };
            GUI.Label(new Rect(0, 0, 150, height), "轨道列表", titleStyle);
            
            if (GUI.Button(new Rect(width - height, 0, height, height), "+"))
            {
                CreateNewGroup();
            }
            
            EditorGUI.DrawRect(new Rect(0, height - 1, width, 1), new Color(0.1f, 0.1f, 0.1f));
        }

        private void DrawTrackListInGroup(ref float virtualY, float scrollOffset, float viewportHeight, float width)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            if (timeline.Groups != null)
            {
                for (int i = 0; i < timeline.Groups.Count; i++)
                {
                    DrawGroup(timeline.Groups[i], ref virtualY, scrollOffset, viewportHeight, 0);
                }
            }
        }

        private void DrawGroup(Group group, ref float virtualY, float scrollOffset, float viewportHeight, float baseOffset)
        {

            
            bool isVisible = (virtualY + SkillEditorStyles.GROUP_HEIGHT > scrollOffset && virtualY < scrollOffset + viewportHeight);
            float drawY = virtualY - scrollOffset + baseOffset;
            Rect groupRect = new Rect(0, drawY, SkillEditorStyles.TRACK_LIST_WIDTH, SkillEditorStyles.GROUP_HEIGHT);
            
            if (isVisible)
            {
                Color bgColor = new Color(0.15f, 0.15f, 0.15f);
                if (group == state.selectedGroup) bgColor = new Color(0.25f, 0.35f, 0.55f);
                else if (group.groupId == hoveredGroupId) bgColor = new Color(0.3f, 0.5f, 0.8f, 0.4f);
                if (!group.isEnabled) bgColor.a *= 0.5f;
                EditorGUI.DrawRect(groupRect, bgColor);
                
                string arrowIcon = group.isCollapsed ? "▶" : "▼";
                if (GUI.Button(new Rect(4, drawY, 16, SkillEditorStyles.GROUP_HEIGHT), arrowIcon, EditorStyles.label))
                {
                    group.isCollapsed = !group.isCollapsed;
                    window.RefreshWindow();
                }
                
                Rect labelRect = new Rect(22, drawY, SkillEditorStyles.TRACK_LIST_WIDTH - 26, SkillEditorStyles.GROUP_HEIGHT);
                if (renamingGroup == group)
                {
                    GUI.SetNextControlName("GroupRename");
                    if (needsFocusOnRename && Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.FocusTextInControl("GroupRename");
                        needsFocusOnRename = false;
                    }
                    float fieldHeight = 18f;
                    Rect fieldRect = new Rect(labelRect.x, labelRect.y + (SkillEditorStyles.GROUP_HEIGHT - fieldHeight) / 2, labelRect.width, fieldHeight);
                    renamingText = EditorGUI.TextField(fieldRect, renamingText);
                }
                else
                {
                    GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
                    if (!group.isEnabled) nameStyle.normal.textColor = Color.gray;
                    GUI.Label(labelRect, group.groupName, nameStyle);
                }
                
                // 交互
                Event e = Event.current;
                if (groupRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        if (e.button == 0)
                        {
                            SelectGroup(group);
                            if (e.clickCount == 2)
                            {
                                renamingGroup = group;
                                renamingText = group.groupName;
                                needsFocusOnRename = true;
                            }
                            e.Use();
                        }
                        else if (e.button == 1)
                        {
                            ShowGroupContextMenu(group);
                            e.Use();
                        }
                    }
                    else if (e.type == EventType.MouseDrag && renamingGroup == null)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("DraggingGroup", group);
                        DragAndDrop.objectReferences = new UnityEngine.Object[0];
                        DragAndDrop.StartDrag(group.groupName);
                        e.Use();
                    }
                }
            }
            
            virtualY += SkillEditorStyles.GROUP_HEIGHT;
            
            // 树状结构：直接遍历 group.tracks
            if (!group.isCollapsed && group.tracks != null)
            {
                for (int i = 0; i < group.tracks.Count; i++)
                {
                    TrackBase track = group.tracks[i];
                    if (track != null) DrawTrackItem(track, ref virtualY, scrollOffset, viewportHeight, baseOffset);
                }
            }
        }

        private void DrawTrackItem(TrackBase track, ref float virtualY, float scrollOffset, float viewportHeight, float baseOffset)
        {

            if (virtualY + SkillEditorStyles.TRACK_HEIGHT < scrollOffset || virtualY > scrollOffset + viewportHeight)
            {
                virtualY += SkillEditorStyles.TRACK_HEIGHT;
                return;
            }

            float drawY = virtualY - scrollOffset + baseOffset;
            Rect trackRect = new Rect(0, drawY, SkillEditorStyles.TRACK_LIST_WIDTH, SkillEditorStyles.TRACK_HEIGHT);
            
            Color bgColor = (state.selectedTrack == track) ? new Color(0.25f, 0.35f, 0.55f) : new Color(0.2f, 0.2f, 0.2f);
            if (!track.isEnabled) bgColor.a *= 0.5f;
            EditorGUI.DrawRect(trackRect, bgColor);
            
            GUI.Label(new Rect(4, drawY + 8, 24, 24), GetTrackIcon(track.trackType));
            
            Rect textRect = new Rect(32, drawY, SkillEditorStyles.TRACK_LIST_WIDTH - 36, SkillEditorStyles.TRACK_HEIGHT);
            if (renamingTrack == track)
            {
                GUI.SetNextControlName("TrackRename");
                if (needsFocusOnRename && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.FocusTextInControl("TrackRename");
                    needsFocusOnRename = false;
                }
                float fieldHeight = 18f;
                Rect fieldRect = new Rect(textRect.x, textRect.y + (SkillEditorStyles.TRACK_HEIGHT - fieldHeight) / 2, textRect.width, fieldHeight);
                renamingText = EditorGUI.TextField(fieldRect, renamingText);
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
                if (!track.isEnabled) labelStyle.normal.textColor = Color.gray;
                GUI.Label(textRect, track.trackName, labelStyle);
            }
            
            // 交互
            Event evt = Event.current;
            if (trackRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.MouseDown)
                {
                    if (evt.button == 0)
                    {
                        SelectTrack(track);
                        if (evt.clickCount == 2)
                        {
                            renamingTrack = track;
                            renamingText = track.trackName;
                            needsFocusOnRename = true;
                        }
                        evt.Use();
                    }
                    else if (evt.button == 1)
                    {
                        ShowTrackContextMenu(track);
                        evt.Use();
                    }
                }
                else if (evt.type == EventType.MouseDrag && renamingTrack == null)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("DraggingTrack", track);
                    DragAndDrop.objectReferences = new UnityEngine.Object[0];
                    DragAndDrop.StartDrag(track.trackName);
                    evt.Use();
                }
            }
            
            EditorGUI.DrawRect(new Rect(0, drawY + SkillEditorStyles.TRACK_HEIGHT - 1, SkillEditorStyles.TRACK_LIST_WIDTH, 1), new Color(0.1f, 0.1f, 0.1f));
            virtualY += SkillEditorStyles.TRACK_HEIGHT;
        }

        #region 分组管理
        
        /// <summary>
        /// 创建新分组
        /// </summary>
        private void CreateNewGroup()
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            window.RecordUndo("添加分组");
            Group newGroup = new Group($"分组 {timeline.Groups.Count + 1}");
            timeline.Groups.Add(newGroup);
            
            Debug.Log($"[技能编辑器] 创建分组: {newGroup.groupName}");
            events.OnRepaintRequest?.Invoke();
        }
        
        /// <summary>
        /// 显示分组右键菜单
        /// </summary>
        private void ShowGroupContextMenu(Group group)
        {
            GenericMenu menu = new GenericMenu();
            
            // 动态生成添加轨道菜单
            var registeredTracks = TrackRegistry.GetRegisteredTracks();
            foreach (var trackInfo in registeredTracks)
            {
                // 使用 MenuPath (例如 "添加轨道/动画轨道")
                string menuPath = Lan.AddTrackMenuItem + "/" + trackInfo.Attribute.DisplayName; 
                // if (!string.IsNullOrEmpty(trackInfo.Attribute.MenuPath))
                // {
                //     menuPath = trackInfo.Attribute.MenuPath;
                // }
                
                // 捕获变量
                Type type = trackInfo.TrackType;
                menu.AddItem(new GUIContent(menuPath), false, () => OnAddTrackToGroup(group, type));
            }
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent(group.isEnabled ? "禁用分组" : "启用分组"), false, () =>
            {
                window.RecordUndo("切换分组状态");
                group.isEnabled = !group.isEnabled;
                window.RefreshWindow();
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("复制分组"), false, () => CopyGroup(group));
            
            if (state.copiedTrack != null)
            {
                menu.AddItem(new GUIContent($"粘贴轨道 ({state.copiedTrack.trackName})"), false, () => PasteTrack(group));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴轨道"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("删除分组"), false, () => DeleteGroup(group, true));
            
            menu.ShowAsContext();
        }

        /// <summary>
        /// 全局空白区域菜单
        /// </summary>
        private void ShowGlobalContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("新建分组"), false, CreateNewGroup);
            
            if (state.copiedGroup != null)
            {
                menu.AddItem(new GUIContent($"粘贴分组 ({state.copiedGroup.groupName})"), false, PasteGroup);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴分组"));
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// 添加轨道到指定分组（树状结构：直接加入 group.tracks）
        /// </summary>
        private void OnAddTrackToGroup(Group group, Type trackType)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            window.RecordUndo("添加轨道");
            TrackBase newTrack = CreateTrack(trackType);
            if (newTrack == null) return;
            
            // 树状结构：轨道直接加入分组
            group.tracks.Add(newTrack);
            
            // 更新缓存
            state.AddTrackToCache(newTrack);
            
            Debug.Log($"[技能编辑器] 成功添加轨道到分组: {newTrack.trackName} -> {group.groupName}");
            events.OnRepaintRequest?.Invoke();
        }
        
        /// <summary>
        /// 根据类型创建轨道实例
        /// </summary>
        private TrackBase CreateTrack(Type trackType)
        {
            try 
            {
                TrackBase newTrack = (TrackBase)Activator.CreateInstance(trackType);
                if (newTrack is AnimationTrack newAnimationTrack && state?.currentTimeline != null)
                {
                    bool hasExistingAnimationTrack = false;
                    foreach (TrackBase existingTrack in state.currentTimeline.AllTracks)
                    {
                        if (existingTrack is AnimationTrack)
                        {
                            hasExistingAnimationTrack = true;
                            break;
                        }
                    }

                    if (!hasExistingAnimationTrack)
                    {
                        newAnimationTrack.isMasterTrack = true;
                    }
                }

                return newTrack;
            }
            catch (Exception e)
            {
                Debug.LogError($"[技能编辑器] 创建轨道失败: {trackType.Name}, 错误: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 删除轨道（树状结构：从所属 group.tracks 移除）
        /// </summary>
        private void DeleteTrack(TrackBase track, bool showDialog = false)
        {
            if (showDialog && !EditorUtility.DisplayDialog("确认删除", $"确定要删除轨道 '{track.trackName}' 吗？", "删除", "取消"))
                return;

            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            window.RecordUndo("删除轨道");

            // 树状结构：从所属分组中移除
            Group parentGroup = timeline.FindGroupContainingTrack(track);
            if (parentGroup != null)
            {
                parentGroup.tracks.Remove(track);
            }

            // 从缓存中移除
            state.RemoveTrackFromCache(track.trackId);

            // 清除选中状态
            if (state.selectedTrack == track)
            {
                state.selectedTrack = null;
                Selection.activeObject = null;
            }

            Debug.Log($"[技能编辑器] 已删除轨道: {track.trackName}");
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 删除分组（树状结构：分组内轨道随之一起删除）
        /// </summary>
        private void DeleteGroup(Group group, bool showDialog = true)
        {
            if (showDialog && !EditorUtility.DisplayDialog("确认删除", $"确定要删除分组 '{group.groupName}' 及其内部所有轨道吗？", "删除", "取消"))
                return;

            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            window.RecordUndo("删除分组");

            // 树状结构：从缓存中移除所有子轨道
            if (group.tracks != null)
            {
                foreach (var track in group.tracks)
                {
                    state.RemoveTrackFromCache(track.trackId);
                }
            }
            
            // 从分组列表中删除（轨道随之一起删除）
            timeline.Groups.Remove(group);

            // 清除选中状态
            if (state.selectedGroup == group)
            {
                state.selectedGroup = null;
                Selection.activeObject = null;
            }

            Debug.Log($"[技能编辑器] 已删除分组: {group.groupName}");
            events.OnRepaintRequest?.Invoke();
        }
        
        /// <summary>
        /// 根据ID查找轨道
        /// </summary>
        private TrackBase FindTrackById(string id)
        {
            return state.GetTrackById(id);
        }
        
        #region 拖拽处理
        
        private void HandleDragAndDrop(Rect rect, float headerHeight)
        {
            Event e = Event.current;
            
            switch (e.type)
            {
                case EventType.DragUpdated:
                    if (rect.Contains(e.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        UpdateDropTarget(e.mousePosition.y - headerHeight + state.verticalScrollOffset);
                        events.OnRepaintRequest?.Invoke();
                        e.Use();
                    }
                    break;
                case EventType.DragPerform:
                    if (rect.Contains(e.mousePosition))
                    {
                        ExecuteDrop();
                        DragAndDrop.AcceptDrag();
                        ClearDropTarget();
                        events.OnRepaintRequest?.Invoke();
                        e.Use();
                    }
                    break;
                case EventType.DragExited:
                    ClearDropTarget();
                    window.RefreshWindow();
                    break;
            }
        }
        
        private void UpdateDropTarget(float virtualMouseY)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;
            
            float y = 0f;
            dropTargetIndex = -1;
            dropTargetGroupId = null;
            hoveredGroupId = null;
            dropIndicatorY = -1f;

            bool isDraggingGroup = DragAndDrop.GetGenericData("DraggingGroup") != null;
            bool isDraggingTrack = DragAndDrop.GetGenericData("DraggingTrack") != null;
            
            if (timeline.Groups != null)
            {
                for (int i = 0; i < timeline.Groups.Count; i++)
                {
                    var group = timeline.Groups[i];
                    float groupTop = y;
                    float groupBottom = y + SkillEditorStyles.GROUP_HEIGHT;
                    
                    if (virtualMouseY >= groupTop && virtualMouseY <= groupBottom)
                    {
                        if (isDraggingTrack)
                        {
                            hoveredGroupId = group.groupId;
                        }
                        else if (isDraggingGroup)
                        {
                            dropTargetIndex = i;
                            isDropAfter = virtualMouseY > (groupTop + SkillEditorStyles.GROUP_HEIGHT / 2);
                            dropIndicatorY = isDropAfter ? groupBottom : groupTop;
                        }
                        return;
                    }
                    y += SkillEditorStyles.GROUP_HEIGHT;
                    
                    // 树状结构：直接遍历 group.tracks
                    if (!group.isCollapsed && group.tracks != null)
                    {
                        for (int j = 0; j < group.tracks.Count; j++)
                        {
                            float trackTop = y;
                            float trackBottom = y + SkillEditorStyles.TRACK_HEIGHT;
                            if (virtualMouseY >= trackTop && virtualMouseY <= trackBottom)
                            {
                                if (isDraggingTrack)
                                {
                                    dropTargetIndex = j;
                                    dropTargetGroupId = group.groupId;
                                    isDropAfter = virtualMouseY > (trackTop + SkillEditorStyles.TRACK_HEIGHT / 2);
                                    dropIndicatorY = isDropAfter ? trackBottom : trackTop;
                                }
                                return;
                            }
                            y += SkillEditorStyles.TRACK_HEIGHT;
                        }
                    }
                }
            }
            
            if (isDraggingGroup)
            {
                float listBottom = y;
                if (virtualMouseY > listBottom - 10f)
                {
                    dropTargetIndex = timeline.Groups.Count - 1;
                    isDropAfter = true;
                    dropIndicatorY = listBottom;
                }
            }
        }
        
        private void ExecuteDrop()
        {
            SkillTimeline timeline = window.GetCurrentTimeline();
            
            TrackBase track = DragAndDrop.GetGenericData("DraggingTrack") as TrackBase;
            if (track != null)
            {
                string finalGroupId = !string.IsNullOrEmpty(hoveredGroupId) ? hoveredGroupId : dropTargetGroupId;
                if (string.IsNullOrEmpty(finalGroupId))
                {
                    Debug.LogWarning("[技能编辑器] 放弃放置：轨道不能脱离分组存在。");
                    return;
                }
                
                window.RecordUndo("移动轨道");

                // 树状结构：从旧分组中移除
                Group oldGroup = timeline.FindGroupContainingTrack(track);
                oldGroup?.tracks.Remove(track);
                
                // 插入新位置
                var targetGroup = timeline.Groups.Find(g => g.groupId == finalGroupId);
                if (targetGroup != null)
                {
                    int insertAt;
                    if (!string.IsNullOrEmpty(hoveredGroupId))
                    {
                        // 并入逻辑：放到末尾
                        insertAt = targetGroup.tracks.Count;
                    }
                    else
                    {
                        // 调序逻辑
                        insertAt = dropTargetIndex + (isDropAfter ? 1 : 0);
                    }
                    insertAt = Mathf.Clamp(insertAt, 0, targetGroup.tracks.Count);
                    // 防重
                    targetGroup.tracks.Remove(track);
                    if (insertAt > targetGroup.tracks.Count) insertAt = targetGroup.tracks.Count;
                    targetGroup.tracks.Insert(insertAt, track);
                }
                return;
            }

            Group group = DragAndDrop.GetGenericData("DraggingGroup") as Group;
            if (group != null && dropTargetIndex != -1)
            {
                window.RecordUndo("移动分组");
                timeline.Groups.Remove(group);
                int insertAt2 = dropTargetIndex + (isDropAfter ? 1 : 0);
                if (insertAt2 > timeline.Groups.Count) insertAt2 = timeline.Groups.Count;
                timeline.Groups.Insert(insertAt2, group);
            }
        }
        
        private void ClearDropTarget()
        {
            dropTargetIndex = -1;
            dropTargetGroupId = null;
            hoveredGroupId = null;
            dropIndicatorY = -1f;
        }
        
        private void DrawDropIndicator(float y)
        {
            Rect indicatorRect = new Rect(0, y - 1, 200, 2);
            EditorGUI.DrawRect(indicatorRect, new Color(0.3f, 0.6f, 1f, 0.8f));
        }

        #endregion
        
        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 显示轨道右键菜单
        /// </summary>
        private void ShowTrackContextMenu(TrackBase track)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent(track.isEnabled ? "禁用轨道" : "启用轨道"), false, () =>
            {
                window.RecordUndo("切换轨道状态");
                track.isEnabled = !track.isEnabled;
                window.RefreshWindow();
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("重命名"), false, () =>
            {
                renamingTrack = track;
                renamingText = track.trackName;
                needsFocusOnRename = true;
                window.RefreshWindow();
            });
            
            menu.AddItem(new GUIContent("复制轨道"), false, () => CopyTrack(track));
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("删除轨道"), false, () => DeleteTrack(track, false));
            
            menu.ShowAsContext();
        }

        #region 复制粘贴业务逻辑

        private void CopyTrack(TrackBase track)
        {
            state.copiedTrack = track.Clone();
            Debug.Log($"[技能编辑器] 已复制轨道: {track.trackName}");
        }

        /// <summary>
        /// 粘贴轨道到目标分组（树状结构：直接加入 group.tracks）
        /// </summary>
        private void PasteTrack(Group targetGroup)
        {
            if (state.copiedTrack == null || targetGroup == null) return;
            
            window.RecordUndo("粘贴轨道");
            TrackBase newTrack = state.copiedTrack.Clone();
            newTrack.trackId = Guid.NewGuid().ToString();
            
            // 修正片段 ID
            foreach (var clip in newTrack.clips)
            {
                clip.clipId = Guid.NewGuid().ToString();
            }

            // 树状结构：直接加入分组
            targetGroup.tracks.Add(newTrack);
            state.AddTrackToCache(newTrack);
            
            Debug.Log($"[技能编辑器] 已粘贴轨道: {newTrack.trackName} -> {targetGroup.groupName}");
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 复制分组（树状结构：直接遍历 group.tracks）
        /// </summary>
        private void CopyGroup(Group group)
        {
            state.copiedGroup = group.Clone();
            state.copiedTracksForGroup.Clear();
            
            if (group.tracks != null)
            {
                foreach (var track in group.tracks)
                {
                    state.copiedTracksForGroup.Add(track.Clone());
                }
            }
            Debug.Log($"[技能编辑器] 已复制分组: {group.groupName} (包含 {state.copiedTracksForGroup.Count} 条轨道)");
        }

        /// <summary>
        /// 粘贴分组（树状结构：轨道直接加入新分组）
        /// </summary>
        private void PasteGroup()
        {
            if (state.copiedGroup == null) return;
            
            window.RecordUndo("粘贴分组");
            SkillTimeline timeline = state.currentTimeline;
            
            // 克隆分组
            Group newGroup = state.copiedGroup.Clone();
            newGroup.groupId = Guid.NewGuid().ToString();
            newGroup.tracks = new List<TrackBase>();
            timeline.Groups.Add(newGroup);
            
            // 克隆并关联轨道
            foreach (var copiedTrack in state.copiedTracksForGroup)
            {
                TrackBase newTrack = copiedTrack.Clone();
                newTrack.trackId = Guid.NewGuid().ToString();
                
                // 修正片段 ID
                foreach (var clip in newTrack.clips)
                {
                    clip.clipId = Guid.NewGuid().ToString();
                }

                newGroup.tracks.Add(newTrack);
                state.AddTrackToCache(newTrack);
            }
            
            Debug.Log($"[技能编辑器] 已粘贴分组: {newGroup.groupName} (包含 {newGroup.tracks.Count} 条轨道)");
            events.OnRepaintRequest?.Invoke();
        }

        #endregion

        /// <summary>
        /// 获取轨道图标
        /// </summary>
        /// <summary>
        /// 获取轨道图标
        /// </summary>
        private GUIContent GetTrackIcon(string trackType)
        {
            string iconName = TrackRegistry.GetTrackIcon(trackType);
            if (string.IsNullOrEmpty(iconName))
            {
                return EditorGUIUtility.IconContent("ScriptableObject Icon");
            }
            return EditorGUIUtility.IconContent(iconName);
        }

        /// <summary>
        /// 选中轨道
        /// </summary>
        private void SelectTrack(TrackBase track)
        {
            state.isTimelineSelected = false;
            state.selectedGroup = null;
            state.selectedTrack = track;
            state.selectedClips.Clear();
            
            state.pasteTargetTrack = track;
            
            events.NotifySelectionChanged();

            SkillTimeline timeline = state.currentTimeline;
            TrackObject trackObj = TrackObject.Create(track, timeline);
            Selection.activeObject = trackObj;
            Debug.Log($"[技能编辑器] 选中轨道: {track.trackName}");
        }

        /// <summary>
        /// 清除选中的轨道
        /// </summary>
        private void ClearTrackSelection()
        {
            state.selectedTrack = null;
            Selection.activeObject = null;
            events.NotifySelectionChanged();
        }

        /// <summary>
        /// 选中分组
        /// </summary>
        private void SelectGroup(Group group)
        {
            state.ClearSelection();
            state.selectedGroup = group;
            
            events.NotifySelectionChanged();

            SkillTimeline timeline = state.currentTimeline;
            GroupObject groupObj = GroupObject.Create(group, timeline);
            Selection.activeObject = groupObj;
            Debug.Log($"[技能编辑器] 选中分组: {group.groupName}");
        }

        #region 快捷键处理

        private void HandleShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.D)
            {
                if (state.selectedGroup != null)
                {
                    DuplicateGroup(state.selectedGroup);
                    e.Use();
                }
                else if (state.selectedTrack != null)
                {
                    DuplicateTrack(state.selectedTrack);
                    e.Use();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                if (state.selectedGroup != null)
                {
                    DeleteGroup(state.selectedGroup, true);
                    e.Use();
                }
                else if (state.selectedTrack != null)
                {
                    DeleteTrack(state.selectedTrack, false);
                    e.Use();
                }
            }
        }

        /// <summary>
        /// 克隆分组（树状结构：使用 DeepClone）
        /// </summary>
        private void DuplicateGroup(Group sourceGroup)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            window.RecordUndo("克隆分组");
            
            int sourceIndex = timeline.Groups.IndexOf(sourceGroup);
            if (sourceIndex < 0) return;

            // 使用 DeepClone 一步完成
            Group newGroup = sourceGroup.DeepClone();
            newGroup.groupId = Guid.NewGuid().ToString();
            
            // 重新分配所有 ID
            foreach (var track in newGroup.tracks)
            {
                track.trackId = Guid.NewGuid().ToString();
                foreach (var clip in track.clips)
                {
                    clip.clipId = Guid.NewGuid().ToString();
                }
            }

            // 插入到当前分组下方
            timeline.Groups.Insert(sourceIndex + 1, newGroup);
            
            state.RebuildTrackCache();
            SelectGroup(newGroup);
            
            Debug.Log($"[技能编辑器] 已完成分组克隆: {newGroup.groupName}");
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 克隆轨道（树状结构：从 FindGroupContainingTrack 查找分组）
        /// </summary>
        private void DuplicateTrack(TrackBase sourceTrack)
        {
            SkillTimeline timeline = state.currentTimeline;
            if (timeline == null) return;

            // 树状结构：查找所在分组
            Group parentGroup = timeline.FindGroupContainingTrack(sourceTrack);
            if (parentGroup == null) return;

            window.RecordUndo("克隆轨道");

            TrackBase newTrack = sourceTrack.Clone();
            newTrack.trackId = Guid.NewGuid().ToString();

            foreach (var clip in newTrack.clips)
            {
                clip.clipId = Guid.NewGuid().ToString();
            }

            // 树状结构：插入到同组选中轨道下方
            int sourceIdx = parentGroup.tracks.IndexOf(sourceTrack);
            parentGroup.tracks.Insert(sourceIdx + 1, newTrack);

            state.RebuildTrackCache();
            SelectTrack(newTrack);

            Debug.Log($"[技能编辑器] 已完成轨道克隆: {newTrack.trackName}");
            events.OnRepaintRequest?.Invoke();
        }

        #endregion

        /// <summary>
        /// 对外清除选中接口
        /// </summary>
        public void ClearSelection()
        {
            state.ClearSelection();
            events.NotifySelectionChanged();
        }

        #endregion
    }
}
