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
    /// 用于定义片段的元数据（显示名称等）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ClipDefinitionAttribute : Attribute
    {
        public string DisplayName { get; private set; }
        public Type[] TargetTrackTypes { get; private set; }

        public ClipDefinitionAttribute(Type targetTrackType, string displayName)
        {
            TargetTrackTypes = new Type[] { targetTrackType };
            DisplayName = displayName;
        }

        public ClipDefinitionAttribute(Type[] targetTrackTypes, string displayName)
        {
            TargetTrackTypes = targetTrackTypes;
            DisplayName = displayName;
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
        public string ColorHex { get; private set; }

        public TrackDefinitionAttribute(string displayName, string colorHex, string icon = "", int order = 0)
        {
            DisplayName = displayName;
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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ShowIfAttribute : Attribute
    {
        public string conditionalSourceField;
        public object expectedValue;
        public bool isEqual;
        public ShowIfAttribute(string conditionalSourceField, object expectedValue,bool isEqual = true)
        {
            this.conditionalSourceField = conditionalSourceField;
            this.expectedValue = expectedValue;
            this.isEqual = isEqual;
        }
    }
}
