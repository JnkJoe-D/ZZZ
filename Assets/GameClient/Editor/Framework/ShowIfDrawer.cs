using System.Collections.Generic;
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
        private static readonly Dictionary<(string, string), string> _comparisonPathCache = new();

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

            string propPath = property.propertyPath;
            var key = (propPath, attr.ComparisonField);

            if (!_comparisonPathCache.TryGetValue(key, out string comparisonPath))
            {
                int lastDot = propPath.LastIndexOf('.');
                string parentPath = lastDot > 0 ? propPath.Substring(0, lastDot) : string.Empty;
                comparisonPath = string.IsNullOrEmpty(parentPath)
                    ? attr.ComparisonField
                    : $"{parentPath}.{attr.ComparisonField}";
                _comparisonPathCache[key] = comparisonPath;
            }

            SerializedProperty comparisonProp = property.serializedObject.FindProperty(comparisonPath);
            if (comparisonProp == null)
            {
                return true;
            }

            object currentValue = GetPropertyValue(comparisonProp);

            if (attr.ComparisonValues != null && attr.ComparisonValues.Length > 0)
            {
                foreach (var val in attr.ComparisonValues)
                {
                    object targetVal = val;
                    if (comparisonProp.propertyType == SerializedPropertyType.Enum && targetVal != null && targetVal.GetType().IsEnum)
                    {
                        targetVal = (int)targetVal;
                    }

                    if (Equals(currentValue, targetVal)) return true;
                }
                return false;
            }

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
