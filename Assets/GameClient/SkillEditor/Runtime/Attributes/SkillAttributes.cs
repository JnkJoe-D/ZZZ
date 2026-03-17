using System;

namespace SkillEditor
{
    /// <summary>
    /// 用于定义在 SkillEditor Inspector 中显示的名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkillPropertyAttribute : Attribute
    {
        public string Name { get; private set; }

        public SkillPropertyAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 用于定义轨道的元数据（显示名称、菜单路径、图标等）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TrackDefinitionAttribute : Attribute
    {
        public string DisplayName { get; private set; }
        public string Icon { get; private set; }
        public int Order { get; private set; }
        public Type ClipType { get; private set; }
        public string ColorHex { get; private set; }

        /// <summary>
        /// 定义轨道元数据
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="menuPath">菜单路径</param>
        /// <param name="clipType">关联的片段类型</param>
        /// <param name="colorHex">轨道颜色 (Hex #RRGGBB)</param>
        /// <param name="icon">图标名称</param>
        /// <param name="order">菜单排序</param>
        public TrackDefinitionAttribute(string displayName, Type clipType, string colorHex, string icon = "", int order = 0)
        {
            DisplayName = displayName;
            ClipType = clipType;
            ColorHex = colorHex;
            Icon = icon;
            Order = order;
        }
    }

    /// <summary>
    /// 用于自动同步资源引用的元数据（GUID, Name, Path）
    /// 绑定到对应的资源字段名称。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SkillAssetReferenceAttribute : Attribute
    {
        public string TargetFieldName { get; private set; }

        public SkillAssetReferenceAttribute(string targetFieldName)
        {
            TargetFieldName = targetFieldName;
        }
    }
}
