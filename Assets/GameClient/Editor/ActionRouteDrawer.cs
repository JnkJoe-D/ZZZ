using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Game.Framework;
using Game.Editor.Framework;
using Game.Logic;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// ActionRoute 的自定义属性绘制器。
    /// 采用自动迭代模式：新增字段时无需修改此 Drawer。
    /// 仅对需要特殊渲染的字段（RequiredWindowTag）进行覆盖。
    /// </summary>
    [CustomPropertyDrawer(typeof(ActionRoute))]
    public sealed class ActionRouteDrawer : PropertyDrawer
    {
        private const float LineGap = 2f;

        // 需要特殊渲染的字段名称集合
        private static readonly HashSet<string> CustomDrawnFields = new() { "RequiredWindowTag" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // ── 折叠头部（自定义 Header，展示 Category/Tag/Target 摘要） ──
            Rect line = NextLine(ref position);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, BuildHeader(property), true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            // ── 自动迭代所有子属性 ──
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty iter = property.Copy();
            bool enterChildren = true;

            while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, endProperty))
            {
                enterChildren = false;

                // ShowIf 可见性检测
                if (!IsVisible(iter)) continue;

                // 特殊字段覆盖渲染
                if (iter.name == "RequiredWindowTag")
                {
                    DrawWindowTag(ref position, iter);
                    continue;
                }

                // 默认渲染（自动支持 SubclassSelector 等 PropertyDrawer）
                Rect rect = NextPropertyRect(ref position, iter, true);
                EditorGUI.PropertyField(rect, iter, true);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + LineGap; // Foldout header

            // 自动迭代计算高度
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty iter = property.Copy();
            bool enterChildren = true;

            while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, endProperty))
            {
                enterChildren = false;
                if (!IsVisible(iter)) continue;
                height += EditorGUI.GetPropertyHeight(iter, true) + LineGap;
            }

            return height;
        }

        // ────────────────── Header 构建 ──────────────────

        private static GUIContent BuildHeader(SerializedProperty property)
        {
            SerializedProperty categoryProperty = property.FindPropertyRelative("Category");
            string category = "Route";
            if (categoryProperty != null &&
                categoryProperty.enumValueIndex >= 0 &&
                categoryProperty.enumValueIndex < categoryProperty.enumDisplayNames.Length)
            {
                category = categoryProperty.enumDisplayNames[categoryProperty.enumValueIndex];
            }

            string tag = property.FindPropertyRelative("RequiredWindowTag")?.stringValue;
            
            string targetName = "None";
            SerializedProperty executeTypeProp = property.FindPropertyRelative("ExecuteType");
            if (executeTypeProp != null)
            {
                // 用 intValue 获取底层枚举实际绑定的整数数值（如 0, 10, 20），enumValueIndex 返回的是 0, 1, 2 索引，强转会导致数值不匹配
                ExecuteTarget target = (ExecuteTarget)executeTypeProp.intValue;
                if (target == ExecuteTarget.Action)
                {
                    SerializedProperty executeAction = property.FindPropertyRelative("ExecuteAction");
                    targetName = executeAction?.objectReferenceValue != null ? executeAction.objectReferenceValue.name : "None";
                }
                else if (target == ExecuteTarget.Event)
                {
                    SerializedProperty routeExecuteEvent = property.FindPropertyRelative("RouteExecuteEvent");
                    if (routeExecuteEvent != null &&
                        routeExecuteEvent.enumValueIndex >= 0 &&
                        routeExecuteEvent.enumValueIndex < routeExecuteEvent.enumDisplayNames.Length)
                    {
                        targetName = $"[Event] {routeExecuteEvent.enumDisplayNames[routeExecuteEvent.enumValueIndex]}";
                    }
                    else
                    {
                        targetName = "[Event] None";
                    }
                }
            }
            else
            {
                SerializedProperty executeAction = property.FindPropertyRelative("ExecuteAction");
                targetName = executeAction?.objectReferenceValue != null ? executeAction.objectReferenceValue.name : "None";
            }
            
            return new GUIContent($"{category} / {tag ?? "-"} -> {targetName}");
        }

        // ────────────────── 特殊字段渲染 ──────────────────

        private static void DrawWindowTag(ref Rect position, SerializedProperty tagProperty)
        {
            Rect line = NextLine(ref position);
            if (tagProperty == null)
            {
                return;
            }

            string[] tags = ActionTagOptions.GetComboWindowTags();
            if (tags.Length == 0)
            {
                tagProperty.stringValue = EditorGUI.TextField(line, "Required Window Tag", tagProperty.stringValue);
                return;
            }

            string currentValue = tagProperty.stringValue ?? string.Empty;
            string[] popupOptions = BuildPopupOptions(tags, currentValue, out int currentIndex);

            Color oldColor = GUI.color;
            if (currentIndex > 0 && !string.Equals(popupOptions[currentIndex], currentValue, StringComparison.Ordinal))
            {
                GUI.color = Color.yellow;
            }

            int newIndex = EditorGUI.Popup(line, "Required Window Tag", currentIndex, popupOptions);
            GUI.color = oldColor;

            tagProperty.stringValue = newIndex <= 0 ? string.Empty : NormalizeSelectedValue(popupOptions[newIndex]);
        }

        // ────────────────── 可见性检测 ──────────────────

        private static readonly Dictionary<string, ShowIfAttribute> _showIfAttributeCache = new();

        private static bool IsVisible(SerializedProperty property)
        {
            if (!_showIfAttributeCache.TryGetValue(property.name, out var showIf))
            {
                var field = typeof(ActionRoute).GetField(property.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                showIf = field?.GetCustomAttribute<ShowIfAttribute>();
                _showIfAttributeCache[property.name] = showIf;
            }

            return ShowIfDrawer.CheckVisible(property, showIf);
        }

        // ────────────────── 工具方法 ──────────────────

        private static string[] BuildPopupOptions(string[] tags, string currentValue, out int currentIndex)
        {
            List<string> options = new()
            {
                "<Empty>"
            };

            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag) && !options.Contains(tag))
                {
                    options.Add(tag);
                }
            }

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                currentIndex = 0;
                return options.ToArray();
            }

            currentIndex = options.IndexOf(currentValue);
            if (currentIndex >= 0)
            {
                return options.ToArray();
            }

            string customOption = $"[Unregistered] {currentValue}";
            options.Insert(1, customOption);
            currentIndex = 1;
            return options.ToArray();
        }

        private static string NormalizeSelectedValue(string selectedOption)
        {
            const string UnregisteredPrefix = "[Unregistered] ";
            if (selectedOption.StartsWith(UnregisteredPrefix, StringComparison.Ordinal))
            {
                return selectedOption.Substring(UnregisteredPrefix.Length);
            }

            return selectedOption;
        }

        private static Rect NextLine(ref Rect position)
        {
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            position.y += EditorGUIUtility.singleLineHeight + LineGap;
            return line;
        }

        private static Rect NextPropertyRect(ref Rect position, SerializedProperty property, bool includeChildren)
        {
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            Rect rect = new Rect(position.x, position.y, position.width, height);
            position.y += height + LineGap;
            return rect;
        }
    }
}
