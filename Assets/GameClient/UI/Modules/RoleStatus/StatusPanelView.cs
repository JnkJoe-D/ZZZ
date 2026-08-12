using UnityEngine;
using UnityEngine.UI;
using Game.UI;

namespace Game.UI.Modules.RoleStatus
{
    public class StatusPanelView : UIView
    {
        public StatusPanelModel Model { get; set; }

        // 自动生成的UI组件字段
        public Image Content {get;private set;}
        public Image Role01 {get;private set;}
        public Image HpBg01 {get;private set;}
        public Image HpFill01 {get;private set;}
        public Image SPBg01 {get;private set;}
        public Image SPFill01 {get;private set;}
        public Image SPPointGray01 {get;private set;}
        public Image SPPoint01 {get;private set;}
        public Image Role02 {get;private set;}
        public Image HpBg02 {get;private set;}
        public Image HpFill02 {get;private set;}
        public Image SPBg02 {get;private set;}
        public Image SPFill02 {get;private set;}
        public Image SPPointGray02 {get;private set;}
        public Image SPPoint02 {get;private set;}
        public Image Role03 {get;private set;}
        public Image HpBg03 {get;private set;}
        public Image HpFill03 {get;private set;}
        public Image SPBg03 {get;private set;}
        public Image SPFill03 {get;private set;}
        public Image SPPointGray03 {get;private set;}
        public Image SPPoint03 {get;private set;}

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            Content = transform.Find("View/Content").GetComponent<Image>();
            Role01 = transform.Find("View/Content/Role01/Role01").GetComponent<Image>();
            HpBg01 = transform.Find("View/Content/Role01/HpBg01").GetComponent<Image>();
            HpFill01 = transform.Find("View/Content/Role01/HpFill01").GetComponent<Image>();
            SPBg01 = transform.Find("View/Content/Role01/SPBg01").GetComponent<Image>();
            SPFill01 = transform.Find("View/Content/Role01/SPFill01").GetComponent<Image>();
            SPPointGray01 = transform.Find("View/Content/Role01/SPFill01/SPPointGray01").GetComponent<Image>();
            SPPoint01 = transform.Find("View/Content/Role01/SPFill01/SPPoint01").GetComponent<Image>();
            Role02 = transform.Find("View/Content/Role02/Role02").GetComponent<Image>();
            HpBg02 = transform.Find("View/Content/Role02/HpBg02").GetComponent<Image>();
            HpFill02 = transform.Find("View/Content/Role02/HpFill02").GetComponent<Image>();
            SPBg02 = transform.Find("View/Content/Role02/SPBg02").GetComponent<Image>();
            SPFill02 = transform.Find("View/Content/Role02/SPFill02").GetComponent<Image>();
            SPPointGray02 = transform.Find("View/Content/Role02/SPFill02/SPPointGray02").GetComponent<Image>();
            SPPoint02 = transform.Find("View/Content/Role02/SPFill02/SPPoint02").GetComponent<Image>();
            Role03 = transform.Find("View/Content/Role03/Role03").GetComponent<Image>();
            HpBg03 = transform.Find("View/Content/Role03/HpBg03").GetComponent<Image>();
            HpFill03 = transform.Find("View/Content/Role03/HpFill03").GetComponent<Image>();
            SPBg03 = transform.Find("View/Content/Role03/SPBg03").GetComponent<Image>();
            SPFill03 = transform.Find("View/Content/Role03/SPFill03").GetComponent<Image>();
            SPPointGray03 = transform.Find("View/Content/Role03/SPFill03/SPPointGray03").GetComponent<Image>();
            SPPoint03 = transform.Find("View/Content/Role03/SPFill03/SPPoint03").GetComponent<Image>();
        }

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            // 实例化独立的材质，防止修改 _FillAmount 污染整个资源库，也避免多角色同屏时干扰
            CloneMaterial(HpFill01);
            CloneMaterial(HpFill02);
            CloneMaterial(HpFill03);
            
