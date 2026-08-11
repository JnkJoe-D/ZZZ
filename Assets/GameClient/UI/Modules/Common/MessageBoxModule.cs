using System;
using UnityEngine;
using Game.Framework;

namespace Game.UI
{
    /// <summary>
    /// 通用提示弹窗模块
    /// 作为全局最高层级的弹窗，用于需要玩家强确认的场景
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/Common/MessageBoxPanel.prefab", Layer = UILayer.System)]
    public class MessageBoxModule : UIModule<MessageBoxView, MessageBoxModel>
    {
        protected override void OnCreate()
        {
            if (View == null) return;

            if (View.ConfirmBtn != null)
            {
                View.ConfirmBtn.onClick.AddListener(OnConfirmClicked);
            }
            
            // 这里绑定时要注意，单按钮模式下 CancelBtn 可能是隐藏或不存在的
            if (View.CancelBtn != null)
            {
                View.CancelBtn.onClick.AddListener(OnCancelClicked);
            }
        }

        protected override void OnShow(object data)
        {
            if (data is MessageBoxModel model)
            {
                Model.Title       = model.Title;
                Model.Content     = model.Content;
                Model.ConfirmText = model.ConfirmText;
                Model.CancelText  = model.CancelText;
                Model.OnConfirm   = model.OnConfirm;
                Model.OnCancel    = model.OnCancel;
                
                View?.UpdateView(Model);
            }
            else
            {
                Debug.LogError("[MessageBox] 必须传入 MessageBoxModel");
                CloseParams();
            }
        }

        private void OnConfirmClicked()
        {
            var callback = Model.OnConfirm;
            CloseParams(); // 先关闭自身
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = Model.OnCancel;
            CloseParams(); // 先关闭自身
            callback?.Invoke();
        }

        private void CloseParams()
        {
            UIManager.Instance.Close(this);
        }
    }
}
