using Game.GamePlay;
using UnityEngine;

namespace Game.UI
{
    public enum SkillKeySlot
    {
        Normal = 0,
        Evade = 1,
        Special = 2,
        Switch = 3,
        Ult = 4,
        QTE = Ult
    }

    /// <summary>
    /// 技能按键面板数据模型 (MVC - Model)
    /// 
    /// 职责：
    ///   1. 权威承载 5 个按键槽位的展示文本、按键底框图标与运行状态
    ///   2. 持有 UI 静态资源配置 (AssetConfig) 与角色静态配置 (RoleConfigAsset)
    ///   3. 封装特殊技/大招的就绪状态与当前展示 Sprite 的纯计算属性 (Computed Properties)
    ///   4. 在数据变更时触发 NotifyChanged()，驱动 View 自动响应刷新
    /// </summary>
    public class SkillKeyModel : UIModel
    {
        // ── 静态资产配置引用 ────────────────────────────────
        public UIAssetConfigAsset AssetConfig { get; set; }
        public RoleConfigAsset CurrentRoleConfig { get; private set; }

        // ── 5 个槽位的按键提示文本 ──────────────────────────
        public string NormalKeyText { get; private set; } = "鼠标左键";
        public string EvadeKeyText { get; private set; } = "Shift";
        public string SpecialKeyText { get; private set; } = "E";
        public string SwitchKeyText { get; private set; } = "空格";
        public string QteKeyText { get; private set; } = "Q";

        // ── 5 个槽位的按键图标 (原版键帽/按键底框图标) ───────
        public Sprite NormalKeyIcon { get; private set; }
        public Sprite EvadeKeyIcon { get; private set; }
        public Sprite SpecialKeyIcon { get; private set; }
        public Sprite SwitchKeyIcon { get; private set; }
        public Sprite QteKeyIcon { get; private set; }

        // ── 角色与能量/喧响值状态 ────────────────────────────
        public int CurrentRoleId { get; private set; }
        public float CurrentEnergy { get; private set; }
        public float RequiredEnergy { get; private set; } = 40f;
        public float CurrentDecibel { get; private set; }
        public float MaxDecibel { get; private set; } = 3000f;

        // ── 支援点数状态 (KeySwitch 切人圆弧) ────────────────
        public int CurrentAssistPoints { get; private set; } = 6;
        public int MaxAssistPoints { get; private set; } = 6;

        // ── 衍生计算属性 (Computed Properties - View 直接只读消费) ─

        /// <summary>特殊技是否处于强化就绪状态</summary>
        public bool IsExReady => CurrentEnergy >= RequiredEnergy;

        /// <summary>终结大招是否处于就绪状态 (喧响值达到 3000)</summary>
        public bool IsUltimateReady => CurrentDecibel >= MaxDecibel;

        /// <summary>
        /// 当前特殊技图标 (优先读取角色专属配置，未配置则回退到 UIAssetConfig 私有保底)
        /// </summary>
        public Sprite SpecialIcon
        {
            get
            {
                if (IsExReady)
                {
                    return (CurrentRoleConfig?.UIConfig?.SpecialAttackExIcon != null)
                        ? CurrentRoleConfig.UIConfig.SpecialAttackExIcon
                        : AssetConfig?.GetSprite<SkillBtnModule>("BranchEx");
                }
                return (CurrentRoleConfig?.UIConfig?.SpecialAttackNormalIcon != null)
                    ? CurrentRoleConfig.UIConfig.SpecialAttackNormalIcon
                    : AssetConfig?.GetSprite<SkillBtnModule>("BranchNormal");
            }
        }

        /// <summary>
        /// 当前终结大招图标 (优先读取角色专属配置，未配置则回退到 UIAssetConfig 私有保底)
        /// </summary>
        public Sprite UltimateIcon
        {
            get
            {
                if (IsUltimateReady)
                {
                    return (CurrentRoleConfig?.UIConfig?.UltimateReadyIcon != null)
                        ? CurrentRoleConfig.UIConfig.UltimateReadyIcon
                        : AssetConfig?.GetSprite<SkillBtnModule>("UltReady");
                }
                return (CurrentRoleConfig?.UIConfig?.UltimateNormalIcon != null)
                    ? CurrentRoleConfig.UIConfig.UltimateNormalIcon
                    : AssetConfig?.GetSprite<SkillBtnModule>("UltNormal");
            }
        }

