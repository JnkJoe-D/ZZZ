using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Game.Framework;
using Game.Editor.Framework;
using Game.GamePlay;

namespace Game.Editor.ActionConfig
{
    /// <summary>
    /// ActionRoute 的自定义属性绘制器。
    /// 采用自动迭代模式：新增字段时无需修改此 Drawer。
    /// 仅对需要特殊渲染的字段（RequiredWindowTag）进行覆盖。
    /// 支持怪物路由中的非法输入条件检测与一键自愈修复。
    /// </summary>
    [CustomPropertyDrawer(typeof(ActionRoute))]
    public sealed class ActionRouteDrawer : PropertyDrawer
    {
        private const float LineGap = 2f;
        private const float WarningBoxHeight = 36f;
        private const float CleanButtonHeight = 20f;

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

            // ── 怪物路由合规性检测与修复 ──
            if (HasMonsterRouteWarning(property, out string warningMsg))
            {
                Rect warnRect = NextRect(ref position, WarningBoxHeight);
                EditorGUI.HelpBox(warnRect, warningMsg, MessageType.Warning);

                Rect btnRect = NextRect(ref position, CleanButtonHeight);
                if (GUI.Button(btnRect, "一键清理怪物非法输入条件与角色条件", EditorStyles.miniButton))
                {
                    CleanInvalidMonsterConditions(property);
                }
            }

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

            // 怪物非法输入警告高度
            if (HasMonsterRouteWarning(property, out _))
            {
                height += WarningBoxHeight + CleanButtonHeight + (LineGap * 2f);
            }

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

            SerializedProperty triggerProp = property.FindPropertyRelative("TriggerStrategy");
            string tag = "-";
            if (triggerProp != null)
            {
                SerializedProperty windowProp = triggerProp.FindPropertyRelative("RequiredWindow");
                if (windowProp != null && windowProp.managedReferenceValue is ATEditor.RouteWindow rw && !string.IsNullOrEmpty(rw.Tag))
                {
                    tag = rw.EditorLabel;
                }
                else
                {
                    string legacyTag = triggerProp.FindPropertyRelative("RequiredWindowTag")?.stringValue;
                    if (!string.IsNullOrEmpty(legacyTag)) tag = legacyTag;
                }
            }
            
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
            
            if (HasMonsterRouteWarning(property, out _))
            {
                return new GUIContent($"⚠️ [非法输入配置] {category} / {tag} -> {targetName}");
            }

            return new GUIContent($"{category} / {tag} -> {targetName}");
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

        private static string[] _cachedBasePopupOptions;
        private static string[] _lastTagsRef;

