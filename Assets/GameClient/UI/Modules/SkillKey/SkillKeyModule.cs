using System;
using System.Threading.Tasks;
using Game.Framework;
using Game.Logic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 技能按键面板控制层 (Module)
    /// 
    /// 职责：
    ///   1. 从全局 UI 配置 (UIGlobalSettingAsset.KeyBindingConfig) 和 PlayerControl 解析按键提示与图标
    ///   2. 监听切人事件 (ActiveCharacterChangedEvent)，切换主控角色的特殊技/大招状态与专属图标
    ///   3. 监听属性变化 (PlayerStatChangedEvent: MP 能量 / Stamina 喧响值)，精准执行状态跃迁驱动 View 表现
    ///   4. 缺省保底机制：大多数角色自动使用通用保底图标，特殊角色 (仪玄/叶瞬光) 优先使用其配置覆写
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/SkillKey/SkillKeyView.prefab", Layer = UILayer.Main)]
    public class SkillKeyModule : UIModule<SkillKeyView, SkillKeyModel>
    {
        // ── 默认保底图标缓存 ─────────────────────────
        private Sprite _defaultBranchExSprite;       // SkillBtnBranch.png (通用达标彩色雷暴分支)
        private Sprite _defaultBranchNormalSprite;   // SkillBtnBranch2.png (通用未达标灰色分支)
        private Sprite _defaultUltimateReadySprite;  // IconRoleSkillKeyUltimate.png (通用金色大招就绪)
        private Sprite _defaultUltimateNormalSprite; // SkillQTE.png (通用大招基础暗色)

        // ── 输入控制对象引用 ─────────────────────────
        private Game.Input.PlayerControl _playerControl;

        protected override void OnCreate()
        {
            base.OnCreate();

            _playerControl = new Game.Input.PlayerControl();

            // 1. 预加载默认保底图标
            _ = LoadDefaultSpritesAsync();

            // 2. 初始化 5 个按键槽位的键位绑定与提示文本
            RefreshKeyBindings();

            // 3. 订阅事件闭环
            EventCenter.Subscribe<ActiveCharacterChangedEvent>(OnActiveCharacterChanged);
            EventCenter.Subscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            EventCenter.Subscribe<AssistPointsChangedEvent>(OnAssistPointsChanged);

            // 4. 首次加载拉取当前主控角色状态与队伍支援点数
            RefreshActiveRole();
            RefreshAssistPoints();
        }

        protected override void OnRemove()
        {
            EventCenter.Unsubscribe<ActiveCharacterChangedEvent>(OnActiveCharacterChanged);
            EventCenter.Unsubscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            EventCenter.Unsubscribe<AssistPointsChangedEvent>(OnAssistPointsChanged);

            _playerControl?.Dispose();
            _playerControl = null;

            base.OnRemove();
        }

        // ─────────────────────────────────────────────
        // 默认保底资源加载
        // ─────────────────────────────────────────────

        private async Task LoadDefaultSpritesAsync()
        {
            try
            {
                var rm = Game.Resource.ResourceManager.Instance;
                if (rm != null)
                {
                    _defaultBranchExSprite = await rm.LoadAssetAsync<Sprite>("Assets/Dependencies/UI/Sprite/Icon/SkillKey/SkillBtnBranch.png");
                    _defaultBranchNormalSprite = await rm.LoadAssetAsync<Sprite>("Assets/Dependencies/UI/Sprite/Icon/SkillKey/SkillBtnBranch2.png");
                    _defaultUltimateReadySprite = await rm.LoadAssetAsync<Sprite>("Assets/Dependencies/UI/Sprite/Icon/SkillKey/IconRoleSkillKeyUltimate.png");
                    _defaultUltimateNormalSprite = await rm.LoadAssetAsync<Sprite>("Assets/Dependencies/UI/Sprite/Icon/SkillKey/SkillQTE.png");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SkillKeyModule] 异步加载默认按键图标异常 (将使用运行时回退): {ex.Message}");
            }

            // 资源加载完成后立即刷新一次视图
            RefreshActiveRole();
        }

        // ─────────────────────────────────────────────
        // 按键绑定映射刷新
        // ─────────────────────────────────────────────

        /// <summary>
        /// 从全局 UI 配置中提取按键展示项并刷新 View
        /// 支持任意改键热刷新
        /// </summary>
        public void RefreshKeyBindings()
        {
            if (_playerControl == null) return;

            // 获取全局按键映射配置资产
            var keyConfig = UIManager.Instance?.Settings?.KeyBindingConfig;

            // 5 个核心槽位 Action
            var actNormal = _playerControl.GamePlay.LightAttack;
            var actEvade = _playerControl.GamePlay.Evade;
            var actSpecial = _playerControl.GamePlay.SpecialSkill;
            var actSwitch = _playerControl.GamePlay.SwitchNext;
            var actQte = _playerControl.GamePlay.Ultimate;

            if (keyConfig != null)
            {
                var itemNormal = keyConfig.GetDisplayItem(actNormal);
                var itemEvade = keyConfig.GetDisplayItem(actEvade);
                var itemSpecial = keyConfig.GetDisplayItem(actSpecial);
                var itemSwitch = keyConfig.GetDisplayItem(actSwitch);
                var itemQte = keyConfig.GetDisplayItem(actQte);

                Model.NormalKeyText = itemNormal.DisplayText;
                Model.NormalKeyIcon = itemNormal.KeyIcon;

                Model.EvadeKeyText = itemEvade.DisplayText;
                Model.EvadeKeyIcon = itemEvade.KeyIcon;

                Model.SpecialKeyText = itemSpecial.DisplayText;
                Model.SpecialKeyIcon = itemSpecial.KeyIcon;

                Model.SwitchKeyText = itemSwitch.DisplayText;
                Model.SwitchKeyIcon = itemSwitch.KeyIcon;

                Model.QteKeyText = itemQte.DisplayText;
                Model.QteKeyIcon = itemQte.KeyIcon;
            }
            else
            {
                // 若配置资产尚未准备就绪，使用默认兜底字符
                Model.NormalKeyText = "鼠标左键";
                Model.EvadeKeyText = "Shift";
                Model.SpecialKeyText = "E";
                Model.SwitchKeyText = "空格";
                Model.QteKeyText = "Q";
            }

            if (View != null)
            {
                View.SetKeyPrompt(SkillKeySlot.Normal, Model.NormalKeyText, Model.NormalKeyIcon);
                View.SetKeyPrompt(SkillKeySlot.Evade, Model.EvadeKeyText, Model.EvadeKeyIcon);
                View.SetKeyPrompt(SkillKeySlot.Special, Model.SpecialKeyText, Model.SpecialKeyIcon);
                View.SetKeyPrompt(SkillKeySlot.Switch, Model.SwitchKeyText, Model.SwitchKeyIcon);
                View.SetKeyPrompt(SkillKeySlot.QTE, Model.QteKeyText, Model.QteKeyIcon);
            }
        }

        // ─────────────────────────────────────────────
        // 角色切换与状态刷新
        // ─────────────────────────────────────────────

        private void OnActiveCharacterChanged(ActiveCharacterChangedEvent evt)
        {
            if (evt.NewEntity != null)
            {
                UpdateRoleState(evt.NewEntity, evt.NewEntity.Config as RoleConfigAsset);
            }
            else
            {
                RefreshActiveRole();
            }
        }

        private void RefreshActiveRole()
        {
            var tm = TeamManager.Instance;
            if (tm == null) return;

            int activeSlot = tm.ActiveSlotIndex;
            var members = tm.PartyMembers;
            if (members != null && activeSlot >= 0 && activeSlot < members.Count)
            {
                var activeMember = members[activeSlot];
                if (activeMember?.Entity != null)
                {
                    UpdateRoleState(activeMember.Entity, activeMember.Config as RoleConfigAsset);
                    return;
                }
            }

            if (tm.LocalCharacter != null)
            {
                UpdateRoleState(tm.LocalCharacter, tm.LocalCharacter.Config as RoleConfigAsset);
            }
        }

        private void UpdateRoleState(RoleEntity entity, RoleConfigAsset roleConfig)
        {
            if (entity == null || entity.StatusModule == null) return;

            Model.CurrentRoleId = roleConfig != null 
                ? (roleConfig.UIConfig != null && roleConfig.UIConfig.RoleID > 0 ? roleConfig.UIConfig.RoleID : roleConfig.ID) 
                : 0;

            // 1. 特殊技状态计算
            float currentEnergy = entity.StatusModule.Attributes.GetCurrent(AttributeId.Energy);
            float cost = 40f;
            if (roleConfig?.UIConfig != null && roleConfig.UIConfig.EXSpecialAttackID > 0)
            {
                cost = entity.StatusModule.GetEXSpecialAttackCost(roleConfig.UIConfig.EXSpecialAttackID);
            }
            Model.CurrentEnergy = currentEnergy;
            Model.RequiredEnergy = cost;
            Model.IsExReady = currentEnergy >= cost;

            // 特殊技图标决策 (优先读取角色专属配置，未配则回退到全局默认保底)
            Sprite specialIcon;
            if (Model.IsExReady)
            {
                specialIcon = (roleConfig?.UIConfig?.SpecialAttackExIcon != null)
                    ? roleConfig.UIConfig.SpecialAttackExIcon
                    : _defaultBranchExSprite;
            }
            else
            {
                specialIcon = (roleConfig?.UIConfig?.SpecialAttackNormalIcon != null)
                    ? roleConfig.UIConfig.SpecialAttackNormalIcon
                    : _defaultBranchNormalSprite;
            }
            Model.CurrentSpecialIcon = specialIcon;

            // 2. 终结大招状态计算
            float currentDecibel = entity.StatusModule.Attributes.GetCurrent(AttributeId.Decibel);
            Model.CurrentDecibel = currentDecibel;
            Model.IsUltimateReady = currentDecibel >= 3000f;

            // 大招图标决策
            Sprite ultimateIcon;
            if (Model.IsUltimateReady)
            {
                ultimateIcon = (roleConfig?.UIConfig?.UltimateReadyIcon != null)
                    ? roleConfig.UIConfig.UltimateReadyIcon
                    : _defaultUltimateReadySprite;
            }
            else
            {
                ultimateIcon = (roleConfig?.UIConfig?.UltimateNormalIcon != null)
                    ? roleConfig.UIConfig.UltimateNormalIcon
                    : _defaultUltimateNormalSprite;
            }
            Model.CurrentUltimateIcon = ultimateIcon;

            // 3. 推送 View 刷新
            if (View != null)
            {
                View.SetSpecialIcon(Model.CurrentSpecialIcon, Model.IsExReady);
                View.SetUltimateIcon(Model.CurrentUltimateIcon, Model.IsUltimateReady);
            }
        }

        // ─────────────────────────────────────────────
        // 实时属性变动事件处理 (防频闪与状态跃迁守载)
        // ─────────────────────────────────────────────

        private void OnPlayerStatChanged(PlayerStatChangedEvent evt)
        {
            var tm = TeamManager.Instance;
            var activeEntity = tm?.LocalCharacter;
            if (activeEntity == null)
            {
                int activeSlot = tm != null ? tm.ActiveSlotIndex : -1;
                var members = tm?.PartyMembers;
                if (members != null && activeSlot >= 0 && activeSlot < members.Count)
                {
                    activeEntity = members[activeSlot]?.Entity;
                }
            }

            if (activeEntity == null || evt.PlayerId != activeEntity.GetInstanceID()) return;

            var roleConfig = activeEntity.Config as RoleConfigAsset;

            if (evt.StatType == StatType.MP)
            {
                float newEnergy = evt.NewValue;
                Model.CurrentEnergy = newEnergy;
                bool isExReady = newEnergy >= Model.RequiredEnergy;

                // 状态跃迁检测：仅在跨越阈值时触发表现层图标切换，避免高频无意义重绘
                if (isExReady != Model.IsExReady)
                {
                    Model.IsExReady = isExReady;

                    Sprite specialIcon = isExReady
                        ? (roleConfig?.UIConfig?.SpecialAttackExIcon != null ? roleConfig.UIConfig.SpecialAttackExIcon : _defaultBranchExSprite)
                        : (roleConfig?.UIConfig?.SpecialAttackNormalIcon != null ? roleConfig.UIConfig.SpecialAttackNormalIcon : _defaultBranchNormalSprite);

                    Model.CurrentSpecialIcon = specialIcon;

                    if (View != null)
                    {
                        View.SetSpecialIcon(specialIcon, isExReady);
                    }
                }
            }
            else if (evt.StatType == StatType.Stamina)
            {
                float newDecibel = evt.NewValue;
                Model.CurrentDecibel = newDecibel;
                bool isUltimateReady = newDecibel >= 3000f;

                // 状态跃迁检测：仅在喧响值满额/失去满额时切换大招高光外环与图标
                if (isUltimateReady != Model.IsUltimateReady)
                {
                    Model.IsUltimateReady = isUltimateReady;

                    Sprite ultimateIcon = isUltimateReady
                        ? (roleConfig?.UIConfig?.UltimateReadyIcon != null ? roleConfig.UIConfig.UltimateReadyIcon : _defaultUltimateReadySprite)
                        : (roleConfig?.UIConfig?.UltimateNormalIcon != null ? roleConfig.UIConfig.UltimateNormalIcon : _defaultUltimateNormalSprite);

                    Model.CurrentUltimateIcon = ultimateIcon;

                    if (View != null)
                    {
                        View.SetUltimateIcon(ultimateIcon, isUltimateReady);
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // 队伍支援点数 (切人圆弧段数) 刷新与事件处理
        // ─────────────────────────────────────────────

        private void RefreshAssistPoints()
        {
            var teamData = TeamRuntimeData.Instance;
            if (teamData != null)
            {
                Model.CurrentAssistPoints = teamData.CurrentAssistPoints;
                Model.MaxAssistPoints = teamData.MaxAssistPoints;

                if (View != null)
                {
                    View.SetAssistPoints(Model.CurrentAssistPoints, Model.MaxAssistPoints);
                }
            }
        }

        private void OnAssistPointsChanged(AssistPointsChangedEvent evt)
        {
            Model.CurrentAssistPoints = evt.CurrentPoints;
            Model.MaxAssistPoints = evt.MaxPoints;

            if (View != null)
            {
                View.SetAssistPoints(evt.CurrentPoints, evt.MaxPoints);
            }
        }
    }
}
