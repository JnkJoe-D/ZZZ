using Game.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 玩家按键操作面板 View (挂载于 SkillKeyView.prefab 根节点)
    /// 纯粹的受控表现层，负责 5 个槽位图标、发光外环与按键提示文本的展示
    /// </summary>
    public class SkillKeyView : UIView
    {
        [Header("普攻 (KeyNormal)")]
        [SerializeField] private Image _imgNormalIcon;
        [SerializeField] private TextMeshProUGUI _tmpNormalKey;

        [Header("闪避 (KeyEvade)")]
        [SerializeField] private Image _imgEvadeIcon;
        [SerializeField] private TextMeshProUGUI _tmpEvadeKey;

        [Header("特殊技 (KeySpecial)")]
        [SerializeField] private Image _imgSpecialIcon;
        [SerializeField] private TextMeshProUGUI _tmpSpecialKey;

        [Header("换人 (KeySwitch)")]
        [SerializeField] private Image _imgSwitchIcon;
        [SerializeField] private TextMeshProUGUI _tmpSwitchKey;
        [SerializeField] private AssistPointUIControl _switchEnergyWidget;

        [Header("连携大招 (KeyQTE)")]
        [SerializeField] private Image _imgQteIcon;
        [SerializeField] private Image _imgQteGlow;
        [SerializeField] private TextMeshProUGUI _tmpQteKey;

        public override void OnInit()
        {
            base.OnInit();
            EnsureBindings();
        }

        private void EnsureBindings()
        {
            var keysRoot = transform.Find("View/Keys");
            if (keysRoot == null)
            {
                GLog.Warning(LogTags.UI, "未找到 'View/Keys' 节点");
                return;
            }

            if (_imgNormalIcon == null) _imgNormalIcon = keysRoot.Find("KeyNormal/Icons/IconKey")?.GetComponent<Image>();
            if (_tmpNormalKey == null) _tmpNormalKey = keysRoot.Find("KeyNormal/KeyValue/TmpKey")?.GetComponent<TextMeshProUGUI>();

            if (_imgEvadeIcon == null) _imgEvadeIcon = keysRoot.Find("KeyEvade/Icons/IconKey")?.GetComponent<Image>();
            if (_tmpEvadeKey == null) _tmpEvadeKey = keysRoot.Find("KeyEvade/KeyValue/TmpKey")?.GetComponent<TextMeshProUGUI>();

            if (_imgSpecialIcon == null) _imgSpecialIcon = keysRoot.Find("KeySpecial/Icons/IconKey")?.GetComponent<Image>();
            if (_tmpSpecialKey == null) _tmpSpecialKey = keysRoot.Find("KeySpecial/KeyValue/TmpKey")?.GetComponent<TextMeshProUGUI>();

            if (_imgSwitchIcon == null) _imgSwitchIcon = keysRoot.Find("KeySwitch/Icons/IconKey")?.GetComponent<Image>();
            if (_tmpSwitchKey == null) _tmpSwitchKey = keysRoot.Find("KeySwitch/KeyValue/TmpKey")?.GetComponent<TextMeshProUGUI>();
            if (_switchEnergyWidget == null) _switchEnergyWidget = keysRoot.Find("KeySwitch/Icons/IconKey")?.GetComponent<AssistPointUIControl>();

            if (_imgQteIcon == null) _imgQteIcon = keysRoot.Find("KeyQTE/Icons/IconKey")?.GetComponent<Image>();
            if (_imgQteGlow == null) _imgQteGlow = keysRoot.Find("KeyQTE/Icons/IconKey (1)")?.GetComponent<Image>();
            if (_tmpQteKey == null) _tmpQteKey = keysRoot.Find("KeyQTE/KeyValue/TmpKey")?.GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// 设置指定槽位的按键提示文本与图标
        /// </summary>
        public void SetKeyPrompt(SkillKeySlot slot, string promptText, Sprite keyIcon = null)
        {
            EnsureBindings();
            switch (slot)
            {
                case SkillKeySlot.Normal:
                    if (_tmpNormalKey != null) _tmpNormalKey.text = promptText;
                    break;
                case SkillKeySlot.Evade:
                    if (_tmpEvadeKey != null) _tmpEvadeKey.text = promptText;
                    break;
                case SkillKeySlot.Special:
                    if (_tmpSpecialKey != null) _tmpSpecialKey.text = promptText;
                    break;
                case SkillKeySlot.Switch:
                    if (_tmpSwitchKey != null) _tmpSwitchKey.text = promptText;
                    break;
                case SkillKeySlot.QTE:
                    if (_tmpQteKey != null) _tmpQteKey.text = promptText;
                    break;
            }
        }

        /// <summary>
        /// 设置特殊技图标与达标发光状态
        /// </summary>
        public void SetSpecialIcon(Sprite icon, bool isExReady)
        {
            EnsureBindings();
            if (_imgSpecialIcon != null && icon != null)
            {
                _imgSpecialIcon.sprite = icon;
            }
        }

        /// <summary>
        /// 设置大招图标与外环高光就绪状态
        /// </summary>
        public void SetUltimateIcon(Sprite icon, bool isReady)
        {
            EnsureBindings();
            if (_imgQteIcon != null && icon != null)
            {
                _imgQteIcon.sprite = icon;
            }

            if (_imgQteGlow != null)
            {
                _imgQteGlow.gameObject.SetActive(isReady);
            }
        }

        /// <summary>
        /// 设置切人槽位的支援点数圆弧段数
        /// </summary>
        public void SetAssistPoints(int current, int max)
        {
            EnsureBindings();
            if (_switchEnergyWidget != null)
            {
                _switchEnergyWidget.SetEnergy(current, max);
            }
        }

        /// <summary>
        /// 全量刷新来自 Model 的表现状态
        /// </summary>
        public void RefreshAll(SkillKeyModel model)
        {
            if (model == null) return;

            SetKeyPrompt(SkillKeySlot.Normal, model.NormalKeyText, model.NormalKeyIcon);
            SetKeyPrompt(SkillKeySlot.Evade, model.EvadeKeyText, model.EvadeKeyIcon);
            SetKeyPrompt(SkillKeySlot.Special, model.SpecialKeyText, model.SpecialKeyIcon);
            SetKeyPrompt(SkillKeySlot.Switch, model.SwitchKeyText, model.SwitchKeyIcon);
            SetKeyPrompt(SkillKeySlot.QTE, model.QteKeyText, model.QteKeyIcon);

            SetSpecialIcon(model.CurrentSpecialIcon, model.IsExReady);
            SetUltimateIcon(model.CurrentUltimateIcon, model.IsUltimateReady);
            SetAssistPoints(model.CurrentAssistPoints, model.MaxAssistPoints);
        }
    }
}
