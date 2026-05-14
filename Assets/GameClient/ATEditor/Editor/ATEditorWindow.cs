using UnityEngine;
using UnityEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 编辑器主窗口
    /// </summary>
    public partial class ATEditorWindow : EditorWindow
    {
        #region 私有字段

        // 子视囀
        // 布局与视窗引用的视图
        private ToolbarView toolbarView;
        private TimelineView timelineView;
        private TrackListView trackListView;

        // Inspector 包装器缓孀(用于保持 Inspector 焦点稳定)
        private ClipObject cachedClipWrapper;
        private TrackObject cachedTrackWrapper;
        private GroupObject cachedGroupWrapper;

        // 核心解耦组什
        private ATEditorState state;
        private ATEditorEvents events;

        // 窗口布局参数
        private const float TOOLBAR_HEIGHT = 30f;
        private const float TRACK_LIST_WIDTH = 200f;

        // 滚动位置
        private Vector2 scrollPosition;
        
        #endregion

        #region Unity 生命周期


        /// <summary>
        /// 打开编辑器窗叀
        /// </summary>
        [MenuItem("ATEditor/动作时间轴编辑器")]
        public static void ShowWindow()
        {
            // 尝试停靠圀Scene 视图旁边
            System.Type[] dockTypes = new System.Type[] { typeof(SceneView) };
            ATEditorWindow window = GetWindow<ATEditorWindow>("动作时间轴编辑器", dockTypes);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // 0. 初始化语言
            Lan.Load();

            // 1. 初始化核心状态与事件
            state = new ATEditorState();
            events = new ATEditorEvents();
            
            // 绑定基础刷新回调
            events.OnRepaintRequest += Repaint;

            // 2. 初始化子视图 (构造注兀
            toolbarView = new ToolbarView(this, state, events);
            timelineView = new TimelineView(this, state, events);
            trackListView = new TrackListView(this, state, events);
            
            // 订阅选中变更事件，同步到原生 Inspector
            events.OnSelectionChanged += SyncSelectionToInspector;

            // 3. 数据初始匀
            state.currentTimeline = ScriptableObject.CreateInstance<SkillTimeline>();
            state.currentTimeline.hideFlags = HideFlags.HideAndDontSave;
            
            // 绑定 Undo 回调
            Undo.undoRedoPerformed += OnUndoRedo;
            
            // 初始化轨道ID
            InitializeTrackIds();

            // 如果没有预览目标，自动加载默认目栀
            if (state.previewTarget == null)
            {
                toolbarView.CreateDefaultPreviewCharacter();
            }

            // 初始化预览播放器 (如果在上面赋予了新的 previewTarget，这里会被正确注兀
            InitPreview();

            // 注册 SceneView 绘制
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            // 停止播放
            if (state != null)
            {
                state.isStopped = true;
            }

            // 解绑 Undo 回调
            Undo.undoRedoPerformed -= OnUndoRedo;

            // 清理 Inspector 选中状态，防止残留包装对象
            if (Selection.activeObject is ClipObject || Selection.activeObject is TrackObject || Selection.activeObject is GroupObject)
            {
                Selection.activeObject = null;
            }

            // 销毁缓存的包装器对豀
            if (cachedClipWrapper != null) DestroyImmediate(cachedClipWrapper);
            if (cachedTrackWrapper != null) DestroyImmediate(cachedTrackWrapper);
            if (cachedGroupWrapper != null) DestroyImmediate(cachedGroupWrapper);

            // 释放预览系统
            DisposePreview();

            // 注销 SceneView 绘制
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnUndoRedo()
        {
            if (state == null || state.currentTimeline == null) return;

            // 1. 保存当前选中皀ID
            var selectedGroupIds = state.selectedGroup?.groupId;
            var selectedTrackId = state.selectedTrack?.trackId;
            var selectedClipIds = new System.Collections.Generic.List<string>();
            foreach (var clip in state.selectedClips)
            {
                if (clip != null) selectedClipIds.Add(clip.clipId);
            }
            bool wasTimelineSelected = state.isTimelineSelected;

            // 2. 重建缓存 (因为 Undo 可能导致 Track 对象引用变化)
            state.RebuildTrackCache();

            // 3. 尝试恢复选中状怀
            // 先清除旧引用
            if (trackListView != null) trackListView.ClearSelection(); 
            if (timelineView != null) timelineView.ClearClipSelection();
            
            // 恢复 Timeline 选中
            state.isTimelineSelected = wasTimelineSelected;

            // 恢复 Group 选中
            if (!string.IsNullOrEmpty(selectedGroupIds))
            {
                foreach (var group in state.currentTimeline.Groups)
                {
                    if (group.groupId == selectedGroupIds)
                    {
                        state.selectedGroup = group;
                        break;
                    }
                }
            }

            // 恢复 Track 选中
            if (!string.IsNullOrEmpty(selectedTrackId))
            {
                state.selectedTrack = state.GetTrackById(selectedTrackId);
            }

            // 恢复 Clip 选中
            if (selectedClipIds.Count > 0)
            {
                foreach (var track in state.currentTimeline.AllTracks)
                {
                    foreach (var clip in track.clips)
                    {
                        if (selectedClipIds.Contains(clip.clipId))
                        {
                            state.selectedClips.Add(clip);
                        }
                    }
                }
            }

            // 4. 同步刀Inspector
            SyncSelectionToInspector();
            
            Repaint();
        }

        /// <summary>
        /// 记录 Undo 并标记数据为脀
        /// </summary>
        public void RecordUndo(string label)
        {
            if (state != null && state.currentTimeline != null)
            {
                Undo.RegisterCompleteObjectUndo(state.currentTimeline, label);
                Undo.RecordObject(this, label);
                EditorUtility.SetDirty(state.currentTimeline);
            }
        }

        private void OnGUI()
        {
            // 自动同步 Duration (只读机制)
            if (Event.current.type == EventType.Layout && state != null && state.currentTimeline != null)
            {
                state.currentTimeline.RecalculateDuration();
            }

            // 绘制工具栀
            DrawToolbar();

            // 绘制主内容区埀
            DrawMainContent();
        }

        private double lastFrameTime;

        private void Update()
        {
            if (state == null) return;

            // 播放逻辑
            if (IsPlaying && !state.isStopped)
            {
                float deltaTime = (float)(EditorApplication.timeSinceStartup - lastFrameTime);
                lastFrameTime = EditorApplication.timeSinceStartup;

                // 防止过大皀deltaTime (例如断点调试吀
                if (deltaTime > 0.1f) deltaTime = 0.016f;

                state.timeIndicator += deltaTime;

                float maxDuration = state.currentTimeline != null ? state.currentTimeline.Duration : 10f;
                
                // 播放结束处理
                if (state.timeIndicator >= maxDuration)
                {
                    if (state.currentTimeline != null && state.currentTimeline.isLoop)
                    {
                         state.timeIndicator = 0f;
                    }
                    else
                    {
                        state.timeIndicator = maxDuration;
                        Stop();
                    }
                }

                Repaint();
                SceneView.RepaintAll();
            }
            else
            {
                lastFrameTime = EditorApplication.timeSinceStartup;
            }
            // 预览系统更新
            UpdatePreview();
        }

        #endregion
        
        #region 数据初始匀
        
        /// <summary>
        /// 初始化轨道ID（为现有轨道生成唯一ID锛?
        /// </summary>
        private void InitializeTrackIds()
        {
            if (state == null || state.currentTimeline == null) return;
            var timeline = state.currentTimeline;
            
            bool changed = false;
            foreach (var track in timeline.AllTracks)
            {
                if (string.IsNullOrEmpty(track.trackId))
                {
                    track.trackId = System.Guid.NewGuid().ToString();
                    changed = true;
                }
            }
            
            // 初始匀更新性能缓存
            state.RebuildTrackCache();
            
            if (changed)
            {
                EditorUtility.SetDirty(timeline);
            }
        }
        
        #endregion

        #region UI 绘制

        /// <summary>
        /// 绘制工具栀
        /// </summary>
        private void DrawToolbar()
        {
            Rect toolbarRect = new Rect(0, 0, position.width, TOOLBAR_HEIGHT);
            GUILayout.BeginArea(toolbarRect);
            
            if (toolbarView != null)
            {
                toolbarView.DoGUI();
            }
            
            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制主内容区域（轨道列表 + Timeline 视图＀
        /// </summary>
        private void DrawMainContent()
        {
            float yOffset = TOOLBAR_HEIGHT;
            float contentHeight = position.height - TOOLBAR_HEIGHT;
            
            // 1. 垂直滚动同步管理 (统一驱动左右视图)
            float totalHeight = state.CalculateTotalHeight();
            bool needsScroll = totalHeight > contentHeight;
            float scrollbarWidth = 0f; // 隐藏滚动轴视觀
            
            /*
            if (needsScroll)
            {
                Rect scrollRect = new Rect(position.width - scrollbarWidth, yOffset, scrollbarWidth, contentHeight);
                state.verticalScrollOffset = GUI.VerticalScrollbar(scrollRect, state.verticalScrollOffset, contentHeight, 0, totalHeight);
            }
            else
            {
                state.verticalScrollOffset = 0;
            }
            */
            if (!needsScroll) state.verticalScrollOffset = 0;


            // 1.1 监听全局常规鼠标滚轮(垂直滚动)
            if (needsScroll && Event.current.type == EventType.ScrollWheel && !Event.current.control && !Event.current.command && !Event.current.shift)
            {
                float scrollSensitivity = 20f; // 调节滚动速度
                state.verticalScrollOffset += Event.current.delta.y * scrollSensitivity;
                state.verticalScrollOffset = Mathf.Clamp(state.verticalScrollOffset, 0, totalHeight - contentHeight);
                Event.current.Use();
            }

            // 2. 轨道列表区域 (固定宽度)
            Rect trackListRect = new Rect(0, yOffset, TRACK_LIST_WIDTH, contentHeight);
            GUILayout.BeginArea(trackListRect);
            if (trackListView != null)
            {
                trackListView.DoGUI();
            }
            GUILayout.EndArea();

            // 3. Timeline 视图区域 (自适应宽度)
            float timelineWidth = position.width - TRACK_LIST_WIDTH - scrollbarWidth;
            Rect timelineRect = new Rect(TRACK_LIST_WIDTH, yOffset, timelineWidth, contentHeight);
            GUILayout.BeginArea(timelineRect);
            if (timelineView != null)
            {
                timelineView.DoGUI(timelineRect);
            }
            GUILayout.EndArea();
            
            // 全局重绘请求 (如果滚动条在劀
            if (needsScroll && Event.current.type == EventType.Used)
            {
                Repaint();
            }
        }

        #endregion



        #region 公共接口

        /// <summary>
        /// 获取当前 Timeline
        /// </summary>
        public SkillTimeline GetCurrentTimeline()
        {
            return state?.currentTimeline;
        }

        public ATEditorState GetState() => state;
        public ATEditorEvents GetEvents() => events;

        /// <summary>
        /// 设置当前 Timeline
        /// </summary>
        public void SetCurrentTimeline(SkillTimeline timeline)
        {
            if(state == null) return;
            
            // 切换数据前清空当前选中，并同步刀Inspector
            state.ClearSelection();
            SyncSelectionToInspector();
            
            state.currentTimeline = timeline;
            
            // 初始匀更新缓存
            InitializeTrackIds();
            
            Repaint();
        }

        /// <summary>
        /// 刷新窗口
        /// </summary>
        public void RefreshWindow()
        {
            Repaint();
        }
        
        /// <summary>
        /// 获取时间轴缩放倀
        /// </summary>
        public float GetTimelineZoom()
        {
            return timelineView != null ? timelineView.GetZoom() : 100f;
        }

        /// <summary>
        /// 时间轴失去焦点回调（清除片段选中＀
        /// </summary>
        public void OnTimelineLostFocus()
        {
            if (timelineView != null)
            {
                timelineView.OnLostFocus();
            }
        }
        /// <summary>
        /// 轨道列表失去焦点回调（清除轨道选中/退出重命名状态）
        /// </summary>
        public void OnTrackListLostFocus()
        {
            if (trackListView != null)
            {
                trackListView.OnLostFocus();
            }
        }

        /// <summary>
        /// 重置时间轴缩放级别为默认倀(100px/s)
        /// </summary>
        public void ResetTimelineZoom()
        {
            if (timelineView != null)
            {
                timelineView.ResetZoom();
            }
        }

        public void SelectTimeline()
        {
            if (state == null) return;
            state.ClearSelection();
            state.isTimelineSelected = true;
            SyncSelectionToInspector();
            Repaint();
        }

        /// <summary>
        /// 同步选中状态到系统皀Inspector
        /// </summary>
        private void SyncSelectionToInspector()
        {
            if (state == null || state.currentTimeline == null) return;

            if (state.isTimelineSelected)
            {
                // 选中 Timeline 自身
                Selection.activeObject = state.currentTimeline;
            }
            else if (state.selectedClips.Count > 0)
            {
                var clip = state.selectedClips[0];
                
                // 如果当前选中的已经是同一个片段，则不需要重新赋值（防止丢失焦点＀
                if (Selection.activeObject is ClipObject existing && existing.clipData == clip)
                    return;

                if (cachedClipWrapper == null) 
                    cachedClipWrapper = ClipObject.Create(clip, state.currentTimeline);
                else
                {
                    cachedClipWrapper.clipData = clip;
                    cachedClipWrapper.timeline = state.currentTimeline;
                }
                Selection.activeObject = cachedClipWrapper;
            }
            else if (state.selectedTrack != null)
            {
                var track = state.selectedTrack;
                
                if (Selection.activeObject is TrackObject existing && existing.trackData == track)
                    return;

                if (cachedTrackWrapper == null)
                    cachedTrackWrapper = TrackObject.Create(track, state.currentTimeline);
                else
                {
                    cachedTrackWrapper.trackData = track;
                    cachedTrackWrapper.timeline = state.currentTimeline;
                }
                Selection.activeObject = cachedTrackWrapper;
            }
            else if (state.selectedGroup != null)
            {
                var group = state.selectedGroup;
                
                if (Selection.activeObject is GroupObject existing && existing.groupData == group)
                    return;

                if (cachedGroupWrapper == null)
                    cachedGroupWrapper = GroupObject.Create(group, state.currentTimeline);
                else
                {
                    cachedGroupWrapper.groupData = group;
                    cachedGroupWrapper.timeline = state.currentTimeline;
                }
                Selection.activeObject = cachedGroupWrapper;
            }
            else
            {
                // 未选中任何内部对象时，如果当前 Inspector 停留在我们的包装对象上，则清空它
                if (Selection.activeObject is ClipObject || Selection.activeObject is TrackObject || Selection.activeObject is GroupObject)
                {
                    Selection.activeObject = null;
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (state == null) return;

            if (state.selectedClips.Count > 0)
            {
                var clip = state.selectedClips[0];
                if (clip != null)
                {
                    var drawer = ClipDrawerFactory.CreateDrawer(clip);
                    drawer?.DrawSceneGUI(clip, state);
                }
            }
        }

        #endregion
    }
}
