using UnityEngine.UI;
using Game.UI;

namespace Game.UI.Modules.RoleStatus
{
    public class StatusPanelView : UIView
    {
        // 自动生成的UI组件字段
        public Image HpBg { get; private set; }
        public Image HpDelay { get; private set; }
        public Image HpFill { get; private set; }
        public Image EnergyBg { get; private set; }
        public Image EnergyFill { get; private set; }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            HpBg = transform.Find("View/Content/HpBg").GetComponent<Image>();
            HpDelay = transform.Find("View/Content/HpDelay").GetComponent<Image>();
            HpFill = transform.Find("View/Content/HpFill").GetComponent<Image>();
            EnergyBg = transform.Find("View/Content/EnergyBg").GetComponent<Image>();
            EnergyFill = transform.Find("View/Content/EnergyFill").GetComponent<Image>();
        }


        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
        }
    }
}
