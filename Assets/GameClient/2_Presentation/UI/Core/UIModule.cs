using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 模块基类（Controller 角色）
    ///
    /// 职责：
    ///   1. 管理对应面板 View 的显示/隐藏/销毁
    ///   2. 持有 Model 数据并响应业务事件更新数据
    ///   3. 将数据变更推送给 View 刷新
    ///   4. 按需加载和管理 Widget 预制体
    ///
    /// Module 本身是纯 C# 对象，不是 MonoBehaviour。
    /// 它的主 View 预制体路径通过 [UIPanel] Attribute 声明。
    /// </summary>
    public abstract class UIModuleBase
    {
        /// <summary>模块是否处于显示状态</summary>
        public bool IsVisible { get; private set; }

        /// <summary>关联的 UIView 基类引用（由框架注入）</summary>
        internal UIView View { get; private set; }

        // ────────────────────────────────────────
        // 框架内部调用（由 UIManager 调用）
        // ────────────────────────────────────────

        internal void Internal_Create(UIView view)
        {
            View = view;
            view.OnInit();
            OnCreate();
        }

        internal void Internal_Show(object data)
        {
            IsVisible = true;
            View?.SetVisible(true);
            OnShow(data);
        }

        internal void Internal_Hide()
        {
            IsVisible = false;
            OnHide();
            View?.SetVisible(false);
        }

        internal void Internal_Destroy()
        {
            OnRemove();
            if (View != null)
            {
                View.OnRemove();
                Object.Destroy(View.gameObject);
                View = null;
            }
            IsVisible = false;
        }

        // ────────────────────────────────────────
        // 子类生命周期（由具体 Module 实现）
        // ────────────────────────────────────────

        /// <summary>
        /// 模块创建时调用一次。View 已经加载完毕，可以在此：
        /// - 绑定按钮回调
        /// - 加载 Widget 预制体
        /// - 订阅 EventCenter 事件
        /// - 将 Model 的 OnChanged 绑定到 View 刷新
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// 每次面板显示时调用（含首次和重复打开）
        /// </summary>
        /// <param name="data">外部传入的上下文数据，可为 null</param>
        protected virtual void OnShow(object data) { }

        /// <summary>
        /// 面板被隐藏时调用
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// 面板被彻底销毁前调用
        /// 在此取消 EventCenter 订阅、释放资源引用等
        /// </summary>
        protected virtual void OnRemove() { }
    }

    /// <summary>
    /// 带有泛型约束的 UIModule
    /// 强类型绑定 View 和 Model，省去每次手动类型转换
    /// 
    /// 用法：
    ///   [UIPanel(ViewPrefab = "Assets/UI/Panels/BagPanel.prefab", Layer = UILayer.Window)]
    ///   public class BagModule : UIModule&lt;BagView, BagModel&gt;
    ///   {
    ///       protected override void OnCreate()
    ///       {
    ///           View.btnClose.onClick.AddListener(() =&gt; UIManager.Instance.Close(this));
    ///           Model.OnChanged += View.Refresh;
    ///       }
    ///   }
    /// </summary>
    public abstract class UIModule<TView, TModel> : UIModuleBase
        where TView : UIView
        where TModel : UIModel, new()
    {
        /// <summary>强类型的 View 引用</summary>
        protected new TView View => base.View as TView;

        /// <summary>强类型的 Model 引用（自动创建）</summary>
        protected TModel Model { get; private set; }

        protected UIModule()
        {
            Model = new TModel();
        }

        protected override void OnRemove()
        {
            Model?.ClearListeners();
            Model?.Reset();
            Model = null;
        }
    }
}
