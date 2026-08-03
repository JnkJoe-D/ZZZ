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

            // 2. 连招派生窗口卡片
            _showCombo = EditorGUILayout.Foldout(_showCombo, "连招派生配置", true, EditorStyles.foldoutHeader);
            if (_showCombo)
            {
                EditorGUILayout.BeginVertical("box");
                string[] tagOptions = ActionTagOptions.GetComboWindowTags();
                comboWindow.comboTag = DrawComboTagField(comboWindow.comboTag, tagOptions);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Combo Window Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Combo Window Clip");
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, ATEditorState state, Color clipColor, string displayName)
        {
            if (clip is RouteWindowClip comboWindow && !string.IsNullOrWhiteSpace(comboWindow.comboTag))
            {
                displayName = comboWindow.comboTag;
            }

            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);
        }

        private static string DrawComboTagField(string currentValue, string[] tagOptions)
        {
            if (tagOptions == null || tagOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("未配置连招窗口标签，请手动输入标签。", MessageType.Warning);
                return EditorGUILayout.TextField("连招标签", currentValue);
            }

            int currentIndex = Array.IndexOf(tagOptions, currentValue);
            if (currentIndex >= 0)
            {
                int newIndex = EditorGUILayout.Popup("连招标签", currentIndex, tagOptions);
                return tagOptions[newIndex];
            }

            Color oldColor = GUI.color;
            GUI.color = Color.yellow;
            string editedValue = EditorGUILayout.TextField("连招标签 [未注册]", currentValue);
            GUI.color = oldColor;
            EditorGUILayout.HelpBox("此标签未在 ActionTagConfigAsset.availableComboWindowTags 中注册。可保留用于迁移或替换为已注册标签。", MessageType.Warning);
            return editedValue;
        }
    }
}
