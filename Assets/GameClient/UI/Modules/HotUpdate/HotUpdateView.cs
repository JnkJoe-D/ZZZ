using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class HotUpdateView : UIView
    {
        // 自动生成的UI组件字段
        public Image Background { get; private set; }
        public TMP_Text HeaderText { get; private set; } //游戏名
        public TMP_Text VersionText { get; private set; }
        public TMP_Text ProcessText { get; private set; } //正在检查更新...
        public TMP_Text SpeedText { get; private set; }
        public Image ProgressImage { get; private set; } //进度显示 filled

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
        }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            Background = transform.Find("View/Background").GetComponent<Image>();
            
            var headerTrans = transform.Find("View/Content/Header/HeaderText");
            if (headerTrans != null) HeaderText = headerTrans.GetComponent<TMP_Text>();
            
            var footerTrans = transform.Find("View/Content/Footer/VersionText");
            if (footerTrans != null) VersionText = footerTrans.GetComponent<TMP_Text>();
            
            var processTrans = transform.Find("View/Content/Progress/ProcessStaus/ProcessText");
            if (processTrans != null) ProcessText = processTrans.GetComponent<TMP_Text>();
            
            var speedTrans = transform.Find("View/Content/Progress/ProcessStaus/SpeedText");
            if (speedTrans != null) SpeedText = speedTrans.GetComponent<TMP_Text>();
            
            var progTrans = transform.Find("View/Content/Progress/ProgressImage");
            if (progTrans != null) ProgressImage = progTrans.GetComponent<Image>();
        }
    }
}