            CloneMaterial(SPFill01);
            CloneMaterial(SPFill02);
            CloneMaterial(SPFill03);
        }

        private void CloneMaterial(Image img)
        {
            if (img != null && img.material != null)
                img.material = new Material(img.material);
        }

        public void UpdateHp(int slotIndex)
        {
            if (Model == null) return;
            float percent = Model.RoleData[slotIndex].HpPercent;

            Image hpFill = GetHpFill(slotIndex);
            if (hpFill == null || hpFill.material == null) return;

            // 主血条瞬间到位
            hpFill.material.SetFloat("_FillAmount", percent);
        }

        public void UpdateEnergy(int slotIndex)
        {
            if (Model == null) return;
            float percent = Model.RoleData[slotIndex].EnergyPercent;
            float thresholdPercent = Model.RoleData[slotIndex].EnergyThresholdPercent;

            Image spFill = GetSPFill(slotIndex);
            Image spPoint = GetSPPoint(slotIndex);
            Image spPointGray = GetSPPointGray(slotIndex);

            if (spFill == null || spFill.material == null) return;

            spFill.material.SetFloat("_FillAmount", percent);
            
            // 根据能量是否满足消耗，控制 Shader 的闪烁和变灰状态
            if (thresholdPercent > 0)
            {
                spFill.material.SetFloat("_FlashActive", percent >= thresholdPercent ? 1f : 0f);
            }
            else
            {
                spFill.material.SetFloat("_FlashActive", percent >= 1f ? 1f : 0f);
            }

            // 更新能量条锚点 (SPPoint/SPPointGray) 的位置与显示状态
            if (thresholdPercent > 0)
            {
                float width = spFill.rectTransform.rect.width;
                float targetX = width * thresholdPercent;

                if (spPoint != null)
                {
                    Vector2 pos = spPoint.rectTransform.anchoredPosition;
                    pos.x = targetX;
                    spPoint.rectTransform.anchoredPosition = pos;
                    spPoint.gameObject.SetActive(percent >= thresholdPercent);
                }

                if (spPointGray != null)
                {
                    Vector2 pos = spPointGray.rectTransform.anchoredPosition;
                    pos.x = targetX;
                    spPointGray.rectTransform.anchoredPosition = pos;
                    spPointGray.gameObject.SetActive(percent < thresholdPercent);
                }
            }
            else
            {
                // 如果没有配置消耗量，则隐藏阈值标记
                if (spPoint != null) spPoint.gameObject.SetActive(false);
                if (spPointGray != null) spPointGray.gameObject.SetActive(false);
            }
        }

        private Image GetHpFill(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return HpFill01;
                case 1: return HpFill02;
                case 2: return HpFill03;
                default: return null;
            }
        }

        private Image GetSPFill(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return SPFill01;
                case 1: return SPFill02;
                case 2: return SPFill03;
                default: return null;
            }
        }

        private Image GetSPPoint(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return SPPoint01;
                case 1: return SPPoint02;
                case 2: return SPPoint03;
                default: return null;
            }
        }

        private Image GetSPPointGray(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return SPPointGray01;
                case 1: return SPPointGray02;
                case 2: return SPPointGray03;
                default: return null;
            }
        }

        public Transform GetRoleAnchor(int slotIndex)
        {
            switch (slotIndex)
            {
                // 这里暂时用Role图标节点作为锚点
                case 0: return Role01 != null ? Role01.transform : null;
                case 1: return Role02 != null ? Role02.transform : null;
                case 2: return Role03 != null ? Role03.transform : null;
                default: return null;
            }
        }

        public void UpdateRoleIcon(int viewSlotIndex)
        {
            if (Model == null) return;
            Sprite icon = Model.RoleData[viewSlotIndex].RoleIcon;

            Image roleImg = GetRoleImage(viewSlotIndex);
            if (roleImg != null && icon != null)
            {
                roleImg.sprite = icon;
            }
        }

        private Image GetRoleImage(int viewSlotIndex)
        {
            switch (viewSlotIndex)
            {
                case 0: return Role01;
                case 1: return Role02;
                case 2: return Role03;
                default: return null;
            }
        }
    }
}