        private static string[] GetBasePopupOptions(string[] tags)
        {
            if (_cachedBasePopupOptions != null && ReferenceEquals(_lastTagsRef, tags))
            {
                return _cachedBasePopupOptions;
            }

            _lastTagsRef = tags;
            List<string> options = new(tags.Length + 1) { "<Empty>" };
            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag) && !options.Contains(tag))
                {
                    options.Add(tag);
                }
            }
            _cachedBasePopupOptions = options.ToArray();
            return _cachedBasePopupOptions;
        }

        private static string[] BuildPopupOptions(string[] tags, string currentValue, out int currentIndex)
        {
            string[] baseOptions = GetBasePopupOptions(tags);

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                currentIndex = 0;
                return baseOptions;
            }

            currentIndex = Array.IndexOf(baseOptions, currentValue);
            if (currentIndex >= 0)
            {
                return baseOptions;
            }

            string customOption = $"[Unregistered] {currentValue}";
            string[] result = new string[baseOptions.Length + 1];
            result[0] = baseOptions[0];
            result[1] = customOption;
            Array.Copy(baseOptions, 1, result, 2, baseOptions.Length - 1);
            currentIndex = 1;
            return result;
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

        private static Rect NextRect(ref Rect position, float height)
        {
            Rect rect = new Rect(position.x, position.y, position.width, height);
            position.y += height + LineGap;
            return rect;
        }

        // ────────────────── 怪物路由合规性校验与自愈 ──────────────────

        private static bool HasMonsterRouteWarning(SerializedProperty property, out string warningMsg)
        {
            warningMsg = null;
            if (property.serializedObject.targetObject is not MonsterActionConfigAsset)
            {
                return false;
            }

            SerializedProperty triggerProp = property.FindPropertyRelative("TriggerStrategy");
            if (triggerProp != null)
            {
                string triggerTypeName = triggerProp.managedReferenceFullTypename;
                if (!string.IsNullOrEmpty(triggerTypeName) && triggerTypeName.Contains(nameof(IntentCommandTrigger)))
                {
                    warningMsg = "【非法配置】怪物路由配置了按键输入触发器 (IntentCommandTrigger)，怪物无法响应硬件输入！";
                    return true;
                }

                SerializedProperty modifiersProp = triggerProp.FindPropertyRelative("Modifiers");
                if (modifiersProp != null && modifiersProp.arraySize > 0)
                {
                    warningMsg = "【非法配置】怪物路由触发器中配置了输入修饰符 (Modifiers)，怪物没有输入组件，该条件在运行时将恒为 false！";
                    return true;
                }
            }

            SerializedProperty extraProp = property.FindPropertyRelative("ExtraConditions");
            if (extraProp != null && extraProp.isArray)
            {
                for (int i = 0; i < extraProp.arraySize; i++)
                {
                    var elem = extraProp.GetArrayElementAtIndex(i);
                    string fullTypeName = elem.managedReferenceFullTypename;
                    if (!string.IsNullOrEmpty(fullTypeName))
                    {
                        Type t = GetTypeFromManagedReferenceFullTypename(fullTypeName);
                        if (t != null && typeof(RoleConditionBase).IsAssignableFrom(t))
                        {
                            warningMsg = $"【非法配置】怪物路由中配置了角色专属条件 ({t.Name})，怪物无法满足此条件！";
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static Type GetTypeFromManagedReferenceFullTypename(string fullTypeName)
        {
            int splitIndex = fullTypeName.IndexOf(' ');
            if (splitIndex < 0) return Type.GetType(fullTypeName);

            string assemblyName = fullTypeName.Substring(0, splitIndex);
            string realTypeName = fullTypeName.Substring(splitIndex + 1);

            Type type = Type.GetType($"{realTypeName}, {assemblyName}");
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(realTypeName))
                    .FirstOrDefault(t => t != null);
            }
            return type;
        }

        private static void CleanInvalidMonsterConditions(SerializedProperty property)
        {
            var targetObjs = property.serializedObject.targetObjects;
            if (targetObjs != null && targetObjs.Length > 0)
            {
                Undo.RecordObjects(targetObjs, "Clean Invalid Monster Conditions");
            }

            SerializedProperty triggerProp = property.FindPropertyRelative("TriggerStrategy");
            if (triggerProp != null)
            {
                string triggerTypeName = triggerProp.managedReferenceFullTypename;
                if (!string.IsNullOrEmpty(triggerTypeName) && triggerTypeName.Contains(nameof(IntentCommandTrigger)))
                {
                    triggerProp.managedReferenceValue = null;
                }
                else
                {
                    SerializedProperty modifiersProp = triggerProp.FindPropertyRelative("Modifiers");
                    if (modifiersProp != null && modifiersProp.arraySize > 0)
                    {
                        modifiersProp.ClearArray();
                    }
                }
            }

            SerializedProperty extraProp = property.FindPropertyRelative("ExtraConditions");
            if (extraProp != null && extraProp.isArray)
            {
                for (int i = extraProp.arraySize - 1; i >= 0; i--)
                {
                    var elem = extraProp.GetArrayElementAtIndex(i);
                    string fullTypeName = elem.managedReferenceFullTypename;
                    if (!string.IsNullOrEmpty(fullTypeName))
                    {
                        Type t = GetTypeFromManagedReferenceFullTypename(fullTypeName);
                        if (t != null && typeof(RoleConditionBase).IsAssignableFrom(t))
                        {
                            extraProp.DeleteArrayElementAtIndex(i);
                        }
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();

            if (targetObjs != null)
            {
                foreach (var obj in targetObjs)
                {
                    if (obj != null) EditorUtility.SetDirty(obj);
                }
            }
        }
    }
}
