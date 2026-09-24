using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Game.Framework;
using Game.GamePlay;

namespace Game.Editor.Framework
{
    /// <summary>
    /// SubclassSelectorAttribute 的自定义属性绘制器。
    /// 为 SerializeReference 的多态类型字段/列表提供优雅的下拉选择器。
    /// 支持依据宿主资产（角色/怪物）智能过滤可用子类，并以领域标签分类展示。
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        // 缓存不同基类/接口下的所有子类型，避免频繁反射检索，保证流畅度
        private static readonly Dictionary<Type, List<Type>> InheritedTypesCache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Use SubclassSelector only on [SerializeReference]!");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // 1. 获取目标多态类型（如果是 List/Array，则为元素类型）
            Type targetType = GetTargetType(fieldInfo);
            if (targetType == null)
            {
                EditorGUI.LabelField(position, label.text, "Unknown field type.");
                EditorGUI.EndProperty();
                return;
            }

            ConditionScope hostScope = GetHostScope(property);
            bool hasValue = property.managedReferenceValue != null;
            string currentTypeName = GetCurrentTypeName(property);

            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (!hasValue)
            {
                // 2. 空值状态：在当前行绘制前缀 Label 和下拉选择按钮
                Rect controlRect = EditorGUI.PrefixLabel(currentRect, label);
                if (GUI.Button(controlRect, "<Null>", EditorStyles.popup))
                {
                    ShowTypeSelectionMenu(property, targetType);
                }
            }
            else
            {
                // 3. 有值状态：头部仅绘制折叠箭头与具体类名，去除右侧悬浮按钮以防止点击冲突
                GUIContent headerLabel = new GUIContent($"{label.text} ({currentTypeName})");
                property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, headerLabel, true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // 4. 将具体的类型选择框作为折叠内容的第一行绘制在里面
                    Rect typeDropdownRect = new Rect(currentRect.x, currentRect.y, currentRect.width, EditorGUIUtility.singleLineHeight);
                    Rect controlRect = EditorGUI.PrefixLabel(typeDropdownRect, new GUIContent("Type"));
                    if (GUI.Button(controlRect, currentTypeName, EditorStyles.popup))
                    {
                        ShowTypeSelectionMenu(property, targetType);
                    }

                    currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // 5. 迭代绘制派生类实例的其他具体可序列化字段
                    SerializedProperty endProperty = property.GetEndProperty();
                    SerializedProperty childProperty = property.Copy();
                    bool enterChildren = true;

                    while (childProperty.NextVisible(enterChildren) && !SerializedProperty.EqualContents(childProperty, endProperty))
                    {
                        enterChildren = false; // 只对最外层子级深入，防止重复

                        // 怪物动作下，触发器内的 Modifiers 列表自动隐藏，防止错误配置按键与输入条件
                        if (hostScope == ConditionScope.Monster && childProperty.name == "Modifiers")
                        {
                            continue;
                        }

                        float childHeight = EditorGUI.GetPropertyHeight(childProperty, true);
                        Rect childRect = new Rect(currentRect.x, currentRect.y, currentRect.width, childHeight);

                        EditorGUI.PropertyField(childRect, childProperty, true);
                        currentRect.y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (property.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight; // Header Foldout 行高

            if (property.isExpanded)
            {
                // 增加内部 "Type" 下拉框的高度
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ConditionScope hostScope = GetHostScope(property);
                SerializedProperty endProperty = property.GetEndProperty();
                SerializedProperty childProperty = property.Copy();
                bool enterChildren = true;

                while (childProperty.NextVisible(enterChildren) && !SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    enterChildren = false;

                    // 怪物动作下，隐藏 Modifiers 的占用高度
                    if (hostScope == ConditionScope.Monster && childProperty.name == "Modifiers")
                    {
                        continue;
                    }

                    height += EditorGUI.GetPropertyHeight(childProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }

        private static string GetCurrentTypeName(SerializedProperty property)
        {
            string fullTypeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullTypeName)) return string.Empty;

            int splitIndex = fullTypeName.IndexOf(' ');
            if (splitIndex < 0) return fullTypeName;

            string assemblyName = fullTypeName.Substring(0, splitIndex);
            string realTypeName = fullTypeName.Substring(splitIndex + 1);

            // 尝试通过完整程序集限定名称获取 Type
            Type type = Type.GetType($"{realTypeName}, {assemblyName}");
            if (type == null)
            {
                // 兜底：从所有加载的程序集中查找类型
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(realTypeName))
                    .FirstOrDefault(t => t != null);
            }

            if (type != null)
            {
                return GetFormattedDisplayName(type);
            }

            // 兜底：截取类名去掉命名空间
            int lastDot = realTypeName.LastIndexOf('.');
            return lastDot >= 0 ? realTypeName.Substring(lastDot + 1) : realTypeName;
        }

        /// <summary>
        /// 获取字段的实际类型（解包 Array 或 List）
        /// </summary>
        private static Type GetTargetType(FieldInfo fieldInfo)
        {
            if (fieldInfo == null) return null;

            Type type = fieldInfo.FieldType;
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        /// <summary>
        /// 获取宿主资产所声明的适用实体领域
        /// </summary>
        private static ConditionScope GetHostScope(SerializedProperty property)
        {
            UnityEngine.Object targetObj = property?.serializedObject?.targetObject;
            if (targetObj is MonsterActionConfigAsset)
            {
                return ConditionScope.Monster;
            }
            if (targetObj is RoleActionConfigAsset)
            {
                return ConditionScope.Role;
            }
            if (targetObj is ActionRouteSetAsset setAsset)
            {
                return setAsset.TargetScope;
            }
            return ConditionScope.All;
        }

        /// <summary>
        /// 获取某多态类型（或其接口/基类）上标记的 ConditionScope
        /// </summary>
        private static ConditionScope GetTypeScope(Type type)
        {
            if (type == null) return ConditionScope.Common;

            // 1. 检查类继承链上的 ConditionScopeAttribute
            var attr = type.GetCustomAttribute<ConditionScopeAttribute>(true);
            if (attr != null)
            {
                return attr.Scope;
            }

            // 2. 检查实现的接口上的 ConditionScopeAttribute
            foreach (Type iface in type.GetInterfaces())
            {
                var ifaceAttr = iface.GetCustomAttribute<ConditionScopeAttribute>(true);
                if (ifaceAttr != null)
                {
                    return ifaceAttr.Scope;
                }
            }

            return ConditionScope.Common;
        }

        /// <summary>
        /// 判断某类型是否允许在指定宿主领域下使用
        /// </summary>
        private static bool IsTypeAllowedForHost(Type type, ConditionScope hostScope)
        {
            if (hostScope == ConditionScope.All || hostScope == ConditionScope.None)
            {
                return true;
            }

            ConditionScope typeScope = GetTypeScope(type);

            // 如果类型标记了通用 (Common)，所有宿主均可用
            if ((typeScope & ConditionScope.Common) != 0)
            {
                return true;
            }

            // 检查宿主与类型作用域是否有交集
            return (typeScope & hostScope) != 0;
        }

        /// <summary>
        /// 获取带有 [通用] / [角色] / [怪物] 领域标签的友好显示名称
        /// </summary>
        private static string GetFormattedDisplayName(Type type)
        {
            var attr = type.GetCustomAttribute<SubclassDisplayNameAttribute>();
            string name = (attr != null && !string.IsNullOrEmpty(attr.DisplayName)) ? attr.DisplayName : type.Name;

            if (typeof(ITransitionCondition).IsAssignableFrom(type))
            {
                ConditionScope typeScope = GetTypeScope(type);
                string tag = "[通用]";
                if ((typeScope & ConditionScope.Role) != 0 && (typeScope & ConditionScope.Common) == 0)
                {
                    tag = "[角色]";
                }
                else if ((typeScope & ConditionScope.Monster) != 0 && (typeScope & ConditionScope.Common) == 0)
                {
                    tag = "[怪物]";
                }
                return $"{tag} {name}";
            }

            if (typeof(IRouteTrigger).IsAssignableFrom(type))
            {
                ConditionScope typeScope = GetTypeScope(type);
                if ((typeScope & ConditionScope.Role) != 0 && (typeScope & ConditionScope.Common) == 0)
                {
                    return $"[角色] {name}";
                }
                return $"[通用] {name}";
            }

            return name;
        }

        /// <summary>
        /// 弹出子类选择上下文菜单（已依据宿主领域过滤）
        /// </summary>
        private static void ShowTypeSelectionMenu(SerializedProperty property, Type targetType)
        {
            GenericMenu menu = new GenericMenu();

            // 选项 1: 置空 <Null>
            menu.AddItem(new GUIContent("<Null>"), property.managedReferenceValue == null, () =>
            {
                var targetObjs = property.serializedObject.targetObjects;
                if (targetObjs != null && targetObjs.Length > 0)
                {
                    Undo.RecordObjects(targetObjs, "Clear Subclass Value");
                }

                property.serializedObject.Update();
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();

                if (targetObjs != null)
                {
                    foreach (var obj in targetObjs)
                    {
                        if (obj != null)
                        {
                            EditorUtility.SetDirty(obj);
                        }
                    }
                }
            });

            menu.AddSeparator("");

            ConditionScope hostScope = GetHostScope(property);

            // 选项 2: 列出当前宿主可用的非抽象子类
            List<Type> derivedTypes = GetDerivedTypes(targetType);
            var filteredTypes = derivedTypes.Where(t => IsTypeAllowedForHost(t, hostScope)).ToList();

            foreach (Type type in filteredTypes)
            {
                string menuPath = GetFormattedDisplayName(type);
                bool isSelected = property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;

                menu.AddItem(new GUIContent(menuPath), isSelected, () =>
                {
                    var targetObjs = property.serializedObject.targetObjects;
                    if (targetObjs != null && targetObjs.Length > 0)
                    {
                        Undo.RecordObjects(targetObjs, $"Set Subclass {type.Name}");
                    }

                    property.serializedObject.Update();
                    try
                    {
                        object instance = Activator.CreateInstance(type);
                        property.managedReferenceValue = instance;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SubclassSelector] Failed to instantiate type '{type.FullName}': {e}");
                    }
                    property.serializedObject.ApplyModifiedProperties();

                    if (targetObjs != null)
                    {
                        foreach (var obj in targetObjs)
                        {
                            if (obj != null)
                            {
                                EditorUtility.SetDirty(obj);
                            }
                        }
                    }
                });
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 获取某类型的所有非抽象具体派生类型
        /// </summary>
        private static List<Type> GetDerivedTypes(Type baseType)
        {
            if (InheritedTypesCache.TryGetValue(baseType, out var list))
            {
                return list;
            }

            list = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition);
                    list.AddRange(types);
                }
                catch
                {
                    // 忽略某些动态加载失败的外部程序集
                }
            }

            InheritedTypesCache[baseType] = list;
            return list;
        }
    }
}
