using System;
using Game.Editor.ActionConfig;
using Game.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// ComboWindowTagAttribute 的自定义属性绘制器。
    /// 读取 ActionTagConfig 资产中配置的 availableComboWindowTags，并渲染为下拉选择框。
    /// 交互行为与 ComboWindowClipDrawer 保持高度一致（支持无标签、注册标签选择、未注册黄色高亮与一键替换）。
    /// </summary>
    [CustomPropertyDrawer(typeof(ComboWindowTagAttribute))]
    public sealed class ComboWindowTagDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 28f;
        private const string ManualInputOption = "[手动输入 / 自定义...]";
        private const string NoneOption = "<None>";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [ComboWindowTag] only on string fields!");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            string[] tagOptions = ActionTagOptions.GetComboWindowTags();
            string currentValue = property.stringValue ?? string.Empty;

            // 1. 如果没有读取到任何标签配置
            if (tagOptions == null || tagOptions.Length == 0)
            {
                Rect helpRect = new Rect(position.x, position.y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, "未配置连招窗口标签，请在 ActionTagConfig 中配置或手动输入。", MessageType.Warning);

                Rect fieldRect = new Rect(position.x, position.y + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                property.stringValue = EditorGUI.TextField(fieldRect, label, currentValue);
                EditorGUI.EndProperty();
                return;
            }

            int matchIndex = Array.IndexOf(tagOptions, currentValue);
            bool isUnregistered = !string.IsNullOrEmpty(currentValue) && matchIndex < 0;

            if (!isUnregistered)
            {
                // 2. 正常下拉框状态（包含 <None>、所有已注册标签及 [手动输入/自定义...]）
                DrawNormalPopup(position, property, label, tagOptions, matchIndex);
            }
            else
            {
                // 3. 未注册标签状态（黄色高亮 + 警告提示 + 替换下拉框）
                DrawUnregisteredField(position, property, label, tagOptions, currentValue);
            }

            // 处理右键上下文菜单（支持快速跳转到 ActionTagConfig 资产）
            HandleContextMenu(position);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            string[] tagOptions = ActionTagOptions.GetComboWindowTags();
            if (tagOptions == null || tagOptions.Length == 0)
            {
                return EditorGUIUtility.singleLineHeight + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            string currentValue = property.stringValue ?? string.Empty;
            bool isUnregistered = !string.IsNullOrEmpty(currentValue) && Array.IndexOf(tagOptions, currentValue) < 0;

            if (isUnregistered)
            {
                return EditorGUIUtility.singleLineHeight * 2 + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private static void DrawNormalPopup(Rect position, SerializedProperty property, GUIContent label, string[] tagOptions, int matchIndex)
        {
            // 下拉选项：<None>, 所有已注册标签, [手动输入 / 自定义...]
            string[] displayOptions = new string[tagOptions.Length + 2];
            displayOptions[0] = NoneOption;
            Array.Copy(tagOptions, 0, displayOptions, 1, tagOptions.Length);
            displayOptions[displayOptions.Length - 1] = ManualInputOption;

            int selectedIndex = string.IsNullOrEmpty(property.stringValue) ? 0 : matchIndex + 1;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    property.stringValue = string.Empty;
                }
                else if (newIndex == displayOptions.Length - 1)
                {
                    // 选择手动输入，赋一个初始未注册值以切换到未注册文本输入模式
                    property.stringValue = "CustomTag";
                }
                else
                {
                    property.stringValue = tagOptions[newIndex - 1];
                }
            }
        }

        private static void DrawUnregisteredField(Rect position, SerializedProperty property, GUIContent label, string[] tagOptions, string currentValue)
        {
            // 第一行：黄色高亮输入框
            Rect textRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Color oldColor = GUI.color;
            GUI.color = Color.yellow;
            string editedValue = EditorGUI.TextField(textRect, $"{label.text} [未注册]", currentValue);
            GUI.color = oldColor;

            if (editedValue != currentValue)
            {
                property.stringValue = editedValue;
            }

            // 第二行：警告提示
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
            EditorGUI.HelpBox(helpRect, "此标签未在 ActionTagConfig 中注册。可保留用于迁移或从下方选择替换。", MessageType.Warning);

            // 第三行：替换下拉框
            y += HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect popupRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

            string[] replaceOptions = new string[tagOptions.Length + 2];
            replaceOptions[0] = "<保持未注册>";
            replaceOptions[1] = "<None> (清除)";
            Array.Copy(tagOptions, 0, replaceOptions, 2, tagOptions.Length);

            EditorGUI.BeginChangeCheck();
            int replaceIndex = EditorGUI.Popup(popupRect, "替换为已注册标签...", 0, replaceOptions);
            if (EditorGUI.EndChangeCheck())
            {
                if (replaceIndex == 1)
                {
                    property.stringValue = string.Empty;
                }
                else if (replaceIndex > 1)
                {
                    property.stringValue = replaceOptions[replaceIndex];
                }
            }
        }

        private static void HandleContextMenu(Rect position)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick && position.Contains(evt.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("在 Project 中定位 ActionTagConfig"), false, () =>
                {
                    string[] guids = AssetDatabase.FindAssets("t:ActionTagConfig");
                    if (guids.Length == 0)
                    {
                        guids = AssetDatabase.FindAssets("t:ActionTagConfigAsset");
                    }
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var asset = AssetDatabase.LoadMainAssetAtPath(path);
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }
                });
                menu.ShowAsContext();
                evt.Use();
            }
        }
    }
}
