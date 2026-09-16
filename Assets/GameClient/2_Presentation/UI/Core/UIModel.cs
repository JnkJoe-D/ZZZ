using System;

namespace Game.UI
{
    /// <summary>
    /// UI 数据模型基类
    /// 
    /// 职责：
    ///   1. 作为纯 C# 数据容器，持有面板所需的业务数据
    ///   2. 当数据变更时触发 OnChanged 事件，通知 View 刷新
    /// 
    /// 用法：
    ///   public class LoginModel : UIModel
    ///   {
    ///       private string _account;
    ///       public string Account
    ///       {
    ///           get => _account;
    ///           set { _account = value; NotifyChanged(); }
    ///       }
    ///   }
    /// </summary>
    public abstract class UIModel
    {
        /// <summary>
        /// 数据变更事件，View 订阅此事件来刷新自身
        /// </summary>
        public event Action OnChanged;

        /// <summary>
        /// 子类在 setter 中调用此方法通知数据变更
        /// </summary>
        protected void NotifyChanged()
        {
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 重置数据到初始状态（子类覆写以实现具体清理逻辑）
        /// </summary>
        public virtual void Reset() { }

        /// <summary>
        /// 清除所有事件订阅（面板关闭时由 Module 调用）
        /// </summary>
        public void ClearListeners()
        {
            OnChanged = null;
        }
    }
}
