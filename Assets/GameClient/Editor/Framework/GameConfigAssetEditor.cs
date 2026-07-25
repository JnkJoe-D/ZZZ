using System.Reflection;
using Game.Framework;
using Game.Logic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// GameConfigAsset 及所有派生类的通用 Inspector 编辑器。
    /// 自动迭代所有序列化属性，内建 ShowIf 条件可见性检测。
    /// 新增字段时无需修改任何 Editor 代码。
    /// </summary>
    [CustomEditor(typeof(GameConfigAsset), true)]
    public class GameConfigAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // m_Script 字段只读显示
                if (iterator.name == "m_Script")
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(iterator);
                    GUI.enabled = true;
                    continue;
                }

                // ShowIf 可见性检测
                if (!IsPropertyVisible(iterator))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 检查属性是否应该可见（支持 ShowIf 条件属性）。
        /// 通过反射获取字段上的 ShowIfAttribute，利用已有的 ShowIfDrawer.CheckVisible 逻辑。
        /// </summary>
        private bool IsPropertyVisible(SerializedProperty property)
        {
            // 从目标类型获取字段信息
            FieldInfo field = GetFieldFromProperty(property);
            if (field == null) return true;

            var showIf = field.GetCustomAttribute<ShowIfAttribute>();
            if (showIf == null) return true;

            return ShowIfDrawer.CheckVisible(property, showIf);
        }

        /// <summary>
        /// 从 SerializedProperty 解析出对应的 FieldInfo。
        /// 支持直接字段和嵌套路径（如 array 元素内的字段）。
        /// </summary>
        private FieldInfo GetFieldFromProperty(SerializedProperty property)
        {
            System.Type type = target.GetType();
            string[] pathParts = property.propertyPath.Split('.');

            FieldInfo field = null;
            for (int i = 0; i < pathParts.Length; i++)
            {
                string part = pathParts[i];

                // 跳过 Array 路径段（如 "Array" 和 "data[0]"）
                if (part == "Array") continue;
                if (part.StartsWith("data[")) continue;

                field = type?.GetField(part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    type = field.FieldType;

                    // 解包数组/泛型集合的元素类型
                    if (type.IsArray)
                    {
                        type = type.GetElementType();
                    }
                    else if (type.IsGenericType)
                    {
                        type = type.GetGenericArguments()[0];
                    }
                }
                else
                {
                    break;
                }
            }

            return field;
        }
    }
}
