using Game.Framework;
using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    public class ATAttackWarningHandler : IAttackWarningHandler
    {
        private readonly CharacterEntity _entity;
        private AttackWarningMarker _marker;
        public ATAttackWarningHandler(CharacterEntity entity)
        {
            _entity = entity;
            _marker = null;
        }
        public void RegisterWarningMarker(AttackWarningClip clip)
        {
            if (clip == null) return;
            if (_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }

            // 1. 初始信号继承自 Clip 配置
            WarningSignalType resolvedSignal = clip.SignalType;

            // 2. 核心动态裁决：若原本配置为黄光（可招架招式），检查当前玩家小队是否有足够支援点数
            if (resolvedSignal == WarningSignalType.Yellow_Parryable)
            {
                bool canParry = CheckPartyCanParry(clip.ParryWeight);
                if (!canParry)
                {
                    resolvedSignal = WarningSignalType.Red_Unparryable;
                    GLog.Info(LogTags.Combat, $"玩家小队支援点数不足以招架({clip.ParryWeight})，预警信号由 Yellow 转为 Red");
                }
            }

            AttackWarningMarker marker = new AttackWarningMarker
            {
                Attacker = _entity,
                SignalType = resolvedSignal,
                ParryWeight = clip.ParryWeight,
                DetectionRadius = clip.DetectionRadius > 0 ? clip.DetectionRadius : 10.0f,
                DetectionAngle = clip.DetectionAngle > 0 ? clip.DetectionAngle : 180.0f,
                CoverageShape = clip.CoverageShape,
                CoverageCenterOffset = clip.CoverageCenterOffset,
                ClashPositionOffset = clip.ClashPositionOffset,
                AllowInPlaceParry = clip.AllowInPlaceParry
            };
            if (CombatWarningManager.Register(marker))
            {
                _marker = marker;
            }
        }

        private static bool CheckPartyCanParry(ParryWeight weight)
        {
            var tm = TeamManager.Instance;
            if (tm == null || tm.LocalCharacter == null) return false;

            // 获取当前出场主控角色的下一个待切入队友
            PartyMember nextMember = tm.GetNextPartyMember(tm.LocalCharacter);
            if (nextMember?.Entity == null) return false;

            RoleEntity incomingRole = nextMember.Entity;
            if (incomingRole.Config is not RoleConfigAsset roleConfig || roleConfig.SupportConfig == null)
                return false;

            var supportCfg = roleConfig.SupportConfig;
            ActionConfigAsset targetAction = null;
            if (supportCfg.SupportType == RoleSupportType.ParryAid)
            {
                targetAction = (weight == ParryWeight.Heavy) ? supportCfg.ParryHeavyAction : supportCfg.ParryLightAction;
            }
            else // EvasionAid
            {
                targetAction = supportCfg.EvasionAidAction;
            }

            if (targetAction == null) return false;

            ISkillCostHandler costHandler = incomingRole.ActionController?.SkillCostHandler ?? new DefaultSkillCostHandler();
            return costHandler.CheckSkillRequirement(targetAction, incomingRole);
        }

        public void RegisterWarningMarker(WarningSignalType signalType, float detectionRadius, float detectionAngle)
        {
            if (_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }

            AttackWarningMarker marker = new AttackWarningMarker
            {
                Attacker = _entity,
                SignalType = signalType,
                DetectionRadius = detectionRadius,
                DetectionAngle = detectionAngle
            };
            if (CombatWarningManager.Register(marker))
            {
                _marker = marker;
            }
        }

        public void UnregisterWarningMarker()
        {
            if(_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }
        }

        public void OnParryContractEnter(ParryContractClip clip)
        {
            // 拼刀契约生命周期窗口激活
        }

        public void OnParryContractExit()
        {
            if (_entity != null)
            {
                CombatWarningManager.UnregisterContractsByAttacker(_entity);
            }
        }
    }
}
