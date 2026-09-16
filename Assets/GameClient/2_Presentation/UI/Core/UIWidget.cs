using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 可复用 UI 组件基类
    /// 
    /// 与 UIView 的区别：
    ///   - Widget 不走 UIManager 管理流程
    ///   - Widget 没有独立的 Module / Model
    ///   - Widget 的加载和数据注入由父 Module 负责
    ///   - Widget 的交互回调通过委托传递给父 Module
    /// 
    /// 用法：
    ///   public class ItemWidget : UIWidget
    ///   {
    ///       [SerializeField] private Image _icon;
    ///       [SerializeField] private Text  _name;
    ///
    ///       public System.Action&lt;int&gt; OnClick;
    ///
    ///       public void SetData(ItemWidgetData data) { ... }
    ///   }
    /// </summary>
    public abstract class UIWidget : MonoBehaviour
    {
        /// <summary>
        /// Widget 初始化（由父 Module 在 Instantiate 后调用）
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// Widget 被回收或销毁前调用
        /// </summary>
        public virtual void OnRecycle() { }
    }
}
