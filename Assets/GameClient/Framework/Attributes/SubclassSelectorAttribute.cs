using System;

namespace UnityEngine
{
    /// <summary>
    /// 用于 SerializeReference 的子类选择器属性。
    /// 加上此属性后，Inspector 会显示一个下拉框，列出所有可用的子类实现。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SubclassSelectorAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 用于多态子类的友好显示名称标签。
    /// 可以定义子类在下拉框和折叠头部显示的友好中文名称。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SubclassDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public SubclassDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
