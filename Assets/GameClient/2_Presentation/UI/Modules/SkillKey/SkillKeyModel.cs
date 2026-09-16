using UnityEngine;

namespace Game.UI
{
    public enum SkillKeySlot
    {
        Normal = 0,
        Evade = 1,
        Special = 2,
        Switch = 3,
        QTE = 4
    }

    /// <summary>
    /// 技能按键面板数据模型
    /// 承载 5 个按键槽位的展示文本、按键图标及特殊技/大招达标激活状态
    /// </summary>
    public class SkillKeyModel : UIModel
    {
        // ── 5 个槽位的按键提示文本 ──────────────────────────
        public string NormalKeyText = "鼠标左键";
        public string EvadeKeyText = "Shift";
        public string SpecialKeyText = "E";
        public string SwitchKeyText = "空格";
        public string QteKeyText = "Q";

        // ── 5 个槽位的按键图标 (原版键帽/按键底框图标) ───────
        public Sprite NormalKeyIcon;
        public Sprite EvadeKeyIcon;
        public Sprite SpecialKeyIcon;
        public Sprite SwitchKeyIcon;
        public Sprite QteKeyIcon;

        // ── 当前主控角色与实体状态 ──────────────────────────
        public int CurrentRoleId;

        // ── 特殊技状态 (KeySpecial) ──────────────────────────
        public float CurrentEnergy;
        public float RequiredEnergy = 40f;
        public bool IsExReady;
        public Sprite CurrentSpecialIcon;

        // ── 大招状态 (KeyQTE) ────────────────────────────────
        public float CurrentDecibel;
        public float MaxDecibel = 3000f;
        public bool IsUltimateReady;
        public Sprite CurrentUltimateIcon;

        // ── 支援点数状态 (KeySwitch 切人圆弧) ────────────────
        public int CurrentAssistPoints = 6;
        public int MaxAssistPoints = 6;

        /// <summary>
        /// 获取指定槽位的提示文本
        /// </summary>
        public string GetKeyText(SkillKeySlot slot)
        {
            return slot switch
            {
                SkillKeySlot.Normal => NormalKeyText,
                SkillKeySlot.Evade => EvadeKeyText,
                SkillKeySlot.Special => SpecialKeyText,
                SkillKeySlot.Switch => SwitchKeyText,
                SkillKeySlot.QTE => QteKeyText,
                _ => string.Empty
            };
        }

        /// <summary>
        /// 获取指定槽位的按键图标
        /// </summary>
        public Sprite GetKeyIcon(SkillKeySlot slot)
        {
            return slot switch
            {
                SkillKeySlot.Normal => NormalKeyIcon,
                SkillKeySlot.Evade => EvadeKeyIcon,
                SkillKeySlot.Special => SpecialKeyIcon,
                SkillKeySlot.Switch => SwitchKeyIcon,
                SkillKeySlot.QTE => QteKeyIcon,
                _ => null
            };
        }
    }
}
