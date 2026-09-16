using Game.Framework;

namespace Game.UI
{
    // ============================================================
    // UI 框架相关事件
    // ============================================================

    /// <summary>
    /// UI 面板打开事件
    /// </summary>
    public struct UIPanelOpenedEvent : IGameEvent
    {
        public string ModuleName;
        public UILayer Layer;
    }

    /// <summary>
    /// UI 面板关闭事件
    /// </summary>
    public struct UIPanelClosedEvent : IGameEvent
    {
        public string ModuleName;
        public UILayer Layer;
    }

    /// <summary>
    /// UI 导航栈回退事件
    /// </summary>
    public struct UINavigateBackEvent : IGameEvent
    {
        public string FromModule;
        public string ToModule;
    }
}