        // ── 状态更新方法 (由 Module 调用以驱动数据变更) ──────

        /// <summary>
        /// 更新主控角色与属性配置
        /// </summary>
        public void UpdateCharacter(RoleEntity entity, RoleConfigAsset config)
        {
            CurrentRoleConfig = config;
            CurrentRoleId = config != null
                ? (config.UIConfig != null && config.UIConfig.RoleID > 0 ? config.UIConfig.RoleID : config.ID)
                : 0;

            if (entity?.StatusModule != null)
            {
                CurrentEnergy = entity.StatusModule.Attributes.GetCurrent(AttributeId.Energy);
                CurrentDecibel = entity.StatusModule.Attributes.GetCurrent(AttributeId.Decibel);

                float cost = 40f;
                if (config?.UIConfig != null && config.UIConfig.EXSpecialAttackID > 0)
                {
                    cost = entity.StatusModule.GetEXSpecialAttackCost(config.UIConfig.EXSpecialAttackID);
                }
                RequiredEnergy = cost;
            }

            NotifyChanged();
        }

        /// <summary>
        /// 更新角色单项实时属性 (MP 能量 / Stamina 喧响值)
        /// </summary>
        public void UpdateStat(StatType statType, float newValue)
        {
            bool changed = false;
            if (statType == StatType.MP)
            {
                if (!Mathf.Approximately(CurrentEnergy, newValue))
                {
                    CurrentEnergy = newValue;
                    changed = true;
                }
            }
            else if (statType == StatType.Stamina)
            {
                if (!Mathf.Approximately(CurrentDecibel, newValue))
                {
                    CurrentDecibel = newValue;
                    changed = true;
                }
            }

            if (changed)
            {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 更新全队支援点数
        /// </summary>
        public void UpdateAssistPoints(int current, int max)
        {
            if (CurrentAssistPoints != current || MaxAssistPoints != max)
            {
                CurrentAssistPoints = current;
                MaxAssistPoints = max;
                NotifyChanged();
            }
        }

        /// <summary>
        /// 设置指定槽位的按键提示与图标
        /// </summary>
        public void SetKeyBinding(SkillKeySlot slot, string text, Sprite icon)
        {
            switch (slot)
            {
                case SkillKeySlot.Normal:
                    NormalKeyText = text;
                    NormalKeyIcon = icon;
                    break;
                case SkillKeySlot.Evade:
                    EvadeKeyText = text;
                    EvadeKeyIcon = icon;
                    break;
                case SkillKeySlot.Special:
                    SpecialKeyText = text;
                    SpecialKeyIcon = icon;
                    break;
                case SkillKeySlot.Switch:
                    SwitchKeyText = text;
                    SwitchKeyIcon = icon;
                    break;
                case SkillKeySlot.Ult:
                    QteKeyText = text;
                    QteKeyIcon = icon;
                    break;
            }
            NotifyChanged();
        }

        public string GetKeyText(SkillKeySlot slot)
        {
            return slot switch
            {
                SkillKeySlot.Normal => NormalKeyText,
                SkillKeySlot.Evade => EvadeKeyText,
                SkillKeySlot.Special => SpecialKeyText,
                SkillKeySlot.Switch => SwitchKeyText,
                SkillKeySlot.Ult => QteKeyText,
                _ => string.Empty
            };
        }

        public Sprite GetKeyIcon(SkillKeySlot slot)
        {
            return slot switch
            {
                SkillKeySlot.Normal => NormalKeyIcon,
                SkillKeySlot.Evade => EvadeKeyIcon,
                SkillKeySlot.Special => SpecialKeyIcon,
                SkillKeySlot.Switch => SwitchKeyIcon,
                SkillKeySlot.Ult => QteKeyIcon,
                _ => null
            };
        }

        public override void Reset()
        {
            CurrentRoleConfig = null;
            AssetConfig = null;
            base.Reset();
        }
    }
}
