using Game.Framework;
using Game.Logic;
using Game.UI;
using UnityEngine;

namespace Game.UI.Modules.RoleStatus
{
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/RoleStatus/StatusPanel.prefab", Layer = UILayer.Window)]
    public class StatusPanelModule : UIModule<StatusPanelView, StatusPanelModel>
    {
        // 存放对应槽位生成的 Mechanic UI View
        private System.Collections.Generic.Dictionary<int, BaseMechanicView> _mechanicViews = new();

        protected override void OnCreate()
        {
            base.OnCreate();

            // 将 Model 注入到 View 中，实现完全的 MVC 数据驱动
            View.Model = Model;

            // 订阅底层属性数值变化事件 (HP/MP)
            EventCenter.Subscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            // 订阅核心机制变化事件
            EventCenter.Subscribe<MechanicStatChangedEvent>(OnMechanicStatChanged);
            // 订阅主控角色切换事件
            EventCenter.Subscribe<ActiveCharacterChangedEvent>(OnActiveCharacterChanged);

            // 界面刚创建时主动拉取一次当前状态并初始化机制UI
            RefreshAllStatus();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EventCenter.Unsubscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);
            EventCenter.Unsubscribe<MechanicStatChangedEvent>(OnMechanicStatChanged);
            EventCenter.Unsubscribe<ActiveCharacterChangedEvent>(OnActiveCharacterChanged);
            
            // 清理动态生成的UI
            foreach (var view in _mechanicViews.Values)
            {
                if (view != null) GameObject.Destroy(view.gameObject);
            }
            _mechanicViews.Clear();
        }

        private void OnActiveCharacterChanged(ActiveCharacterChangedEvent evt)
        {
            // 角色切换时，全局刷新一次映射
            RefreshAllStatus();
        }

        private int GetViewSlotIndex(int memberSlotIndex)
        {
            int activeSlot = TeamManager.Instance.ActiveSlotIndex;
            if (activeSlot < 0) activeSlot = 0;
            int count = TeamManager.Instance.PartyMembers.Count;
            if (count <= 0) return 0;
            
            // 0 -> Active, 1 -> Next, 2 -> Next.Next
            return (memberSlotIndex - activeSlot + count) % count;
        }



        private void RefreshAllStatus()
        {
            var partyMembers = TeamManager.Instance?.PartyMembers;
            if (partyMembers == null) return;

            // 获取数据，直接分发给 View 进行表现刷新
            foreach (var member in partyMembers)
            {
                if (member == null) continue;
                int viewSlotIndex = GetViewSlotIndex(member.SlotIndex);

                if (member.Config is RoleConfigAsset roleConfig && roleConfig.UIConfig?.RoleIconGeneral != null)
                {
                    Model.RoleData[viewSlotIndex].RoleIcon = roleConfig.UIConfig.RoleIconGeneral;
                    View.UpdateRoleIcon(viewSlotIndex);
                }

                if (member.Entity?.StatusModule != null)
                {
                    float hpPercent = member.Entity.StatusModule.Attributes.GetPercent(AttributeId.HP);
                    
                    float currentEnergy = member.Entity.StatusModule.Attributes.GetCurrent(AttributeId.Energy);
                    float maxEnergy = member.Entity.StatusModule.Attributes.GetFinal(AttributeId.MaxEnergy);
                    float energyPercent = maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
                    
                    // 获取强化特殊技的消耗阈值
                    float thresholdPercent = 0f;
                    if (member.Config is RoleConfigAsset rc && rc.UIConfig != null && rc.UIConfig.EXSpecialAttackID > 0)
                    {
                        float cost = member.Entity.StatusModule.GetEXSpecialAttackCost(rc.UIConfig.EXSpecialAttackID);
                        thresholdPercent = maxEnergy > 0 ? cost / maxEnergy : 0f;
                    }

                    Model.RoleData[viewSlotIndex].HpPercent = hpPercent;
                    Model.RoleData[viewSlotIndex].EnergyPercent = energyPercent;
                    Model.RoleData[viewSlotIndex].EnergyThresholdPercent = thresholdPercent;

                    View.UpdateHp(viewSlotIndex);
                    View.UpdateEnergy(viewSlotIndex);
                }

                // 动态生成或重新挂载角色专属机制UI
                if (member.Config is RoleConfigAsset roleCfg && roleCfg.UIConfig?.MechanicUIPrefab != null)
                {
                    // 获取用于挂载的父节点
                    Transform anchor = View.GetRoleAnchor(viewSlotIndex);
                    if (anchor != null)
                    {
                        if (!_mechanicViews.TryGetValue(member.SlotIndex, out var mechanicView))
                        {
                            GameObject mechanicInst = GameObject.Instantiate(roleCfg.UIConfig.MechanicUIPrefab, anchor);
                            mechanicView = mechanicInst.GetComponent<BaseMechanicView>();
                            if (mechanicView != null)
                            {
                                mechanicView.OnInit();
                                _mechanicViews[member.SlotIndex] = mechanicView;
                            }
                        }
                        else
                        {
                            // 角色切换导致该角色的UI槽位发生变化，重新挂载到新的视图槽位下
                            mechanicView.transform.SetParent(anchor, false);
                        }
                    }
                }
            }
        }

