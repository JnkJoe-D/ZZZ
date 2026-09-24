using System;
using Game.Editor.ActionConfig;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(RouteWindowClip))]
    public sealed class ComboWindowClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showCombo = true;

        public override void DrawInspector(ClipBase clip)
        {
            if (clip is not RouteWindowClip comboWindow)
            {
                base.DrawInspector(clip);
                return;
            }

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 路由派生窗口卡片（仅允许选择已配置的预设窗口）
            _showCombo = EditorGUILayout.Foldout(_showCombo, "路由派生窗口配置", true, EditorStyles.foldoutHeader);
            if (_showCombo)
            {
                EditorGUILayout.BeginVertical("box");

                var presetWindows = ActionTagOptions.GetRouteWindows();
                string[] presetLabels = ActionTagOptions.GetRouteWindowDisplayOptions();

                if (presetLabels == null || presetLabels.Length == 0)
                {
                    EditorGUILayout.HelpBox("未在 ActionTagConfig 中配置路由窗口，请先在配置资产中添加预设窗口。", MessageType.Warning);
                }
                else
                {
                    int matchPresetIndex = -1;
                    if (comboWindow.routewindow != null)
                    {
                        for (int i = 0; i < presetWindows.Count; i++)
                        {
                            if (presetWindows[i] != null && presetWindows[i].Matches(comboWindow.routewindow))
                            {
                                matchPresetIndex = i;
                                break;
                            }
                        }
                    }

                    if (matchPresetIndex >= 0)
                    {
                        // 当前窗口属于已注册预设
                        int newIndex = EditorGUILayout.Popup("预设窗口", matchPresetIndex, presetLabels);
                        if (newIndex != matchPresetIndex)
                        {
                            comboWindow.routewindow = presetWindows[newIndex]?.Clone();
                        }
                    }
                    else
                    {
                        // 当前窗口未设置或不在已注册列表中
                        string currentLabel = comboWindow.routewindow != null && !string.IsNullOrEmpty(comboWindow.routewindow.Tag)
                            ? $"{comboWindow.routewindow.EditorLabel} [未在配置SO中注册]"
                            : "<请选择预设窗口>";

                        Color oldColor = GUI.color;
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("当前状态", currentLabel);
                        GUI.color = oldColor;

                        int newIndex = EditorGUILayout.Popup("选择预设窗口...", -1, presetLabels);
                        if (newIndex >= 0)
                        {
                            comboWindow.routewindow = presetWindows[newIndex]?.Clone();
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Route Window Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Route Window Clip");
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, ATEditorState state, Color clipColor, string displayName)
        {
            if (clip is RouteWindowClip comboWindow && comboWindow.routewindow != null && !string.IsNullOrWhiteSpace(comboWindow.routewindow.Tag))
            {
                displayName = comboWindow.routewindow.EditorLabel;
            }

            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);
        }
    }
}
