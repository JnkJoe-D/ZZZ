using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 路由窗口抽象基类。
    /// </summary>
    [Serializable]
    public abstract class RouteWindow
    {
        public string Tag;

        /// <summary>
        /// 编辑器及下拉列表中统一显示的标签格式："[RouteWindow的具体子类名称]routewindow.tag"
        /// </summary>
        public virtual string EditorLabel => $"[{GetType().Name}] {Tag}";

        public bool IsValid => !string.IsNullOrEmpty(Tag);

        public abstract RouteWindow Clone();

        public bool Matches(RouteWindow other)
        {
            if (other == null) return false;
            return GetType() == other.GetType() && string.Equals(Tag, other.Tag, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is not RouteWindow other) return false;
            return Matches(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GetType().GetHashCode() * 397) ^ (Tag != null ? StringComparer.Ordinal.GetHashCode(Tag) : 0);
            }
        }
    }

    [Serializable]
    public sealed class BufferRouteWindow : RouteWindow
    {
        public override RouteWindow Clone() => new BufferRouteWindow { Tag = Tag };
    }

    [Serializable]
    public sealed class ExecuteRouteWindow : RouteWindow
    {
        public override RouteWindow Clone() => new ExecuteRouteWindow { Tag = Tag };
    }

    [Serializable]
    public sealed class AutoRouteWindow : RouteWindow
    {
        public override RouteWindow Clone() => new AutoRouteWindow { Tag = Tag };
    }
}

