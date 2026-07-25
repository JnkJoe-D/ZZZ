using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// SubclassSelectorAttribute 的自定义属性绘制器。
    /// 为 SerializeReference 的多态类型字段/列表提供优雅的下拉选择器。
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

                SerializedProperty endProperty = property.GetEndProperty();
                SerializedProperty childProperty = property.Copy();
                bool enterChildren = true;

                while (childProperty.NextVisible(enterChildren) && !SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    enterChildren = false;
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
                var attr = type.GetCustomAttribute<SubclassDisplayNameAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
                {
                    return attr.DisplayName;
                }
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
        /// 弹出子类选择上下文菜单
        /// </summary>
        private static void ShowTypeSelectionMenu(SerializedProperty property, Type targetType)
        {
            GenericMenu menu = new GenericMenu();

            // 选项 1: 置空 <Null>
            menu.AddItem(new GUIContent("<Null>"), property.managedReferenceValue == null, () =>
            {
                property.serializedObject.Update();
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            // 选项 2: 列出所有可用的非抽象子类
            List<Type> derivedTypes = GetDerivedTypes(targetType);
            foreach (Type type in derivedTypes)
            {
                string menuPath = GetTypeMenuName(type);
                bool isSelected = property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;

                menu.AddItem(new GUIContent(menuPath), isSelected, () =>
                {
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
                });
            }

            menu.ShowAsContext();
        }

        private static string GetTypeMenuName(Type type)
        {
            var attr = type.GetCustomAttribute<SubclassDisplayNameAttribute>();
            string name = (attr != null && !string.IsNullOrEmpty(attr.DisplayName)) ? attr.DisplayName : type.Name;
            
            // 可以根据命名空间自动分类
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                string ns = type.Namespace;
                // 去掉公用前缀 Game.Logic 等，方便更直观分类
                ns = ns.Replace("Game.Logic", "").TrimStart('.');
                if (!string.IsNullOrEmpty(ns))
                {
                    return $"{ns}/{name}";
                }
            }
            return name;
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
