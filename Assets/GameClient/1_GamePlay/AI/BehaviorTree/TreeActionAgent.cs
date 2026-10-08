using UnityEngine;
using Game.GamePlay;
using NPBehave;
using System;

namespace Game.GamePlay 
{
    /// <summary>
    /// 行为树动作专属代理类，汇聚对 Entity 的各种快捷单帧操作与战术上下文桥接。
    /// </summary>
    public class TreeActionAgent : IDisposable
    {
        private MonsterEntity _owner;

        /// <summary>
        /// 实体战术上下文引用（行为树 Tasks 通过此属性声明策略与意图）
        /// </summary>
        public MonsterTacticalContext Context => _owner?.TacticalContext;

        public MonsterEntity Owner => _owner;

        // BTRunner 初始化时创建该类，传入自己所绑定的 Entity
        public TreeActionAgent(MonsterEntity owner)
        {
            _owner = owner;
            if (_owner != null && _owner.HitReactionComponent is MonsterHitReactionComponent hitModule)
            {
                hitModule.OnHitTimestampChanged += HandleHitTimestampChanged;
            }
        }

        public void Dispose()
        {
            if (_owner != null && _owner.HitReactionComponent is MonsterHitReactionComponent hitModule)
            {
                hitModule.OnHitTimestampChanged -= HandleHitTimestampChanged;
            }
        }

        private void HandleHitTimestampChanged()
        {
            if (_owner == null || _owner.BTRunner == null || _owner.BTRunner.RuntimeBlackboard == null) return;
            var hitData = _owner.DataModule?.Get<HitReactionRuntimeData>();
            if (hitData != null)
            {
                _owner.BTRunner.RuntimeBlackboard.Set(BBKeyMapper.GetString(BBKey.HitTriggerTimestamp), hitData.HitTriggerTimestamp);
            }
        }

        public void Init(Blackboard bb)
        {
        }

        /// <summary>
        /// 单帧尝试播放 Action。
        /// </summary>
        public bool SendCommand(ActionConfigAsset actionConfig, out long commandId)
        {
            commandId = 0;
            if (_owner == null || _owner.ActionController == null || actionConfig == null) return false;
            var command = CharacterCommandFactory.CreateDirectAssetCommand(actionConfig);
            commandId = command.Id;

            _owner.ActionController.OnInput(command);
            return true;
        }



        public CommandFate CheckCommandFate(long commandId)
        {
            return _owner.ActionController?.CheckCommandFate(commandId) ?? CommandFate.Dropped;
        }

        public float GetDistanceToTarget()
        {
            return _owner.TargetFinder?.GetDistanceToTarget() ?? -1f;
        }

        public float GetCombinedPhysicalRadius()
        {
            float monsterRadius = _owner?.MovementComponent?.CharacterRadius ?? 0.8f;
            if (monsterRadius <= 0f && _owner != null && _owner.TryGetComponent<CharacterController>(out var selfCc))
            {
                monsterRadius = selfCc.radius;
            }
            float targetRadius = 0.5f;
            var target = _owner?.TargetFinder?.GetTarget();
            if (target != null && target.TryGetComponent<CharacterController>(out var targetCc))
            {
                targetRadius = targetCc.radius;
            }
            return (monsterRadius > 0f ? monsterRadius : 0.8f) + targetRadius;
        }

        public ActionConfigAsset CurrentPlayingAction => _owner?.ActionController?.CurrentPlayingAction;
        public MonsterLocomotionConfig LocomotionConfig => (_owner?.Config as MonsterConfigAsset)?.locomotionConfig;



        public void ServiceUpdate(Blackboard bb)
        {
            var target = _owner?.TargetFinder?.GetTarget();
            bb[BBKeyMapper.GetString(BBKey.HasTarget)] = target != null;
            bb[BBKeyMapper.GetString(BBKey.DistanceToTarget)] = (target != null && _owner != null)
                ? Vector3.Distance(_owner.transform.position, target.position)
                : float.MaxValue;
            var beheaviorData = _owner?.DataModule?.Get<MonSterBehaviorRuntimeData>();
            float cd = beheaviorData?.AttackCooldownTimer ?? 0f;
            bb[BBKeyMapper.GetString(BBKey.AttackCooldownTimer)] = cd;
            bb[BBKeyMapper.GetString(BBKey.AttackIntervalTimer)] = cd;

            var hitData = _owner?.DataModule?.Get<HitReactionRuntimeData>();
            bb[BBKeyMapper.GetString(BBKey.InHitReaction)] = hitData != null && hitData.InHitReaction;
            bb[BBKeyMapper.GetString(BBKey.IsStunned)] = false;
            bb[BBKeyMapper.GetString(BBKey.IsInRange)] = Context?.IsInRange ?? false;
            bb[BBKeyMapper.GetString(BBKey.IsSelfControl)] = _owner?.IsSelfControl ?? false;
        }

        public bool IsPlayingAction(ActionConfigAsset actionConfig)
        {
            if (_owner == null || _owner.ActionController == null) return false;
            return _owner.ActionController.CurrentPlayingAction == actionConfig;
        }

        public void StartAttackCooldown(float cooldown)
        {
            var aiData = _owner?.DataModule?.Get<MonSterBehaviorRuntimeData>();
            aiData?.StartAttackCooldown(cooldown);
        }

        public bool IsInHitStun()
        {
            var hitData = _owner?.DataModule?.Get<HitReactionRuntimeData>();
            return hitData != null && hitData.InHitReaction;
        }
    }
}
