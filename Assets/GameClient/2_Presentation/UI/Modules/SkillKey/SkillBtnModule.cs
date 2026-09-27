using Game.Framework;
using Game.GamePlay;
using Game.Input;

namespace Game.UI
{
    /// <summary>
    /// 技能按键面板控制层 (MVC - Controller)
    /// 
    /// 职责：
    ///   1. 充当输入/系统领域事件与 UI 数据模型之间的调度中继
    ///   2. 启动时从全局配置 (UIManager.Settings) 提取静态配置并注入 Model
    ///   3. 挂接 Model 与 View，并在 Model 数据变动时驱动 View.Refresh()
    ///   4. 响应切人 (ActiveRoleChangedEvent)、属性 (PlayerStatChangedEvent)、支援点 (AssistPointsChangedEvent)
    ///      只负责将最新数据推送给 Model，由 Model 进行权威计算并驱动表现
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/SkillBtn/SkillBtnPanel.prefab", Layer = UILayer.Main)]
    public class SkillBtnModule : UIModule<SkillBtnView, SkillKeyModel>
    {
        private PlayerControl _playerControl;

        protected override void OnCreate()
        {
            base.OnCreate();

            _playerControl = new PlayerControl();

            // 1. 将全局静态资源配置注入 Model
            if (UIManager.Instance?.Settings?.AssetConfig != null)
            {
                Model.AssetConfig = UIManager.Instance.Settings.AssetConfig;
            }

            // 2. 将 Model 注入 View，并订阅数据驱动刷新闭环
            if (View != null)
            {
                View.BindModel(Model);
            }
            Model.OnChanged += OnModelChanged;

            // 3. 初始化 5 个按键槽位的键位映射并填充 Model
            RefreshKeyBindings();

            // 4. 订阅领域事件总线
            EventCenter.Subscribe<ActiveRoleChangedEvent>(OnActiveRoleChanged);
            EventCenter.Subscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            EventCenter.Subscribe<AssistPointsChangedEvent>(OnAssistPointsChanged);

            // 5. 首次拉取主控角色状态与队伍支援点数
            RefreshActiveRole();
            RefreshAssistPoints();
        }

        protected override void OnRemove()
        {
            EventCenter.Unsubscribe<ActiveRoleChangedEvent>(OnActiveRoleChanged);
            EventCenter.Unsubscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            EventCenter.Unsubscribe<AssistPointsChangedEvent>(OnAssistPointsChanged);

            if (Model != null)
            {
                Model.OnChanged -= OnModelChanged;
            }

            _playerControl?.Dispose();
            _playerControl = null;

            base.OnRemove();
        }

        private void OnModelChanged()
        {
            View?.Refresh();
        }

        // ─────────────────────────────────────────────
        // 按键绑定映射刷新
        // ─────────────────────────────────────────────

        /// <summary>
        /// 从全局按键映射配置中提取当前按键展示项并推送至 Model
        /// </summary>
        public void RefreshKeyBindings()
        {
            if (_playerControl == null) return;

            var keyConfig = UIManager.Instance?.Settings?.KeyBindingConfig;

            var actNormal = _playerControl.GamePlay.LightAttack;
            var actEvade = _playerControl.GamePlay.Evade;
            var actSpecial = _playerControl.GamePlay.SpecialSkill;
            var actSwitch = _playerControl.GamePlay.SwitchNext;
            var actUlt = _playerControl.GamePlay.Ultimate;

            if (keyConfig != null)
            {
                var itemNormal = keyConfig.GetDisplayItem(actNormal);
                var itemEvade = keyConfig.GetDisplayItem(actEvade);
                var itemSpecial = keyConfig.GetDisplayItem(actSpecial);
                var itemSwitch = keyConfig.GetDisplayItem(actSwitch);
                var itemUlt = keyConfig.GetDisplayItem(actUlt);

                Model.SetKeyBinding(SkillKeySlot.Normal, itemNormal.DisplayText, itemNormal.KeyIcon);
                Model.SetKeyBinding(SkillKeySlot.Evade, itemEvade.DisplayText, itemEvade.KeyIcon);
                Model.SetKeyBinding(SkillKeySlot.Special, itemSpecial.DisplayText, itemSpecial.KeyIcon);
                Model.SetKeyBinding(SkillKeySlot.Switch, itemSwitch.DisplayText, itemSwitch.KeyIcon);
                Model.SetKeyBinding(SkillKeySlot.Ult, itemUlt.DisplayText, itemUlt.KeyIcon);
            }
            else
            {
                Model.SetKeyBinding(SkillKeySlot.Normal, "鼠标左键", null);
                Model.SetKeyBinding(SkillKeySlot.Evade, "Shift", null);
                Model.SetKeyBinding(SkillKeySlot.Special, "E", null);
                Model.SetKeyBinding(SkillKeySlot.Switch, "空格", null);
                Model.SetKeyBinding(SkillKeySlot.Ult, "Q", null);
            }
        }

        // ─────────────────────────────────────────────
        // 角色切换与状态刷新
        // ─────────────────────────────────────────────

        private void OnActiveRoleChanged(ActiveRoleChangedEvent evt)
        {
            if (evt.NewEntity != null)
            {
                Model.UpdateCharacter(evt.NewEntity, evt.NewEntity.Config as RoleConfigAsset);
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
                    Model.UpdateCharacter(activeMember.Entity, activeMember.Config as RoleConfigAsset);
                    return;
                }
            }

            if (tm.LocalCharacter != null)
            {
                Model.UpdateCharacter(tm.LocalCharacter, tm.LocalCharacter.Config as RoleConfigAsset);
            }
        }

        // ─────────────────────────────────────────────
        // 实时属性变动事件处理 (MP 能量 / Stamina 喧响值)
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

            // 仅转发有效属性变更给 Model，表现层换图由 Model 状态计算驱动
            if (evt.StatType == StatType.MP || evt.StatType == StatType.Stamina)
            {
                Model.UpdateStat(evt.StatType, evt.NewValue);
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
                Model.UpdateAssistPoints(teamData.CurrentAssistPoints, teamData.MaxAssistPoints);
            }
        }

        private void OnAssistPointsChanged(AssistPointsChangedEvent evt)
        {
            Model.UpdateAssistPoints(evt.CurrentPoints, evt.MaxPoints);
        }
    }
}
