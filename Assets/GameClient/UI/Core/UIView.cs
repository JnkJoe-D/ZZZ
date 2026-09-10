using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 面板 View 基类（挂载在面板的根 Prefab 上）
    /// 
    /// 职责：
    ///   1. 持有面板的 Canvas 引用，由框架自动设置 SortingOrder
    ///   2. 提供生命周期虚方法供子类实现
    ///   3. 提供刘海屏适配（SafeArea）
    /// 
    /// 注意：View 绝不持有业务逻辑，按钮点击等交互通过委托回调给 Module
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class UIView : MonoBehaviour
    {
        // ── 由框架自动获取 ────────────────────
        private Canvas _canvas;

        public Canvas Canvas
        {
            get
            {
                if (_canvas == null)
                    _canvas = GetComponent<Canvas>();
                return _canvas;
            }
        }
        [SerializeField]
        private GameObject _view;

        public GameObject View
        {
            get
            {
                if (_view == null)
                {
                    var viewTrans = transform.Find("View");
                    _view = viewTrans != null ? viewTrans.gameObject : gameObject;
                }
                return _view;
            }
        }

        /// <summary>Canvas 的渲染排序</summary>
        public int SortingOrder
        {
            get => Canvas != null ? Canvas.sortingOrder : 0;
            set
            {
                if (Canvas != null)
                {
                    Canvas.overrideSorting = true;
                    Canvas.sortingOrder = value;
                }
            }
        }

        // ────────────────────────────────────────
        // 生命周期（由 UIManager / Module 调用）
        // ────────────────────────────────────────

        /// <summary>
        /// Prefab 加载并实例化后调用一次
        /// 子类在此初始化组件引用、注册按钮回调等
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 面板每次显示时调用（包括首次和重复打开）
        /// </summary>
        public virtual void OnShow() { }

        /// <summary>
        /// 面板被隐藏时调用（不销毁，仅 SetActive(false)）
        /// </summary>
        public virtual void OnHide() { }

        /// <summary>
        /// 面板被销毁前调用
        /// 子类在此进行清理工作（取消订阅等）
        /// </summary>
        public virtual void OnRemove() { }

        // ────────────────────────────────────────
        // 显示/隐藏
        // ────────────────────────────────────────

        public void SetVisible(bool visible)
        {
            View.SetActive(visible);

            if (visible)
                OnShow();
            else
                OnHide();
        }
    }
}
