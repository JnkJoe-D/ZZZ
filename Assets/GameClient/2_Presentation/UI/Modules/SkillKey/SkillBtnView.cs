using Game.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 技能按键面板 View (MVC - View，挂载于 SkillBtnPanel.prefab 根节点)
    /// 
    /// 职责：
    ///   1. 纯粹的受控表现层，仅负责 5 个槽位背景、图标与按键提示文本的展示
    ///   2. 持有 Model 引用，在被触发时直接读取 Model 的只读属性渲染自身
    ///   3. 绝对禁止反向篡改核心业务状态
    /// </summary>
    public class SkillBtnView : UIView
    {
        // ── 自动绑定的 UI 组件字段 ──────────────────────────
        public Image SwitchBg { get; private set; }
        public TMP_Text SwitchKeyTMP { get; private set; }
        public Image SpecialBg { get; private set; }
        public TMP_Text SpeecialKeyTMP { get; private set; }
        public Image EvadeBg { get; private set; }
        public TMP_Text SpecialKeyTMP { get; private set; }
        public Image NormalBg { get; private set; }
        public TMP_Text NormalKeyTMP { get; private set; }
        public Image UltraBg { get; private set; }
        public TMP_Text UltraKeyTMP { get; private set; }
        public Image SkillSwitchBtn { get; private set; }
        public Image SkillSepcialBtn { get; private set; }
        public Image SkillEvadeBtn { get; private set; }
        public Image SkillNormalBtn { get; private set; }
        public Image SkillUltraBtn { get; private set; }

        // ── 数据模型引用 (MVC - Model) ──────────────────────
        public SkillKeyModel Model { get; private set; }

        private void BindUIComponents()
        {
            SwitchBg = transform.Find("View/Bg/Switch/Icon/SwitchBg")?.GetComponent<Image>();
            SwitchKeyTMP = transform.Find("View/Bg/Switch/KeyValue/SwitchKeyTMP")?.GetComponent<TMP_Text>();
            SpecialBg = transform.Find("View/Bg/Special/Icon/SpecialBg")?.GetComponent<Image>();
            SpeecialKeyTMP = transform.Find("View/Bg/Special/KeyValue/SpeecialKeyTMP")?.GetComponent<TMP_Text>();
            EvadeBg = transform.Find("View/Bg/Evade/Icon/EvadeBg")?.GetComponent<Image>();
            SpecialKeyTMP = transform.Find("View/Bg/Evade/KeyValue/SpecialKeyTMP")?.GetComponent<TMP_Text>();
            NormalBg = transform.Find("View/Bg/Normal/Icon/NormalBg")?.GetComponent<Image>();
            NormalKeyTMP = transform.Find("View/Bg/Normal/KeyValue/NormalKeyTMP")?.GetComponent<TMP_Text>();
            UltraBg = transform.Find("View/Bg/Ultra/Icon/UltraBg")?.GetComponent<Image>();
            UltraKeyTMP = transform.Find("View/Bg/Ultra/KeyValue/UltraKeyTMP")?.GetComponent<TMP_Text>();
            SkillSwitchBtn = transform.Find("View/BtnsCanvas/Btns/SkillSwitchBtn")?.GetComponent<Image>();
            SkillSepcialBtn = transform.Find("View/BtnsCanvas/Btns/SkillSepcialBtn")?.GetComponent<Image>();
            SkillEvadeBtn = transform.Find("View/BtnsCanvas/Btns/SkillEvadeBtn")?.GetComponent<Image>();
            SkillNormalBtn = transform.Find("View/BtnsCanvas/Btns/SkillNormalBtn")?.GetComponent<Image>();
            SkillUltraBtn = transform.Find("View/BtnsCanvas/Btns/SkillUltraBtn")?.GetComponent<Image>();
        }

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
        }

        /// <summary>
        /// 绑定数据模型引用 (由 Module 在初始化时调用)
        /// </summary>
        public void BindModel(SkillKeyModel model)
        {
            Model = model;
        }

        /// <summary>
        /// 数据驱动刷新接口：直接从绑定的 Model 读取只读属性并刷新界面
        /// </summary>
        public void Refresh()
        {
            if (Model == null) return;

            // 1. 特殊技与终结大招图标刷新 (直接读取 Model 计算属性)
            var specialIcon = Model.SpecialIcon;
            if (SkillSepcialBtn != null && specialIcon != null)
            {
                SkillSepcialBtn.sprite = specialIcon;
            }

            var ultIcon = Model.UltimateIcon;
            if (SkillUltraBtn != null && ultIcon != null)
            {
                SkillUltraBtn.sprite = ultIcon;
            }

            // 2. 按键提示文本与键帽背景刷新
            if (NormalKeyTMP != null) NormalKeyTMP.text = Model.NormalKeyText;
            if (SpecialKeyTMP != null) SpecialKeyTMP.text = Model.EvadeKeyText;
            if (SpeecialKeyTMP != null) SpeecialKeyTMP.text = Model.SpecialKeyText;
            if (SwitchKeyTMP != null) SwitchKeyTMP.text = Model.SwitchKeyText;
            if (UltraKeyTMP != null) UltraKeyTMP.text = Model.QteKeyText;

            if (NormalBg != null && Model.NormalKeyIcon != null) NormalBg.sprite = Model.NormalKeyIcon;
            if (EvadeBg != null && Model.EvadeKeyIcon != null) EvadeBg.sprite = Model.EvadeKeyIcon;
            if (SpecialBg != null && Model.SpecialKeyIcon != null) SpecialBg.sprite = Model.SpecialKeyIcon;
            if (SwitchBg != null && Model.SwitchKeyIcon != null) SwitchBg.sprite = Model.SwitchKeyIcon;
            if (UltraBg != null && Model.QteKeyIcon != null) UltraBg.sprite = Model.QteKeyIcon;

            // 3. 支援点数刷新
            SetAssistPoints(Model.CurrentAssistPoints, Model.MaxAssistPoints);
        }

        /// <summary>
        /// 设置指定槽位的按键提示文本与图标 (兼容直接调用)
        /// </summary>
        public void SetKeyPrompt(SkillKeySlot slot, string promptText, Sprite keyIcon = null)
        {
            switch (slot)
            {
                case SkillKeySlot.Normal:
                    if (NormalKeyTMP != null) NormalKeyTMP.text = promptText;
                    if (NormalBg != null && keyIcon != null) NormalBg.sprite = keyIcon;
                    break;
                case SkillKeySlot.Evade:
                    if (SpecialKeyTMP != null) SpecialKeyTMP.text = promptText;
                    if (EvadeBg != null && keyIcon != null) EvadeBg.sprite = keyIcon;
                    break;
                case SkillKeySlot.Special:
                    if (SpeecialKeyTMP != null) SpeecialKeyTMP.text = promptText;
                    if (SpecialBg != null && keyIcon != null) SpecialBg.sprite = keyIcon;
                    break;
                case SkillKeySlot.Switch:
                    if (SwitchKeyTMP != null) SwitchKeyTMP.text = promptText;
                    if (SwitchBg != null && keyIcon != null) SwitchBg.sprite = keyIcon;
                    break;
                case SkillKeySlot.Ult:
                    if (UltraKeyTMP != null) UltraKeyTMP.text = promptText;
                    if (UltraBg != null && keyIcon != null) UltraBg.sprite = keyIcon;
                    break;
            }
        }

        /// <summary>
        /// 设置特殊技图标 (兼容直接调用)
        /// </summary>
        public void SetSpecialIcon(Sprite icon, bool isExReady)
        {
            if (SkillSepcialBtn != null && icon != null)
            {
                SkillSepcialBtn.sprite = icon;
            }
        }

        /// <summary>
        /// 设置大招图标 (兼容直接调用)
        /// </summary>
        public void SetUltimateIcon(Sprite icon, bool isReady)
        {
            if (SkillUltraBtn != null && icon != null)
            {
                SkillUltraBtn.sprite = icon;
            }
        }

        /// <summary>
        /// 设置切人槽位的支援点数圆弧段数
        /// </summary>
        public void SetAssistPoints(int current, int max)
        {
            var switchEnergyWidget = SkillSwitchBtn != null ? SkillSwitchBtn.GetComponent<SkillSwitchBtnFill>() : null;
            if (switchEnergyWidget != null)
            {
                switchEnergyWidget.SetEnergy(current, max);
            }
        }

        /// <summary>
        /// 全量刷新 (兼容旧调用)
        /// </summary>
        public void RefreshAll(SkillKeyModel model)
        {
            if (model != null)
            {
                Model = model;
            }
            Refresh();
        }
    }

    /// <summary>
    /// 兼容旧命名的别名类
    /// </summary>
    public class SkillKeyView : SkillBtnView
    {
    }
}
