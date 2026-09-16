using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 将 string 字段标记为连招派生窗口标签下拉选择框。
    /// 数据源从 ActionTagConfig 资产中读取已配置的 availableComboWindowTags。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ComboWindowTagAttribute : PropertyAttribute
    {
    }
}
