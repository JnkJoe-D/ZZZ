using UnityEngine;
using UnityEditor;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 工具栏视图
    /// </summary>
    public class ToolbarView
    {
        private SkillEditorWindow window;
        private SkillEditorState state;
        private SkillEditorEvents events;

        public ToolbarView(SkillEditorWindow window, SkillEditorState state, SkillEditorEvents events)
        {
            this.window = window;
            this.state = state;
            this.events = events;
        }

        public void DoGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Space(10);
            
            // 播放控制组
            DrawTransportControls();
            
            GUILayout.Space(20);
            
            GUILayout.Space(20);

            // 2. 文件操作组 (中间)
            if (GUILayout.Button(Lan.ImportFromJson, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                OnImportJSON();
            }
            if (GUILayout.Button(Lan.ExportToJson, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                OnExportJSON();
            }
            if (GUILayout.Button(Lan.Save, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                OnSaveJson();
            }

            if (GUILayout.Button(Lan.Settings, EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                OnSettings();
            }

            GUILayout.Space(10);

            // 4. 预览角色选择器
            DrawPreviewTargetSelector();

            GUILayout.FlexibleSpace();

            // 3. 视口控制 (右侧)
            // Timeline 选中/Inspector 按钮
            string displayName = string.IsNullOrEmpty(state.currentFilePath) ? "未保存" : System.IO.Path.GetFileName(state.currentFilePath);
            bool isSelected = GUILayout.Toggle(state.isTimelineSelected, displayName, EditorStyles.toolbarButton, GUILayout.Width(120));
            if (isSelected && !state.isTimelineSelected)
            {
                window.SelectTimeline();
            }
            
            if (GUILayout.Button($"{Lan.Zoom}: {state.zoom:F0}px/s", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                state.ResetView();
                events.OnRepaintRequest?.Invoke();
            }

            EditorGUILayout.EndHorizontal();
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
        /// 绘制播放控制按钮
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

        private void OnImportJSON()
        {
            JsonFileSelectionWindow.Show(state.DefaultExportDirectory, (path) => 
            {
                var newTimeline = SerializationUtility.ImportFromJsonPath(path);
                if (newTimeline != null)
                {
                    window.SetCurrentTimeline(newTimeline);
                    state.RebuildTrackCache();
                    state.currentFilePath = path; // 记录路径
                    // 重置先前的播放状态
                    state.isStopped = true;
                    state.timeIndicator = 0f;
                    window.Stop(); // Ensure window-level stop logic applies
                    events.OnRepaintRequest?.Invoke();
                }
            });
        }

        private void OnExportJSON()
        {
            if (state.currentTimeline == null) return;
            
            string path = EditorUtility.SaveFilePanel(Lan.ExportPanelTitle, state.DefaultExportDirectory, "未命名", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                SerializationUtility.ExportToJson(state.currentTimeline, path);
                state.currentFilePath = path; // 记录路径
                AssetDatabase.Refresh();
                Debug.Log($"[{Lan.EditorTitle}] {Lan.ExportToJson}: {path}");
            }
        }

        private void OnSaveJson()
        {
            if (state.currentTimeline == null) return;

            // 如果有记录的文件路径，直接覆盖
            if (!string.IsNullOrEmpty(state.currentFilePath))
            {
                SerializationUtility.ExportToJson(state.currentTimeline, state.currentFilePath);
                AssetDatabase.Refresh();
                Debug.Log($"[{Lan.EditorTitle}] {Lan.Save}: {state.currentFilePath}");
            }
            else
            {
                // 否则执行另存为
                OnExportJSON();
            }
        }

        private void OnSettings()
        {
            SkillEditorSettingsWindow.Show(state, () => {
                // 当设置变更时，请求重绘
                events.OnRepaintRequest?.Invoke();
            });
        }

        /// <summary>
        /// 绘制预览角色选择器
        /// </summary>
        private void DrawPreviewTargetSelector()
        {
            EditorGUILayout.LabelField(Lan.PreviewTarget, EditorStyles.miniLabel, GUILayout.Width(60));
            
            EditorGUI.BeginChangeCheck();
            state.previewTarget = (GameObject)EditorGUILayout.ObjectField(
                state.previewTarget, typeof(GameObject), true, GUILayout.Width(120));
            
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
                Debug.LogWarning($"[SkillEditor] 默认预览角色 Prefab 未找到: {state.DefaultPreviewCharacterPath}");
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