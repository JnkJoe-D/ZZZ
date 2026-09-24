using System;
using System.Collections.Generic;
using System.Text;
using ATEditor;
using Game.Editor.ActionConfig;
using Game.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// ComboWindowTagAttribute 及 RouteWindow 的自定义属性绘制器。
    /// 读取 ActionTagConfig 资产中配置的 availableRouteWindows，并渲染为下拉选择框。
    /// 支持通过 ComboWindowTagAttribute.AllowedTypes 进行精确的多态类型筛选。
    /// 下拉列表文本格式为："[RouteWindow的具体子类名称]routewindow.tag"。
    /// </summary>
    [CustomPropertyDrawer(typeof(ComboWindowTagAttribute))]
    [CustomPropertyDrawer(typeof(RouteWindow))]
    public sealed class ComboWindowTagDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 28f;
        private const string NoneOption = "<None> (任意 / 全局)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                Type[] allowedTypes = (attribute as ComboWindowTagAttribute)?.AllowedTypes;
                DrawManagedReference(position, property, label, allowedTypes);
                return;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                DrawStringField(position, property, label);
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                Type[] allowedTypes = (attribute as ComboWindowTagAttribute)?.AllowedTypes;
                return GetManagedReferenceHeight(property, allowedTypes);
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                return GetStringFieldHeight(property);
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ManagedReference (RouteWindow) 绘制逻辑（支持类型过滤）
        // ═══════════════════════════════════════════════════════════════════

        private static void DrawManagedReference(Rect position, SerializedProperty property, GUIContent label, Type[] allowedTypes)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1. 获取全局配置并通过 AllowedTypes 进行过滤
            GetFilteredRouteWindows(allowedTypes, out var routeWindows, out string[] displayOptions);
            RouteWindow currentWindow = property.managedReferenceValue as RouteWindow;

            // 2. 如果没有配置任何窗口或过滤后为空
            if (displayOptions == null || displayOptions.Length == 0)
            {
                Rect helpRect = new Rect(position.x, position.y, position.width, HelpBoxHeight);
                string msg = allowedTypes != null && allowedTypes.Length > 0
                    ? $"ActionTagConfig 中未配置支持的窗口类型 ({FormatAllowedTypes(allowedTypes)})。"
                    : "未在 ActionTagConfig 中配置路由窗口，请先配置。";
                EditorGUI.HelpBox(helpRect, msg, MessageType.Warning);

                Rect fieldRect = new Rect(position.x, position.y + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                string text = currentWindow != null ? currentWindow.EditorLabel : "<None>";
                EditorGUI.LabelField(fieldRect, label.text, text);
                EditorGUI.EndProperty();
                return;
            }

            // 3. 检查当前窗口是否符合类型约束
            bool isTypeCompatible = true;
            if (currentWindow != null && allowedTypes != null && allowedTypes.Length > 0)
            {
                isTypeCompatible = false;
                for (int t = 0; t < allowedTypes.Length; t++)
                {
                    if (allowedTypes[t] != null && allowedTypes[t].IsAssignableFrom(currentWindow.GetType()))
                    {
                        isTypeCompatible = true;
                        break;
                    }
                }
            }

            if (!isTypeCompatible)
            {
                // 3.1 类型不兼容状态（红色/黄色警告 + 替换下拉框）
                Rect line1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Color oldColor = GUI.color;
                GUI.color = Color.red;
                EditorGUI.LabelField(line1, label.text, $"{currentWindow.EditorLabel} [类型不兼容！]");
                GUI.color = oldColor;

                float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, $"当前窗口与本触发器不兼容！仅支持: {FormatAllowedTypes(allowedTypes)}。请从下方选择兼容窗口替换。", MessageType.Error);

                y += HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect popupRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

                string[] replaceOptions = new string[displayOptions.Length + 2];
                replaceOptions[0] = "<保持当前不兼容配置>";
                replaceOptions[1] = NoneOption;
                Array.Copy(displayOptions, 0, replaceOptions, 2, displayOptions.Length);

                EditorGUI.BeginChangeCheck();
                int replaceIndex = EditorGUI.Popup(popupRect, "替换为兼容窗口...", 0, replaceOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    if (replaceIndex == 1)
                    {
                        property.managedReferenceValue = null;
                    }
                    else if (replaceIndex > 1)
                    {
                        RouteWindow selectedConfig = routeWindows[replaceIndex - 2];
                        property.managedReferenceValue = selectedConfig?.Clone();
                    }
                }

                HandleContextMenu(position);
                EditorGUI.EndProperty();
                return;
            }

            // 4. 类型兼容，检查是否在已注册列表中
            int matchIndex = -1;
            if (currentWindow != null)
            {
                for (int i = 0; i < routeWindows.Count; i++)
                {
                    if (routeWindows[i] != null && routeWindows[i].Matches(currentWindow))
                    {
                        matchIndex = i;
                        break;
                    }
                }
            }

            bool isUnregistered = currentWindow != null && matchIndex < 0;

            if (!isUnregistered)
            {
                // 4.1 正常下拉选择（选项列表严格为筛选后的兼容窗口）
                string[] popupOptions = new string[displayOptions.Length + 1];
                popupOptions[0] = NoneOption;
                Array.Copy(displayOptions, 0, popupOptions, 1, displayOptions.Length);

                int selectedIndex = currentWindow == null ? 0 : matchIndex + 1;

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, popupOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newIndex == 0)
                    {
                        property.managedReferenceValue = null;
                    }
                    else
                    {
                        RouteWindow selectedConfig = routeWindows[newIndex - 1];
                        property.managedReferenceValue = selectedConfig?.Clone();
                    }
                }
            }
            else
            {
                // 4.2 未注册窗口状态（黄色高亮 + 警告提示 + 替换下拉框）
                Rect line1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(line1, label.text, $"{currentWindow.EditorLabel} [未在配置SO中注册]");
                GUI.color = oldColor;

                float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, "此窗口未在 ActionTagConfig 资产中注册。可保留或从下方下拉列表中替换。", MessageType.Warning);

                y += HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect popupRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

                string[] replaceOptions = new string[displayOptions.Length + 2];
                replaceOptions[0] = "<保持当前未注册配置>";
                replaceOptions[1] = NoneOption;
                Array.Copy(displayOptions, 0, replaceOptions, 2, displayOptions.Length);

                EditorGUI.BeginChangeCheck();
                int replaceIndex = EditorGUI.Popup(popupRect, "替换为已注册窗口...", 0, replaceOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    if (replaceIndex == 1)
                    {
                        property.managedReferenceValue = null;
                    }
                    else if (replaceIndex > 1)
                    {
                        RouteWindow selectedConfig = routeWindows[replaceIndex - 2];
                        property.managedReferenceValue = selectedConfig?.Clone();
                    }
                }
            }

            HandleContextMenu(position);
            EditorGUI.EndProperty();
        }

        private static float GetManagedReferenceHeight(SerializedProperty property, Type[] allowedTypes)
        {
            GetFilteredRouteWindows(allowedTypes, out _, out string[] displayOptions);
            if (displayOptions == null || displayOptions.Length == 0)
            {
                return EditorGUIUtility.singleLineHeight + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            RouteWindow currentWindow = property.managedReferenceValue as RouteWindow;
            if (currentWindow != null)
            {
                // 检查类型兼容性
                if (allowedTypes != null && allowedTypes.Length > 0)
                {
                    bool compatible = false;
                    for (int t = 0; t < allowedTypes.Length; t++)
                    {
                        if (allowedTypes[t] != null && allowedTypes[t].IsAssignableFrom(currentWindow.GetType()))
                        {
                            compatible = true;
                            break;
                        }
                    }
                    if (!compatible)
                    {
                        return EditorGUIUtility.singleLineHeight * 2 + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing * 2;
                    }
                }

                // 检查是否在注册列表中
                var routeWindows = ActionTagOptions.GetRouteWindows();
                bool found = false;
                for (int i = 0; i < routeWindows.Count; i++)
                {
                    if (routeWindows[i] != null && routeWindows[i].Matches(currentWindow))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return EditorGUIUtility.singleLineHeight * 2 + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing * 2;
                }
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private static void GetFilteredRouteWindows(Type[] allowedTypes, out List<RouteWindow> filteredWindows, out string[] filteredLabels)
        {
            var allRouteWindows = ActionTagOptions.GetRouteWindows();
            filteredWindows = new List<RouteWindow>();
            List<string> labels = new List<string>();

            for (int i = 0; i < allRouteWindows.Count; i++)
            {
                RouteWindow rw = allRouteWindows[i];
                if (rw == null) continue;

                if (allowedTypes != null && allowedTypes.Length > 0)
                {
                    bool isMatch = false;
                    for (int t = 0; t < allowedTypes.Length; t++)
                    {
                        if (allowedTypes[t] != null && allowedTypes[t].IsAssignableFrom(rw.GetType()))
                        {
                            isMatch = true;
                            break;
                        }
                    }
                    if (!isMatch) continue;
                }

                filteredWindows.Add(rw);
                labels.Add(rw.EditorLabel);
            }

            filteredLabels = labels.ToArray();
        }

        private static string FormatAllowedTypes(Type[] allowedTypes)
        {
            if (allowedTypes == null || allowedTypes.Length == 0) return "任意窗口";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(allowedTypes[i].Name);
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  String 字段向下兼容绘制逻辑
        // ═══════════════════════════════════════════════════════════════════

        private static void DrawStringField(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            string[] tagOptions = ActionTagOptions.GetComboWindowTags();
            string currentValue = property.stringValue ?? string.Empty;

            if (tagOptions == null || tagOptions.Length == 0)
            {
                Rect helpRect = new Rect(position.x, position.y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, "未配置窗口标签，请在 ActionTagConfig 中配置。", MessageType.Warning);

                Rect fieldRect = new Rect(position.x, position.y + HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                property.stringValue = EditorGUI.TextField(fieldRect, label, currentValue);
                EditorGUI.EndProperty();
                return;
            }

            int matchIndex = Array.IndexOf(tagOptions, currentValue);
            bool isUnregistered = !string.IsNullOrEmpty(currentValue) && matchIndex < 0;

            if (!isUnregistered)
            {
                string[] displayOptions = new string[tagOptions.Length + 1];
                displayOptions[0] = "<None>";
                Array.Copy(tagOptions, 0, displayOptions, 1, tagOptions.Length);

                int selectedIndex = string.IsNullOrEmpty(property.stringValue) ? 0 : matchIndex + 1;
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = newIndex == 0 ? string.Empty : tagOptions[newIndex - 1];
                }
            }
            else
            {
                Rect textRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                string editedValue = EditorGUI.TextField(textRect, $"{label.text} [未注册]", currentValue);
                GUI.color = oldColor;

                if (editedValue != currentValue)
                {
                    property.stringValue = editedValue;
                }

                float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                Rect helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, "此标签未在 ActionTagConfig 中注册。", MessageType.Warning);

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
                    if (replaceIndex == 1) property.stringValue = string.Empty;
                    else if (replaceIndex > 1) property.stringValue = replaceOptions[replaceIndex];
                }
            }

            HandleContextMenu(position);
            EditorGUI.EndProperty();
        }

        private static float GetStringFieldHeight(SerializedProperty property)
        {
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

        private static void HandleContextMenu(Rect position)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick && position.Contains(evt.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("在 Project 中定位 ActionTagConfig"), false, () =>
                {
                    string[] guids = AssetDatabase.FindAssets("t:ActionTagConfig");
                    if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:ActionTagConfigAsset");
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
