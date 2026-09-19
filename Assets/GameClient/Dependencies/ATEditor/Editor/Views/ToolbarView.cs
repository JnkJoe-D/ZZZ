using UnityEngine;
using UnityEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 工具栏视图
    /// </summary>
    public class ToolbarView
    {
        private ATEditorWindow window;
        private ATEditorState state;
        private ATEditorEvents events;

        // 缓存圆角按钮样式（懒初始化）
        private static GUIStyle _roundedButtonStyle;
        private static GUIStyle _roundedToggleStyle;

        private static GUIStyle RoundedButtonStyle
        {
            get
            {
                if (_roundedButtonStyle == null)
                {
                    _roundedButtonStyle = new GUIStyle("miniButton")
                    {
                        fontSize = 11,
                        fixedHeight = 16,
                        padding = new RectOffset(6, 6, 1, 1),
                        margin = new RectOffset(2, 2, 1, 0),
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _roundedButtonStyle;
            }
        }

        private static GUIStyle RoundedToggleStyle
        {
            get
            {
                if (_roundedToggleStyle == null)
                {
                    _roundedToggleStyle = new GUIStyle("miniButton")
                    {
                        fontSize = 11,
                        fixedHeight = 16,
                        padding = new RectOffset(6, 6, 1, 1),
                        margin = new RectOffset(2, 2, 1, 0),
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _roundedToggleStyle;
            }
        }

        public ToolbarView(ATEditorWindow window, ATEditorState state, ATEditorEvents events)
        {
            this.window = window;
            this.state = state;
            this.events = events;
        }

        public void DoGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Space(8);
            
            // 播放控制组（保持原有 toolbarButton 样式）
            DrawTransportControls();
            
            GUILayout.Space(4);

            // 归一化文件操作下拉菜单（左边界紧挨播放栏右边界）
            DrawFileMenuDropdown();

            GUILayout.Space(6);

            // 角色工作区选择器
            DrawWorkspaceSelector();

            GUILayout.FlexibleSpace();

            // 视口控制 (右侧) - 文件名 Toggle + 缩放复原按钮
            string displayName;
            if (state.currentTimeline == null)
            {
                displayName = "未打开动作";
            }
            else
            {
                displayName = string.IsNullOrEmpty(state.currentFilePath) ? "未保存" : System.IO.Path.GetFileName(state.currentFilePath);
            }
            bool isSelected = GUILayout.Toggle(state.isTimelineSelected, displayName, RoundedToggleStyle, GUILayout.Width(120));
            if (isSelected && !state.isTimelineSelected)
            {
                window.SelectTimeline();
            }
            
            GUILayout.Space(4);

            // 缩放复原按钮保持原有样式（用户要求不改圆角）
            if (GUILayout.Button($"{Lan.Zoom}: {state.zoom:F0}px/s", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                state.ResetView();
                events.OnRepaintRequest?.Invoke();
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制归一化文件菜单下拉按钮（包含：导入、导出/另存、保存、设置）
        /// </summary>
        private void DrawFileMenuDropdown()
        {
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            if (EditorGUILayout.DropdownButton(new GUIContent("文件"), FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("导入..."), false, OnImport);
                menu.AddItem(new GUIContent("导出..."), false, OnExportDual);
                menu.AddItem(new GUIContent(Lan.Save), false, OnSaveDual);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent(Lan.Settings + "..."), false, OnSettings);
                menu.ShowAsContext();
            }
            GUI.backgroundColor = oldBg;
        }

        #region 按钮回调

        private void OnJumpToStart()
        {
            if (!CheckTarget()) return;
            window.JumpToStart();
            events.OnRepaintRequest?.Invoke();
        }

        private void OnPrevFrame()
        {
            if (!CheckTarget()) return;
            window.StepBackward();
            events.OnRepaintRequest?.Invoke();
        }

        private void OnNextFrame()
        {
            if (!CheckTarget()) return;
            window.StepForward();
            events.OnRepaintRequest?.Invoke();
        }

        private void OnJumpToEnd()
        {
            if (!CheckTarget()) return;
            window.JumpToEnd();
            events.OnRepaintRequest?.Invoke();
        }

        /// <summary>
        /// 绘制播放控制按钮（保持原有紧凑 toolbarButton 样式）
        /// </summary>
        private void DrawTransportControls()
        {
            // 跳转首帧
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.FirstKey"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                OnJumpToStart();
            }

            // 上一帧
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.PrevKey"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                OnPrevFrame();
            }
            // 播放/暂停
            var icon = window.IsPlaying ? EditorGUIUtility.IconContent("d_PauseButton") : EditorGUIUtility.IconContent("d_PlayButton");
            string tooltip = window.IsPlaying ? Lan.Pause : Lan.Play;
            if (GUILayout.Button(new GUIContent(icon.image, tooltip), EditorStyles.toolbarButton, GUILayout.Width(35)))
            {
                OnTogglePlay();
            }

            // 停止
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_PreMatQuad").image, Lan.StopTooltip), EditorStyles.toolbarButton, GUILayout.Width(35)))
            {
                OnStop();
            }
            // 下一帧
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.NextKey"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                OnNextFrame();
            }

            // 跳转末帧
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Animation.LastKey"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                OnJumpToEnd();
            }
        }

        private void OnTogglePlay()
        {
            if (!CheckTarget()) return;
            window.TogglePlay();
        }

        private void OnStop()
        {
            window.Stop();
            events.OnRepaintRequest?.Invoke();
        }

        private void OnImport()
        {
            ImportTimelineWindow.Show(state.DefaultAssetDirectory, state.DefaultJsonDirectory, (newTimeline, path) => 
            {
                if (newTimeline != null)
                {
                    // 1. 检查并对齐资产所属工作区
                    var db = ATEditorWorkspaceDatabase.Instance;
                    var matchedWs = db.GetWorkspaceByAssetPath(path);
                    if (matchedWs != null && matchedWs.Id != state.ActiveWorkspaceId)
                    {
                        state.ActiveWorkspaceId = matchedWs.Id;
                    }

                    // 2. 只要导入的是 Asset，立马克隆一份切断与底层 AssetDatabase 的联系，用克隆体作为编辑器上下文
                    // 这样所有的修改都只在内存里，直到保存时才覆盖目标文件
                    if (AssetDatabase.Contains(newTimeline))
                    {
                        var clone = Object.Instantiate(newTimeline);
                        clone.name = newTimeline.name;
                        newTimeline = clone;
                    }

                    window.SetCurrentTimeline(newTimeline);
                    state.RebuildTrackCache();
                    state.currentFilePath = path; // 记录路径

                    // 3. 确保当前工作区预览对象生成并预热
                    window.EnsureWorkspacePreviewTarget();
                    window.InitPreview();

                    // 4. 重置先前的播放状态
                    state.isStopped = true;
                    state.timeIndicator = 0f;
                    window.Stop(); // 确保触发窗口级的停止逻辑
                    events.OnRepaintRequest?.Invoke();
                    window.Repaint();
                }
            }, state.currentFilePath);
        }

        private void OnExportDual()
        {
            if (state.currentTimeline == null) return;
            
            string targetJsonDir = state.GetActiveWorkspaceJsonDirectory();
            string targetAssetDir = state.GetActiveWorkspaceAssetDirectory();

            if (!System.IO.Directory.Exists(targetJsonDir)) System.IO.Directory.CreateDirectory(targetJsonDir);
            if (!System.IO.Directory.Exists(targetAssetDir)) System.IO.Directory.CreateDirectory(targetAssetDir);

            string defaultName = string.IsNullOrEmpty(state.currentTimeline.name) ? "未命名" : state.currentTimeline.name;
            string path = EditorUtility.SaveFilePanel(Lan.ExportPanelTitle, targetJsonDir, defaultName, "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string selectedDir = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                
                state.currentTimeline.name = fileName; 

                // 检查是否保存到了另一个工作区
                CheckAndHandleCrossWorkspaceSave(selectedDir);

                SerializationUtility.SaveDual(state.currentTimeline, selectedDir, targetAssetDir, fileName);
                state.currentFilePath = path; // 记录最新路径
                AssetDatabase.Refresh();
            }
        }

        public void SaveCurrentTimeline()
        {
            OnSaveDual();
        }

        private void OnSaveDual()
        {
            if (state.currentTimeline == null) return;

            // 如果有记录的文件路径，直接基于这个名字覆盖双轨
            if (!string.IsNullOrEmpty(state.currentFilePath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(state.currentFilePath);
                string jsonDir = System.IO.Path.GetDirectoryName(state.currentFilePath).Replace("\\", "/");
                string assetDir = state.GetActiveWorkspaceAssetDirectory();

                SerializationUtility.SaveDual(state.currentTimeline, jsonDir, assetDir, fileName);
                AssetDatabase.Refresh();
            }
            else
            {
                // 否则执行另存为
                OnExportDual();
            }
        }

        private void CheckAndHandleCrossWorkspaceSave(string savedJsonDir)
        {
            var db = ATEditorWorkspaceDatabase.Instance;
            foreach (var ws in db.Workspaces)
            {
                if (ws.Id == state.ActiveWorkspaceId) continue;

                string wsJsonDir = System.IO.Path.Combine(state.DefaultJsonDirectory, ws.FolderName).Replace("\\", "/");
                if (string.Equals(savedJsonDir.Trim('/'), wsJsonDir.Trim('/'), System.StringComparison.OrdinalIgnoreCase))
                {
                    bool switchWs = EditorUtility.DisplayDialog(
                        "跨工作区保存提示",
                        $"检测到文件保存到了 [{ws.DisplayName}] 工作区目录。\n是否同时将当前编辑器工作区切换为 [{ws.DisplayName}]？",
                        "切换工作区",
                        "保持当前工作区");

                    if (switchWs)
                    {
                        window.SwitchWorkspace(ws.Id, false);
                    }
                    break;
                }
            }
        }

        private void OnSettings()
        {
            ATEditorSettingsWindow.Show(state, () => {
                events.OnRepaintRequest?.Invoke();
            });
        }

        /// <summary>
        /// 绘制角色工作区选择器下拉框
        /// </summary>
        private void DrawWorkspaceSelector()
        {
            var ws = state.ActiveWorkspace;
            string display = ws != null ? $"{ws.Category} / {ws.DisplayName}" : "选择工作区";

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.95f, 1f, 1f);
            if (EditorGUILayout.DropdownButton(new GUIContent($"📁 {display}"), FocusType.Keyboard, EditorStyles.toolbarDropDown, GUILayout.Width(180)))
            {
                GenericMenu menu = new GenericMenu();
                var db = ATEditorWorkspaceDatabase.Instance;
                var categories = db.GetCategories();

                foreach (var category in categories)
                {
                    var workspaces = db.GetWorkspacesByCategory(category);
                    foreach (var w in workspaces)
                    {
                        bool isCurrent = ws != null && ws.Id == w.Id;
                        string menuPath = $"{w.Category}/{w.DisplayName}";
                        string id = w.Id;
                        menu.AddItem(new GUIContent(menuPath), isCurrent, () =>
                        {
                            window.SwitchWorkspace(id, true);
                        });
                    }
                }

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("⚙ 工作区管理器..."), false, () =>
                {
                    ATWorkspaceManagerWindow.OpenWindow();
                });
                menu.AddItem(new GUIContent("＋ 新建工作区..."), false, () =>
                {
                    ATWorkspaceManagerWindow.OpenForNewWorkspace();
                });

                menu.ShowAsContext();
            }
            GUI.backgroundColor = oldBg;
        }

        #endregion

        private bool CheckTarget()
        {
            if (state.previewTarget == null)
            {
                window.EnsureWorkspacePreviewTarget();
            }
            if (state.previewTarget == null)
            {
                var ws = state.ActiveWorkspace;
                string wsName = ws != null ? ws.DisplayName : "当前工作区";
                EditorUtility.DisplayDialog("缺少预览对象", $"工作区 '{wsName}' 尚未配置或生成预览 Prefab。\n请在工作区管理中配置'绑定预览 Prefab'。", "确定");
                return false;
            }
            return true;
        }
    }
}

