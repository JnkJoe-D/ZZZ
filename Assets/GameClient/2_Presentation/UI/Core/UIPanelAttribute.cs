using System;

namespace Game.UI
{
    /// <summary>
    /// 面板层级定义
    /// 数值越大，渲染越靠前（SortingOrder 越大）
    /// 同层内的多个面板通过自增偏移量自动排列
    /// </summary>
    public enum UILayer
    {
        Background = 0,    // 背景层（场景 UI、大厅背景）
        Main       = 1000, // 主界面层（主城 HUD）
        Window     = 2000, // 普通窗口层（背包、技能面板）
        Dialog     = 3000, // 弹窗层（确认框、奖励弹窗）
        Guide      = 4000, // 引导层（新手引导蒙版）
        Toast      = 5000, // 提示层（飘字、Tips）
        Loading    = 6000, // 加载层（Loading 转场遮罩）
        System     = 7000, // 系统层（系统弹窗）
        Top        = 8000, // 最顶层（GM 工具、Debug 信息）
    }

    /// <summary>
    /// 标注在 UIModule 子类上，声明该模块对应的主 View 预制体路径和层级
    /// 
    /// 用法：
    ///   [UIPanel(ViewPrefab = "Assets/UI/Panels/LoginPanel.prefab", Layer = UILayer.Window)]
    ///   public class LoginModule : UIModule&lt;LoginView, LoginModel&gt; { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class UIPanelAttribute : Attribute
    {
        /// <summary>主 Panel View 的预制体路径（由 ResourceManager 加载）</summary>
        public string ViewPrefab { get; set; }

        /// <summary>面板所在的层级</summary>
        public UILayer Layer { get; set; } = UILayer.Window;

        /// <summary>是否为全屏面板（打开时优化隐藏下层）</summary>
        public bool IsFullScreen { get; set; } = false;
    }
}
