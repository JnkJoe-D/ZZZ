using TMPro;
using UnityEngine.UI;

namespace Game.UI 
{
    public class LoadingView : UIView
    {
        // 自动生成的UI组件字段
        public Image Background { get; private set; }
        public TMP_Text LoadingText { get; private set; }
        public Image LoadingImage { get; private set; }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            Background = transform.Find("View/Background").GetComponent<Image>();
            LoadingText = transform.Find("View/Content/Loading/LoadingText").GetComponent<TMP_Text>();
            LoadingImage = transform.Find("View/Content/Loading/LoadingImage").GetComponent<Image>();
        }



        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
        }
    }
}
