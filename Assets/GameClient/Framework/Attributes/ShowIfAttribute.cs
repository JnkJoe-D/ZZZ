using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 根据字段值决定是否在 Inspector 中显示的属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string ComparisonField { get; }
        public object ComparisonValue { get; }
        public object[] ComparisonValues { get; }

        public ShowIfAttribute(string comparisonField, object comparisonValue)
        {
            ComparisonField = comparisonField;
            ComparisonValue = comparisonValue;
            ComparisonValues = null;
        }

        public ShowIfAttribute(string comparisonField, params object[] comparisonValues)
        {
            ComparisonField = comparisonField;
            ComparisonValue = null;
            ComparisonValues = comparisonValues;
        }
    }
}
