using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    /// <summary>
    /// 通用提示弹窗视图组件 (需挂载在预制体根节点)
    /// </summary>
    public class MessageBoxView : UIView
    {
        // 自动生成的UI组件字段
        public TMP_Text TitleText{get;private set;}
        public TMP_Text ContentText { get; private set; }
        public Button ConfirmBtn { get; private set; }
        public TMP_Text ConfirmText { get; private set; }
        public Button CancelBtn { get; private set; }
        public TMP_Text CancelText { get; private set; }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            TitleText = transform.Find("View/Header/TitleText").GetComponent<TMP_Text>();
            ContentText = transform.Find("View/Content/ContentText").GetComponent<TMP_Text>();
            ConfirmBtn = transform.Find("View/Footer/ConfirmBtn").GetComponent<Button>();
            ConfirmText = transform.Find("View/Footer/ConfirmBtn/ConfirmText").GetComponent<TMP_Text>();
            CancelBtn = transform.Find("View/Footer/CancelBtn").GetComponent<Button>();
            CancelText = transform.Find("View/Footer/CancelBtn/CancelText").GetComponent<TMP_Text>();
        }
        public override void OnInit()
        {
            BindUIComponents();
        }

        public void UpdateView(MessageBoxModel model)
        {
            string defaultTitle = "系统提示";
            string defaultConfirm = "确定";
            string defaultCancel = "取消";

            if (TitleText != null) 
                TitleText.text = model.Title ?? defaultTitle;

            if (ContentText != null) 
                ContentText.text = model.Content;
            
            // 确认按钮文本
            if (ConfirmText != null)
            {
                ConfirmText.text = model.ConfirmText ?? defaultConfirm;
            }

            // 处理单按钮/双按钮模式
            if (model.IsSingleButton)
            {
                if (CancelBtn != null) CancelBtn.gameObject.SetActive(false);
            }
            else
            {
                if (CancelBtn != null) CancelBtn.gameObject.SetActive(true);
                if (CancelText != null)
                {
                    CancelText.text = model.CancelText ?? defaultCancel;
                }
            }
        }
    }
}
