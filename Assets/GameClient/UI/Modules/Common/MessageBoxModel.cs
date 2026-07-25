using System;
using Game.Framework;

namespace Game.UI
{
    /// <summary>
    /// 通用提示弹窗视图数据
    /// </summary>
    public class MessageBoxModel : UIModel
    {
        public string Title   { get; set; }
        public string Content { get; set; }
        public string ConfirmText { get; set; }
        public string CancelText  { get; set; }
        
        public Action OnConfirm { get; set; }
        public Action OnCancel  { get; set; }
        
        // 是否仅显示确认按钮（单按钮模式）
        public bool IsSingleButton => string.IsNullOrEmpty(CancelText) && OnCancel == null;
    }
}
