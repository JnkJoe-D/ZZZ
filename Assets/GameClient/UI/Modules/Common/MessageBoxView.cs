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

    }
}
