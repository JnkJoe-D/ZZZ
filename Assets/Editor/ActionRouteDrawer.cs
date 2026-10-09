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
    /// 路由自定义属性绘制器
    /// 采用自动迭代模式 新增字段时无需修改此绘制器
    /// 仅对需要特殊渲染的字段进行覆盖
    /// 支持怪物路由中的非法输入条件检测与一键自愈修复
    /// </summary>
    [CustomPropertyDrawer(typeof(ActionRoute))]
    public sealed class ActionRouteDrawer : PropertyDrawer
    {
        private const float LineGap = 2f;
        private const float WarningBoxHeight = 36f;
        private const float CleanButtonHeight = 20f;

        private const float TargetListHeaderHeight = 20f;
        private const float TargetCardHeaderHeight = 22f;
        private const float TargetCardPadding = 4f;
        private const float TargetCardSpacing = 4f;
        private const float AddTargetButtonHeight = 22f;

        // 需要特殊渲染的字段名称集合
        private static readonly HashSet<string> CustomDrawnFields = new() { "RequiredWindowTag", "Targets" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 折叠头部 展示分类 标签 目标摘要
            Rect line = NextLine(ref position);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, BuildHeader(property), true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            // 怪物路由合规性检测与修复
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

            // 自动迭代所有子属性
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

                if (iter.name == "Targets")
                {
                    DrawTargetsList(ref position, iter);
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

                if (iter.name == "Targets")
                {
                    height += GetTargetsListHeight(iter) + LineGap;
                    continue;
                }

                height += EditorGUI.GetPropertyHeight(iter, true) + LineGap;
            }

            return height;
        }

        // 标题构建

        private static GUIContent BuildHeader(SerializedProperty property)
        {
            SerializedProperty categoryProperty = property.FindPropertyRelative("Category");
            string category = "路由";
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
            
            string targetName = "无";
            int eventCount = 0;
            string firstEventName = null;

            SerializedProperty targetsProp = property.FindPropertyRelative("Targets");
            if (targetsProp != null && targetsProp.isArray)
            {
                for (int i = 0; i < targetsProp.arraySize; i++)
                {
                    SerializedProperty elem = targetsProp.GetArrayElementAtIndex(i);
                    SerializedProperty actionProp = elem.FindPropertyRelative("Action");
                    if (actionProp != null)
                    {
                        if (actionProp.objectReferenceValue != null)
                        {
                            targetName = actionProp.objectReferenceValue.name;
                        }
                    }

                    SerializedProperty eventProp = elem.FindPropertyRelative("RouteExecuteEvent");
                    if (eventProp != null)
                    {
                        eventCount++;
                        if (firstEventName == null && eventProp.enumValueIndex >= 0 && eventProp.enumValueIndex < eventProp.enumDisplayNames.Length)
                        {
                            firstEventName = eventProp.enumDisplayNames[eventProp.enumValueIndex];
                        }
                    }
                }
            }

            // 若未配置动作但配置了事件 则以首个事件作为标题
            if (targetName == "无" && firstEventName != null)
            {
                targetName = $"事件 {firstEventName}";
            }

            // 若配置了事件 在动作名后面追加事件数量摘要
            if (eventCount > 0 && !targetName.StartsWith("事件", StringComparison.Ordinal))
            {
                targetName = $"{targetName} 加{eventCount}个事件";
            }
            
            if (HasMonsterRouteWarning(property, out _))
            {
                return new GUIContent($"非法输入配置 {category} / {tag} -> {targetName}");
            }

            SerializedProperty timingProp = property.FindPropertyRelative("ArbitrationTiming");
            string timingTag = string.Empty;
            if (timingProp != null && timingProp.intValue == (int)RouteArbitrationTiming.Immediate)
            {
                timingTag = " 即时";
            }

            return new GUIContent($"{category} / {tag} -> {targetName}{timingTag}");
        }

        // 目标专属卡片渲染与约束

        private static void EnsureTargetsIntegrity(SerializedProperty targetsProp)
        {
            if (targetsProp == null || !targetsProp.isArray) return;

            int actionCount = 0;
            int primaryActionIndex = -1;

            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                var elem = targetsProp.GetArrayElementAtIndex(i);
                if (elem == null) continue;
                var actionProp = elem.FindPropertyRelative("Action");
                if (actionProp != null)
                {
                    actionCount++;
                    if (primaryActionIndex < 0)
                    {
                        primaryActionIndex = i;
                    }
                    else if (actionProp.objectReferenceValue != null)
                    {
                        var primaryElem = targetsProp.GetArrayElementAtIndex(primaryActionIndex);
                        var primaryActionProp = primaryElem?.FindPropertyRelative("Action");
                        if (primaryActionProp?.objectReferenceValue == null)
                        {
                            primaryActionIndex = i;
                        }
                    }
                }
            }

            if (actionCount > 1)
            {
                // 多于一个动作目标 保留有效的 清理多余项
                for (int i = targetsProp.arraySize - 1; i >= 0; i--)
                {
                    if (i == primaryActionIndex) continue;
                    var elem = targetsProp.GetArrayElementAtIndex(i);
                    if (elem != null && elem.FindPropertyRelative("Action") != null)
                    {
                        targetsProp.DeleteArrayElementAtIndex(i);
                        if (i < primaryActionIndex) primaryActionIndex--;
                    }
                }
                targetsProp.serializedObject.ApplyModifiedProperties();
            }
            else if (actionCount == 0)
            {
                // 没有动作目标时 自动在列表首位插入动作目标
                targetsProp.InsertArrayElementAtIndex(0);
                var elem = targetsProp.GetArrayElementAtIndex(0);
                elem.managedReferenceValue = new ActionRouteTarget();
                elem.isExpanded = true;
                targetsProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private static float GetTargetsListHeight(SerializedProperty targetsProp)
        {
            if (targetsProp == null || !targetsProp.isArray) return 0f;

            float height = TargetListHeaderHeight + LineGap;
            if (!targetsProp.isExpanded)
            {
                return height;
            }

            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                var elem = targetsProp.GetArrayElementAtIndex(i);
                if (elem == null) continue;
                float cardHeight = TargetCardHeaderHeight;
                if (elem.isExpanded)
                {
                    cardHeight += (TargetCardPadding * 2f) + (EditorGUIUtility.singleLineHeight * 2f) + LineGap;
                }
                height += cardHeight + TargetCardSpacing;
            }

            height += AddTargetButtonHeight + LineGap;
            return height;
        }

        private static void DrawTargetsList(ref Rect position, SerializedProperty targetsProp)
        {
            if (targetsProp == null || !targetsProp.isArray) return;

            // 维护动作恒为一个的约束与自愈
            EnsureTargetsIntegrity(targetsProp);

            int actionCount = 0;
            int eventCount = 0;
            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                var elem = targetsProp.GetArrayElementAtIndex(i);
                if (elem == null) continue;
                if (elem.FindPropertyRelative("Action") != null) actionCount++;
                else if (elem.FindPropertyRelative("RouteExecuteEvent") != null) eventCount++;
            }

            // 列表折叠头部
            Rect headerRect = NextRect(ref position, TargetListHeaderHeight);
            string listLabel = $"执行目标列表  动作 {actionCount} 项  事件 {eventCount} 项";
            targetsProp.isExpanded = EditorGUI.Foldout(headerRect, targetsProp.isExpanded, listLabel, true, EditorStyles.foldoutHeader);

            if (!targetsProp.isExpanded)
            {
                return;
            }

            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                if (i >= targetsProp.arraySize) break;
                var elem = targetsProp.GetArrayElementAtIndex(i);
                if (elem == null) break;

                bool isAction = elem.FindPropertyRelative("Action") != null;

                float cardHeight = TargetCardHeaderHeight;
                if (elem.isExpanded)
                {
                    cardHeight += (TargetCardPadding * 2f) + (EditorGUIUtility.singleLineHeight * 2f) + LineGap;
                }

                Rect cardRect = NextRect(ref position, cardHeight);
                position.y += (TargetCardSpacing - LineGap);

                if (DrawTargetCard(cardRect, elem, i, targetsProp.arraySize, isAction, targetsProp))
                {
                    return;
                }
            }

            // 底部添加按钮 恒定添加执行事件
            Rect addBtnRect = NextRect(ref position, AddTargetButtonHeight);
            addBtnRect.x += 12f;
            addBtnRect.width -= 24f;

            if (GUI.Button(addBtnRect, new GUIContent("+ 添加执行事件", "为该路由添加伴随触发的系统事件"), EditorStyles.miniButton))
            {
                int newIndex = targetsProp.arraySize;
                targetsProp.InsertArrayElementAtIndex(newIndex);
                var newElem = targetsProp.GetArrayElementAtIndex(newIndex);
                newElem.managedReferenceValue = new EventRouteTarget
                {
                    RouteExecuteEvent = ExecuteEvent.None,
                    MaxExecuteCount = 1
                };
                newElem.isExpanded = true;
                targetsProp.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
        }

        private static bool DrawTargetCard(Rect cardRect, SerializedProperty elem, int index, int count, bool isAction, SerializedProperty targetsProp)
        {
            // 配色方案 动作使用科技青蓝 事件使用暖金琥珀
            Color bgColor = isAction
                ? (EditorGUIUtility.isProSkin ? new Color(0.12f, 0.24f, 0.35f, 0.55f) : new Color(0.80f, 0.90f, 0.98f, 0.7f))
                : (EditorGUIUtility.isProSkin ? new Color(0.35f, 0.22f, 0.10f, 0.55f) : new Color(0.98f, 0.90f, 0.80f, 0.7f));

            Color headerBg = isAction
                ? (EditorGUIUtility.isProSkin ? new Color(0.15f, 0.32f, 0.46f, 0.85f) : new Color(0.70f, 0.85f, 0.96f, 0.9f))
                : (EditorGUIUtility.isProSkin ? new Color(0.44f, 0.28f, 0.12f, 0.85f) : new Color(0.95f, 0.82f, 0.68f, 0.9f));

            Color borderColor = isAction
                ? (EditorGUIUtility.isProSkin ? new Color(0.20f, 0.48f, 0.70f, 0.6f) : new Color(0.50f, 0.70f, 0.85f, 0.8f))
                : (EditorGUIUtility.isProSkin ? new Color(0.70f, 0.45f, 0.18f, 0.6f) : new Color(0.85f, 0.65f, 0.40f, 0.8f));

            Color accentBarColor = isAction
                ? new Color(0.20f, 0.65f, 0.95f, 1f)
                : new Color(0.95f, 0.60f, 0.15f, 1f);

            // 绘制底色与边框
            EditorGUI.DrawRect(cardRect, bgColor);
            EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 1f), borderColor);
            EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.yMax - 1f, cardRect.width, 1f), borderColor);
            EditorGUI.DrawRect(new Rect(cardRect.xMax - 1f, cardRect.y, 1f, cardRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, 4f, cardRect.height), accentBarColor);

            // 头部条
            Rect cardHeaderRect = new Rect(cardRect.x, cardRect.y, cardRect.width, TargetCardHeaderHeight);
            EditorGUI.DrawRect(cardHeaderRect, headerBg);

            // 构建标题文本
            string titleText;
            if (isAction)
            {
                var actionProp = elem.FindPropertyRelative("Action");
                string actName = actionProp?.objectReferenceValue != null ? actionProp.objectReferenceValue.name : "未配置动作";
                titleText = $"动作  {actName}";
            }
            else
            {
                var eventProp = elem.FindPropertyRelative("RouteExecuteEvent");
                string evtName = "无";
                if (eventProp != null && eventProp.enumValueIndex >= 0 && eventProp.enumValueIndex < eventProp.enumDisplayNames.Length)
                {
                    evtName = eventProp.enumDisplayNames[eventProp.enumValueIndex];
                }
                titleText = $"事件  {evtName}";
            }

            // 右侧按钮宽度
            float btnAreaWidth = 72f;
            Rect foldoutRect = new Rect(cardRect.x + 8f, cardRect.y + 2f, cardRect.width - btnAreaWidth - 12f, 18f);
            elem.isExpanded = EditorGUI.Foldout(foldoutRect, elem.isExpanded, titleText, true, EditorStyles.boldLabel);

            // 右侧控制按钮
            float btnX = cardRect.xMax - btnAreaWidth - 4f;
            Rect upBtnRect = new Rect(btnX, cardRect.y + 2f, 20f, 18f);
            Rect downBtnRect = new Rect(btnX + 22f, cardRect.y + 2f, 20f, 18f);
            Rect lockOrDelBtnRect = new Rect(btnX + 44f, cardRect.y + 2f, 26f, 18f);

            // 上移按钮
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUI.Button(upBtnRect, new GUIContent("▲", "在执行列表中上移"), EditorStyles.miniButtonLeft))
                {
                    targetsProp.MoveArrayElement(index, index - 1);
                    targetsProp.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return true;
                }
            }

            // 下移按钮
            using (new EditorGUI.DisabledScope(index >= count - 1))
            {
                if (GUI.Button(downBtnRect, new GUIContent("▼", "在执行列表中下移"), EditorStyles.miniButtonMid))
                {
                    targetsProp.MoveArrayElement(index, index + 1);
                    targetsProp.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return true;
                }
            }

            // 第三个按钮 动作目标恒为锁定不可删 仅事件目标可删除
            if (isAction)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUI.Button(lockOrDelBtnRect, new GUIContent("锁", "动作目标恒定保留不可删除"), EditorStyles.miniButtonRight);
                }
            }
            else
            {
                Color oldGuiColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f, 1f);
                if (GUI.Button(lockOrDelBtnRect, new GUIContent("删", "删除此事件目标"), EditorStyles.miniButtonRight))
                {
                    targetsProp.DeleteArrayElementAtIndex(index);
                    targetsProp.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return true;
                }
                GUI.backgroundColor = oldGuiColor;
            }

            // 卡片主体内容
            if (elem.isExpanded)
            {
                float bodyY = cardHeaderRect.yMax + TargetCardPadding;
                float contentX = cardRect.x + 14f;
                float contentWidth = cardRect.width - 24f;

                if (isAction)
                {
                    var actionProp = elem.FindPropertyRelative("Action");
                    if (actionProp != null)
                    {
                        Rect actionRect = new Rect(contentX, bodyY, contentWidth, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(actionRect, actionProp, new GUIContent("目标动作", "切换并播放的目标动作资产"));
                    }
                    bodyY += EditorGUIUtility.singleLineHeight + LineGap;

                    var validateProp = elem.FindPropertyRelative("ValidateSkillRequirement");
                    if (validateProp != null)
                    {
                        Rect valRect = new Rect(contentX, bodyY, contentWidth, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(valRect, validateProp, new GUIContent("校验技能需求", "是否校验释放条件及技能消耗"));
                    }
                }
                else
                {
                    var eventProp = elem.FindPropertyRelative("RouteExecuteEvent");
                    if (eventProp != null)
                    {
                        Rect evtRect = new Rect(contentX, bodyY, contentWidth, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(evtRect, eventProp, new GUIContent("系统事件", "命中后触发的系统路由事件"));
                    }
                    bodyY += EditorGUIUtility.singleLineHeight + LineGap;

                    var limitProp = elem.FindPropertyRelative("_executeCountLimit");
                    if (limitProp != null)
                    {
                        Rect limRect = new Rect(contentX, bodyY, contentWidth, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(limRect, limitProp, new GUIContent("最大执行次数", "当前动作周期内最大执行次数 默认一 小于等于零表示无限制允许重复执行"));
                    }
                }
            }

            return false;
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
                tagProperty.stringValue = EditorGUI.TextField(line, "指定窗口标签", tagProperty.stringValue);
                return;
            }

            string currentValue = tagProperty.stringValue ?? string.Empty;
            string[] popupOptions = BuildPopupOptions(tags, currentValue, out int currentIndex);

            Color oldColor = GUI.color;
            if (currentIndex > 0 && !string.Equals(popupOptions[currentIndex], currentValue, StringComparison.Ordinal))
            {
                GUI.color = Color.yellow;
            }

            int newIndex = EditorGUI.Popup(line, "指定窗口标签", currentIndex, popupOptions);
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
                    warningMsg = "非法配置 怪物路由配置了按键输入触发器 怪物无法响应硬件输入";
                    return true;
                }

                SerializedProperty modifiersProp = triggerProp.FindPropertyRelative("Modifiers");
                if (modifiersProp != null && modifiersProp.arraySize > 0)
                {
                    warningMsg = "非法配置 怪物路由触发器中配置了输入修饰符 怪物没有输入组件 运行时恒为不满足";
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
                            warningMsg = "非法配置 怪物路由中配置了角色专属条件 怪物无法满足此条件";
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
