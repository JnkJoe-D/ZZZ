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
    
        public void UpdateView(HotUpdateModel model)
        {

            /*
            // -------------------------------------------------------------
            // 【正式代码示例】通过配置表加载静态文本与图片背景 (假设拥有 TbUIConfig)
            // -------------------------------------------------------------
            // 1. 获取配表数据
            // var uiConfig = ConfigManager.Instance.Tables.TbUIConfig.Get(1001); 
            // 
            // 2. 加载 Sprite 图集并赋值给 Background
            // var bgHandle = ResourceManager.Instance.LoadAssetAsync<Sprite>(uiConfig.BgPath, sprite => {
            //      Background.sprite = sprite;
            // });
            //
            // 3. 赋值多语言 / 硬编码文字
            // HeaderText.text = uiConfig.TitleText;
            */

            // -------------------------------------------------------------
            // 本地测试：直接操作 View 组件中绑定的对象
            // 由于上面未开启正式加载代码，背景图片将直接使用预制体默认指定的引用
            // -------------------------------------------------------------
            
            if (ProgressImage != null)
            {
                ProgressImage.fillAmount = model.DownloadProgress;
            }

            if (ProcessText != null && !string.IsNullOrEmpty(model.StatusText))
            {
                ProcessText.text = model.StatusText;
            }

            if (VersionText != null)
            {
                VersionText.text = model.VersionText;
            }

            if (SpeedText != null)
            {
                SpeedText.text = model.SpeedText;
            }

            if (HeaderText != null)
            {
                HeaderText.text = "检测更新"; // 硬编码测试
            }
        }
}
}
