using System;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 将 RouteWindow 或 string 字段标记为连招派生窗口标签下拉选择框。
    /// 支持通过 allowedTypes 参数限定允许选取的窗口子类类型（如仅限 Buffer/Execute，或仅限 Auto）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ComboWindowTagAttribute : PropertyAttribute
    {
        public Type[] AllowedTypes { get; }

        public ComboWindowTagAttribute(params Type[] allowedTypes)
        {
            AllowedTypes = allowedTypes;
        }
    }
}

