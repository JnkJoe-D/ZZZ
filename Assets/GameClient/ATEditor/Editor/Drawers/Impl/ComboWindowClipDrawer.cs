using System;
using Game.Editor.ActionConfig;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(ComboWindowClip))]
    public sealed class ComboWindowClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            if (clip is not ComboWindowClip comboWindow)
            {
                base.DrawInspector(clip);
                return;
            }

            EditorGUILayout.LabelField("Combo Window", EditorStyles.boldLabel);

            string[] tagOptions = ActionTagOptions.GetComboWindowTags();
            string newClipName = comboWindow.clipName;
            bool newEnabled = comboWindow.isEnabled;
            float newStartTime = comboWindow.StartTime;
            float newDuration = comboWindow.Duration;
            string newComboTag = comboWindow.comboTag;

            EditorGUI.BeginChangeCheck();
            newClipName = EditorGUILayout.TextField("Clip Name", newClipName);
            newEnabled = EditorGUILayout.Toggle("Enabled", newEnabled);
            newStartTime = Mathf.Max(0f, EditorGUILayout.FloatField("Start Time", newStartTime));
            newDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField("Duration", newDuration));
            newComboTag = DrawComboTagField(newComboTag, tagOptions);

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Inspector Change: Combo Window");
                }

                comboWindow.clipName = newClipName;
                comboWindow.isEnabled = newEnabled;
                comboWindow.StartTime = newStartTime;
                comboWindow.Duration = newDuration;
                comboWindow.comboTag = newComboTag;
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, ATEditorState state, Color clipColor, string displayName)
        {
            if (clip is ComboWindowClip comboWindow && !string.IsNullOrWhiteSpace(comboWindow.comboTag))
            {
                displayName = comboWindow.comboTag;
            }

            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);
        }

        private static string DrawComboTagField(string currentValue, string[] tagOptions)
        {
            if (tagOptions == null || tagOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("No combo window tags are configured. Enter a tag manually.", MessageType.Warning);
                return EditorGUILayout.TextField("Combo Tag", currentValue);
            }

            int currentIndex = Array.IndexOf(tagOptions, currentValue);
            if (currentIndex >= 0)
            {
                int newIndex = EditorGUILayout.Popup("Combo Tag", currentIndex, tagOptions);
                return tagOptions[newIndex];
            }

            Color oldColor = GUI.color;
            GUI.color = Color.yellow;
            string editedValue = EditorGUILayout.TextField("Combo Tag [Unregistered]", currentValue);
            GUI.color = oldColor;
            EditorGUILayout.HelpBox("This tag is not registered in ActionTagConfigAsset.availableComboWindowTags. Keep it for migration or replace it with a registered tag.", MessageType.Warning);
            return editedValue;
        }
    }
}
