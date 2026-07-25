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
                
                RefreshView();
            }
            else
            {
                Debug.LogError("[MessageBox] 必须传入 MessageBoxModel！");
                CloseParams();
            }
        }

        private void RefreshView()
        {
            if (View == null) return;

            /*
            // -------------------------------------------------------------
            // 【正式代码示例】通过配置表加载静态文本
            // -------------------------------------------------------------
            // var uiConfig = ConfigManager.Instance.Tables.TbUIConfig.Get(2001); // 提示框配置
            // string defaultTitle = uiConfig.DefaultTitle; // "系统提示"
            // string defaultConfirm = uiConfig.DefaultConfirm; // "确定"
            // string defaultCancel = uiConfig.DefaultCancel; // "取消"
            */

            string defaultTitle = "系统提示";
            string defaultConfirm = "确定";
            string defaultCancel = "取消";

            if (View.TitleText != null) 
                View.TitleText.text = Model.Title ?? defaultTitle;

            if (View.ContentText != null) 
                View.ContentText.text = Model.Content;
            
            // 确认按钮文本
            if (View.ConfirmText != null)
            {
                View.ConfirmText.text = Model.ConfirmText ?? defaultConfirm;
            }

            // 处理单按钮/双按钮模式
            if (Model.IsSingleButton)
            {
                if (View.CancelBtn != null) View.CancelBtn.gameObject.SetActive(false);
                // 通常单按钮模式下，确认按钮会居中，这可以交给 UI 预制体的 Layout 组件处理
            }
            else
            {
                if (View.CancelBtn != null) View.CancelBtn.gameObject.SetActive(true);
                if (View.CancelText != null)
                {
                    View.CancelText.text = Model.CancelText ?? defaultCancel;
                }
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
