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
            
            GUILayout.Space(16);

            // 文件操作组 - 使用圆角按钮 + 间距
            DrawRoundedButton("导入", 80, OnImport);
            GUILayout.Space(4);
            DrawRoundedButton("导出/另存", 80, OnExportDual);
            GUILayout.Space(4);
            DrawRoundedButton(Lan.Save, 60, OnSaveDual);
            GUILayout.Space(8);
            DrawRoundedButton(Lan.Settings, 56, OnSettings);

            GUILayout.Space(8);

            // 预览角色选择器（移除"预览角色："文本标签，直接显示 ObjectField）
            DrawPreviewTargetSelector();

            GUILayout.FlexibleSpace();

            // 视口控制 (右侧) - 文件名 Toggle + 缩放复原按钮
            string displayName = string.IsNullOrEmpty(state.currentFilePath) ? "未保存" : System.IO.Path.GetFileName(state.currentFilePath);
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
        /// 绘制圆角按钮（带悬停色差）
        /// </summary>
        private void DrawRoundedButton(string label, float width, System.Action onClick)
        {
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            if (GUILayout.Button(label, RoundedButtonStyle, GUILayout.Width(width)))
            {
                onClick?.Invoke();
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
                    // 只要导入的是 Asset，立马克隆一份切断与底层 AssetDatabase 的联系，用克隆体作为编辑器上下文
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
                    // 重置先前的播放状态
                    state.isStopped = true;
                    state.timeIndicator = 0f;
                    window.Stop(); // Ensure window-level stop logic applies
                    events.OnRepaintRequest?.Invoke();
                }
            }, state.currentFilePath);
        }

        private void OnExportDual()
        {
            if (state.currentTimeline == null) return;
            
            // 弹出个窗口只是为了取个名字，这里默认导向 JSON 目录
            string path = EditorUtility.SaveFilePanel(Lan.ExportPanelTitle, state.DefaultJsonDirectory, "未命名", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                // 由于 currentTimeline 始终是内存独立的 Clone，直接改名并保存即可
                state.currentTimeline.name = fileName; 

                SerializationUtility.SaveDual(state.currentTimeline, state.DefaultJsonDirectory, state.DefaultAssetDirectory, fileName);
                state.currentFilePath = path; // 记录最新路径
                AssetDatabase.Refresh();
            }
        }

        private void OnSaveDual()
        {
            if (state.currentTimeline == null) return;

            // 如果有记录的文件路径，直接基于这个名字覆盖双轨
            if (!string.IsNullOrEmpty(state.currentFilePath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(state.currentFilePath);
                SerializationUtility.SaveDual(state.currentTimeline, state.DefaultJsonDirectory, state.DefaultAssetDirectory, fileName);
                AssetDatabase.Refresh();
            }
            else
            {
                // 否则执行另存为
                OnExportDual();
            }
        }

        private void OnSettings()
        {
            ATEditorSettingsWindow.Show(state, () => {
                // 当设置变更时，请求重绘
                events.OnRepaintRequest?.Invoke();
            });
        }

        /// <summary>
        /// 绘制预览角色选择器（移除"预览角色："文本标签）
        /// </summary>
        private void DrawPreviewTargetSelector()
        {
            EditorGUI.BeginChangeCheck();
            state.previewTarget = (GameObject)EditorGUILayout.ObjectField(
                state.previewTarget, typeof(GameObject), true, GUILayout.Width(130));
            
            if (EditorGUI.EndChangeCheck())
            {
                // 当目标改变时，强制重建上下文，供 Drawer 静态预览使用
                window.InitPreview();
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 从 Editor/Resources 加载默认角色并实例化到场景
        /// </summary>
        public void CreateDefaultPreviewCharacter()
        {
            GameObject target = GameObject.Find("DefaultPreviewCharacter");
            if(target!=null)
            {
                state.previewTarget = target;
                state.initialAutoPreviewTarget = target; // 记录为初始寻找到的目标
                return;
            }
            target = AssetDatabase.LoadAssetAtPath<GameObject>(
                    state.DefaultPreviewCharacterPath);
            if (target != null)
            {
                state.previewTarget = Object.Instantiate(target);
                state.previewTarget.name = "DefaultPreviewCharacter";
                // 记录为初始自动创建的目标
                state.initialAutoPreviewTarget = state.previewTarget;
            }
            else
            {
                Debug.LogWarning($"[ATEditor] 默认预览角色 Prefab 未找到: {state.DefaultPreviewCharacterPath}");
            }
        }

        #endregion
        private bool CheckTarget()
        {
            if (state.previewTarget == null)
            {
                Debug.LogWarning(Lan.PreviewTargetWarning);
                CreateDefaultPreviewCharacter();
            }
            return state.previewTarget != null;
        }
    }
}

