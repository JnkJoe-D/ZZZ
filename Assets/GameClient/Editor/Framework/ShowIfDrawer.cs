using System.Reflection;
using Game.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// ShowIf 属性的绘制器。
    /// 注意：如果父对象有自定义 Drawer（如 ActionRouteDrawer），该 Drawer 可能不会自动生效，需要手动调用检测逻辑。
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public sealed class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (CheckVisible(property, attribute as ShowIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CheckVisible(property, attribute as ShowIfAttribute) 
                ? EditorGUI.GetPropertyHeight(property, label, true) 
                : 0f;
        }

        public static bool CheckVisible(SerializedProperty property, ShowIfAttribute attr)
        {
            if (attr == null) return true;

            string path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');
            string parentPath = lastDot > 0 ? path.Substring(0, lastDot) : string.Empty;
            
            SerializedProperty comparisonProp = string.IsNullOrEmpty(parentPath) 
                ? property.serializedObject.FindProperty(attr.ComparisonField)
                : property.serializedObject.FindProperty($"{parentPath}.{attr.ComparisonField}");

            if (comparisonProp == null)
            {
                return true;
            }

            object currentValue = GetPropertyValue(comparisonProp);
            object targetValue = attr.ComparisonValue;

            // Handle Enum comparison (SerializedProperty.intValue vs Enum object)
            if (comparisonProp.propertyType == SerializedPropertyType.Enum && targetValue != null && targetValue.GetType().IsEnum)
            {
                targetValue = (int)targetValue;
            }

            return Equals(currentValue, targetValue);
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Enum: return property.intValue;
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.Float: return property.floatValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
                default: return null;
            }
        }
    }
}
