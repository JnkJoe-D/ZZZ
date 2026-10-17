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
        /// 打开技能编辑器窗口
        /// </summary>
        [MenuItem("ATEditor/动作时间轴编辑器")]
        public static void ShowWindow()
        {
            // 尝试停靠在 Scene 视图旁边
            System.Type[] dockTypes = new System.Type[] { typeof(SceneView) };
            ATEditorWindow window = GetWindow<ATEditorWindow>("动作时间轴编辑器", dockTypes);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is ActionTimeline timeline)
            {
                // 打开编辑器窗口
                ShowWindow();
                
                // 获取窗口实例并加载 timeline
                var window = GetWindow<ATEditorWindow>();
                if (window != null && window.state != null)
                {
                    string path = AssetDatabase.GetAssetPath(timeline);

                    // 1. 检查并对齐资产所属的工作区
                    var db = ATEditorWorkspaceDatabase.Instance;
                    var matchedWs = db.GetWorkspaceByAssetPath(path);
                    if (matchedWs != null && matchedWs.Id != window.state.ActiveWorkspaceId)
                    {
                        window.state.ActiveWorkspaceId = matchedWs.Id;
                    }

                    // 2. 加载 timeline
                    window.SetCurrentTimeline(timeline);
                    window.state.currentFilePath = path;
                    
                    // 3. 确保当前工作区的预览对象已就绪 (T-Pose) 并预热
                    window.EnsureWorkspacePreviewTarget();
                    window.InitPreview();

                    // 4. 重置先前的播放状态
                    window.state.isStopped = true;
                    window.state.timeIndicator = 0f;
                    window.Stop();
                    window.events.OnRepaintRequest?.Invoke();
                    window.Repaint();
                }
                return true;
            }
            return false;
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

            // 2. 初始化子视图 (构造注入)
            toolbarView = new ToolbarView(this, state, events);
            timelineView = new TimelineView(this, state, events);
            trackListView = new TrackListView(this, state, events);
            
            // 订阅选中变更事件，同步到原生 Inspector
            events.OnSelectionChanged += SyncSelectionToInspector;

            // 3. 数据初始化（默认不自动创建空白时间轴，若无资产则直接处于主菜单）
            if (state.currentTimeline != null)
            {
                InitializeTrackIds();
                InitPreview();
            }
            
            // 绑定 Undo 回调
            Undo.undoRedoPerformed += OnUndoRedo;
            
            // 监听运行模式切换（防止从运行模式切回时当前 Timeline 被销毁导致视图空白）
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

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

            // 解绑运行模式切换回调
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

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

            // 3. 尝试恢复选中状态
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

            // 4. 同步到 Inspector
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
            if (state == null) return;

            // 若当前未打开任何动作时间轴，全屏渲染主菜单
            if (state.currentTimeline == null)
            {
                DrawMainMenu();
                return;
            }

            // 自动同步 Duration (只读机制)
            if (Event.current.type == EventType.Layout && state.currentTimeline != null)
            {
                state.currentTimeline.RecalculateDuration();
            }

            // 绘制工具栏
            DrawToolbar();

            // 绘制主内容区域
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
        /// 绘制主菜单界面（正中央从上往下依次排布：新建、打开、设置）
        /// </summary>
        private void DrawMainMenu()
        {
            // 全屏深色背景填充，参考图1统一色系
            Rect fullRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(fullRect, new Color(0.1607843f, 0.1607843f, 0.1607843f));

            float menuWidth = 300f;
            float menuHeight = 260f;
            float startX = Mathf.Max(10f, (position.width - menuWidth) * 0.5f);
            float startY = Mathf.Max(10f, (position.height - menuHeight) * 0.5f);

            GUILayout.BeginArea(new Rect(startX, startY, menuWidth, menuHeight));
            {
                GUILayout.Space(10);

                // 标题
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
                };
                GUILayout.Label("ATEditor 动作时间轴", titleStyle);

                GUILayout.Space(6);

                // 工作区副标题
                var ws = state?.ActiveWorkspace;
                string wsText = ws != null ? $"当前工作区: {ws.Category} / {ws.DisplayName}" : "未配置工作区";
                GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) }
                };
                GUILayout.Label(wsText, subStyle);

                GUILayout.Space(26);

                // 统一菜单大按钮样式
                GUIStyle menuBtnStyle = new GUIStyle("button")
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 40,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 4, 4)
                };

                // 按钮 1：新建
                if (GUILayout.Button("＋  新 建", menuBtnStyle))
                {
                    CreateNewTimeline();
                }

                GUILayout.Space(8);

                // 按钮 2：打开
                if (GUILayout.Button("📂  打 开", menuBtnStyle))
                {
                    OpenTimelineDialog();
                }

                GUILayout.Space(8);

                // 按钮 3：设置
                if (GUILayout.Button("⚙  设 置", menuBtnStyle))
                {
                    OpenSettingsDialog();
                }
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制工具栏
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
            float scrollbarWidth = 0f; // 隐藏滚动条视图
            
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
            
            // 全局重绘请求 (如果滚动条在拖动)
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
        public ActionTimeline GetCurrentTimeline()
        {
            return state?.currentTimeline;
        }

        public ATEditorState State => state;
        public ATEditorEvents Events => events;
        public ATEditorState GetState() => state;
        public ATEditorEvents GetEvents() => events;

        /// <summary>
        /// 设置当前 Timeline
        /// </summary>
        public void SetCurrentTimeline(ActionTimeline timeline)
        {
            if(state == null) return;
            
            // 切换数据前清空当前选中，并同步到 Inspector
            state.ClearSelection();
            SyncSelectionToInspector();
            
            state.currentTimeline = timeline;
            
            // 初始匀更新缓存
            InitializeTrackIds();
            
            Repaint();
        }

        /// <summary>
        /// 重置为一个全新的空白时间轴
        /// </summary>
        public void ResetToBlankTimeline()
        {
            var empty = ScriptableObject.CreateInstance<ActionTimeline>();
            empty.hideFlags = HideFlags.HideAndDontSave;
            empty.name = "未命名动作";
            empty.Groups.Clear();
            SetCurrentTimeline(empty);
            if (state != null)
            {
                state.currentFilePath = null;
                state.timeIndicator = 0f;
            }
        }

        /// <summary>
        /// 主菜单【新建】操作
        /// </summary>
        public void CreateNewTimeline()
        {
            ResetToBlankTimeline();
            EnsureWorkspacePreviewTarget();
            InitPreview();
            if (state != null)
            {
                state.isStopped = true;
                state.timeIndicator = 0f;
            }
            Stop();
            events?.OnRepaintRequest?.Invoke();
            Repaint();
        }

        /// <summary>
        /// 主菜单【打开】操作
        /// </summary>
        public void OpenTimelineDialog()
        {
            toolbarView?.OpenImportDialog();
        }

        /// <summary>
        /// 主菜单【设置】操作
        /// </summary>
        public void OpenSettingsDialog()
        {
            ATEditorSettingsWindow.Show(state, () => {
                events?.OnRepaintRequest?.Invoke();
            });
        }

        /// <summary>
        /// 关闭当前时间轴并返回主菜单，带智能未保存修改检测
        /// </summary>
        /// <returns>是否成功关闭（若用户选择取消则返回 false）</returns>
        public bool CloseCurrentTimeline()
        {
            if (state == null || state.currentTimeline == null)
            {
                return true;
            }

            // 情况 1：未保存在磁盘的新建资产 (currentFilePath 为空)
            if (string.IsNullOrEmpty(state.currentFilePath))
            {
                // 提示动作资产未保存是否保存，可选项-保存/不保存/取消
                int option = EditorUtility.DisplayDialogComplex(
                    "动作资产未保存",
                    "动作资产未保存，是否保存？",
                    "保存",   // 0: 对应 [文件]-[导出]
                    "取消",   // 1: 取消关闭
                    "不保存"  // 2: 放弃并关闭
                );

                if (option == 1) // 取消
                {
                    return false;
                }
                else if (option == 0) // 保存 (对应[文件]-[导出])
                {
                    if (toolbarView != null && toolbarView.ExportDualDirect(out _, out _, out _))
                    {
                        // 导出成功，执行关闭
                    }
                    else
                    {
                        // 用户在保存文件面板中点击了取消，放弃关闭
                        return false;
                    }
                }
                // option == 2 -> 不保存，直接往下走关闭
            }
            // 情况 2：已有的资产 (!string.IsNullOrEmpty(state.currentFilePath))
            else
            {
                // 检测是否有修改（是否与磁盘资产有区别）
                if (HasUnsavedChanges())
                {
                    int option = EditorUtility.DisplayDialogComplex(
                        "未保存的修改",
                        "动作资产有未保存修改，是否保存？",
                        "保存",   // 0: 对应 [文件]-[保存]
                        "取消",   // 1: 取消关闭
                        "不保存"  // 2: 放弃并关闭
                    );

                    if (option == 1) // 取消
                    {
                        return false;
                    }
                    else if (option == 0) // 保存 (对应[文件]-[保存])
                    {
                        toolbarView?.SaveCurrentTimeline();
                    }
                    // option == 2 -> 不保存，直接往下走关闭
                }
            }

            // 执行实际关闭逻辑
            ExecuteCloseTimeline();
            return true;
        }

        private void ExecuteCloseTimeline()
        {
            Stop();
            DisposePreview();

            if (state != null)
            {
                state.ClearSelection();
                state.currentTimeline = null;
                state.currentFilePath = null;
                state.isStopped = true;
                state.timeIndicator = 0f;
            }

            SyncSelectionToInspector();
            events?.OnRepaintRequest?.Invoke();
            Repaint();
        }

        /// <summary>
        /// 检测当前内存中的时间轴是否与磁盘文件内容存在差异
        /// </summary>
        public bool HasUnsavedChanges()
        {
            if (state == null || state.currentTimeline == null) return false;
            if (string.IsNullOrEmpty(state.currentFilePath)) return true;

            string jsonPath = state.currentFilePath;
            if (jsonPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
            {
                var db = ATEditorWorkspaceDatabase.Instance;
                var matchedWs = db.GetWorkspaceByAssetPath(jsonPath) ?? state.ActiveWorkspace;
                string jsonDir;
                if (matchedWs != null && !string.IsNullOrEmpty(matchedWs.FolderName))
                {
                    jsonDir = System.IO.Path.Combine(state.DefaultJsonDirectory, matchedWs.FolderName).Replace("\\", "/");
                }
                else
                {
                    jsonDir = state.GetActiveWorkspaceJsonDirectory();
                }
                string fileName = System.IO.Path.GetFileNameWithoutExtension(jsonPath);
                jsonPath = System.IO.Path.Combine(jsonDir, fileName + ".json").Replace("\\", "/");
            }

            if (!System.IO.File.Exists(jsonPath))
            {
                return true;
            }

            try
            {
                SerializationUtility.RefreshAllGuids(state.currentTimeline);
                string memJson = JsonUtility.ToJson(state.currentTimeline, false);

                string diskRaw = System.IO.File.ReadAllText(jsonPath);
                var tempTimeline = ScriptableObject.CreateInstance<ActionTimeline>();
                JsonUtility.FromJsonOverwrite(diskRaw, tempTimeline);
                SerializationUtility.RefreshAllGuids(tempTimeline);
                string diskJson = JsonUtility.ToJson(tempTimeline, false);
                DestroyImmediate(tempTimeline);

                return memJson != diskJson;
            }
            catch
            {
                return EditorUtility.IsDirty(state.currentTimeline);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.EnteredPlayMode)
            {
                // 即将进入/已进入运行模式：彻底销毁预览对象，避免污染游戏运行场景
                DestroyAllPreviewTargets();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                // 退出运行模式回到编辑模式：先清理任何潜在游离预览实例
                DestroyAllPreviewTargets();

                if (state == null || state.currentTimeline == null)
                {
                    ResetToBlankTimeline();
                }
                else if (!string.IsNullOrEmpty(state.currentFilePath))
                {
                    // 仅在已有载入动作资产时才恢复生成预览模型
                    EnsureWorkspacePreviewTarget();
                    InitPreview();
                }
                Repaint();
            }
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
        /// 重置时间轴缩放级别为默认值 (100px/s)
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
        /// 同步选中状态到系统的 Inspector
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
                
                // 如果当前选中的已经是同一个片段，则不需要重新赋值（防止丢失焦点）
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