        private void OnMechanicStatChanged(MechanicStatChangedEvent evt)
        {
            var partyMembers = TeamManager.Instance?.PartyMembers;
            if (partyMembers == null) return;

            PartyMember targetMember = null;
            foreach (var member in partyMembers)
            {
                if (member?.Entity != null && member.Entity.GetInstanceID() == evt.PlayerId)
                {
                    targetMember = member;
                    break;
                }
            }

            if (targetMember != null && _mechanicViews.TryGetValue(targetMember.SlotIndex, out var mechanicView))
            {
                mechanicView.UpdateView(evt.Data);
            }
        }

        private void OnPlayerStatChanged(PlayerStatChangedEvent evt)
        {
            var partyMembers = TeamManager.Instance?.PartyMembers;
            if (partyMembers == null) return;

            PartyMember targetMember = null;
            foreach (var member in partyMembers)
            {
                if (member?.Entity != null && member.Entity.GetInstanceID() == evt.PlayerId)
                {
                    targetMember = member;
                    break;
                }
            }

            if (targetMember == null) return;
            int viewSlotIndex = GetViewSlotIndex(targetMember.SlotIndex);

            // StatType.HP 对应 AttributeId.HP
            if (evt.StatType == StatType.HP)
            {
                float percent = evt.MaxValue > 0 ? evt.NewValue / evt.MaxValue : 0;
                Model.RoleData[viewSlotIndex].HpPercent = percent;
                View.UpdateHp(viewSlotIndex);
            }
            // StatType.MP 对应 AttributeId.Energy (框架底层的默认映射)
            else if (evt.StatType == StatType.MP)
            {
                float percent = evt.MaxValue > 0 ? evt.NewValue / evt.MaxValue : 0;
                
                float thresholdPercent = 0f;
                if (targetMember.Config is RoleConfigAsset rCfg && rCfg.UIConfig != null && rCfg.UIConfig.EXSpecialAttackID > 0 && targetMember.Entity?.StatusModule != null)
                {
                    float cost = targetMember.Entity.StatusModule.GetEXSpecialAttackCost(rCfg.UIConfig.EXSpecialAttackID);
                    thresholdPercent = evt.MaxValue > 0 ? cost / evt.MaxValue : 0f;
                }
                
                Model.RoleData[viewSlotIndex].EnergyPercent = percent;
                Model.RoleData[viewSlotIndex].EnergyThresholdPercent = thresholdPercent;
                View.UpdateEnergy(viewSlotIndex);
            }
        }
    }
}
