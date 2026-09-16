using Game.Framework;

namespace Game.Framework
{


    // ============================================================
    // 场景流转相关事件
    // ============================================================

    /// <summary>
    /// 场景切换开始事件
    /// UI 系统监听此事件以弹起 Loading 界面并准备显示进度
    /// </summary>
    public struct SceneChangeBeginEvent : IGameEvent
    {
        public SceneTransitionParams TransitionParams;
    }

    /// <summary>
    /// 场景加载进度事件
    /// 每帧广播加载进度（0~1），驱动 Loading 进度条动画
    /// </summary>
    public struct SceneLoadProgressEvent : IGameEvent
    {
        public float Progress;
        public string LoadingText;
    }

    /// <summary>
    /// 场景切换完成事件
    /// UI 系统监听此事件以关闭 Loading 界面，业务逻辑层启动新场景逻辑
    /// </summary>
    public struct SceneChangeEndEvent : IGameEvent
    {
        public string SceneName;
        public bool   Success;
    }
}
