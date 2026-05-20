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
    [CustomPropertyDrawer(typeof(ActionRoute))]
    public sealed class ActionRouteDrawer : PropertyDrawer
    {
        private const float LineGap = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect line = NextLine(ref position);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, BuildHeader(property), true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            DrawProperty(ref position, property, "Category");
            DrawWindowTag(ref position, property.FindPropertyRelative("RequiredWindowTag"));
            DrawProperty(ref position, property, "TriggerMode");
            DrawProperty(ref position, property, "ModifierCheckTiming");
            DrawProperty(ref position, property, "EventType");
            DrawProperty(ref position, property, "RequiredType");
            DrawProperty(ref position, property, "RequiredPhase");
            DrawProperty(ref position, property, "Modifier");
            DrawProperty(ref position, property, "ModifierRequiredKey");
            DrawProperty(ref position, property, "InverseKeyStatus");
            DrawProperty(ref position, property, "ModifierConditions", includeChildren: true);
            DrawProperty(ref position, property, "ExecuteType");
            DrawProperty(ref position, property, "ExecuteAction");
            DrawProperty(ref position, property, "RouteExecuteEvent");
            DrawProperty(ref position, property, "ExtraConditions", includeChildren: true);
            DrawProperty(ref position, property, "Priority");
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + LineGap;
            height += PropertyHeight(property, "Category");
            height += PropertyHeight(property, "RequiredWindowTag");
            height += PropertyHeight(property, "TriggerMode");
            height += PropertyHeight(property, "ModifierCheckTiming");
            height += PropertyHeight(property, "EventType");
            height += PropertyHeight(property, "RequiredType");
            height += PropertyHeight(property, "RequiredPhase");
            height += PropertyHeight(property, "Modifier");
            height += PropertyHeight(property, "ModifierRequiredKey");
            height += PropertyHeight(property, "InverseKeyStatus");
            height += PropertyHeight(property, "ModifierConditions", includeChildren: true);
            height += PropertyHeight(property, "ExecuteType");
            height += PropertyHeight(property, "ExecuteAction");
            height += PropertyHeight(property, "RouteExecuteEvent");
            height += PropertyHeight(property, "ExtraConditions", includeChildren: true);
            height += PropertyHeight(property, "Priority");
            return height;
        }

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

        private static void DrawProperty(
            ref Rect position,
            SerializedProperty root,
            string propertyName,
            bool includeChildren = false)
        {
            SerializedProperty child = root.FindPropertyRelative(propertyName);
            if (child == null)
            {
                return;
            }

            if (!IsVisible(child))
            {
                return;
            }

            Rect rect = NextPropertyRect(ref position, child, includeChildren);
            EditorGUI.PropertyField(rect, child, includeChildren);
        }

        private static bool IsVisible(SerializedProperty property)
        {
            var field = typeof(ActionRoute).GetField(property.name);
            if (field == null)
            {
                return true;
            }

            var showIf = field.GetCustomAttribute<ShowIfAttribute>();
            return ShowIfDrawer.CheckVisible(property, showIf);
        }

        private static float PropertyHeight(SerializedProperty root, string propertyName, bool includeChildren = false)
        {
            SerializedProperty child = root.FindPropertyRelative(propertyName);
            if (child == null || !IsVisible(child))
            {
                return 0f;
            }
            return EditorGUI.GetPropertyHeight(child, includeChildren) + LineGap;
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
