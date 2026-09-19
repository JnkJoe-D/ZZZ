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

            // 若播放的是非移动动作（如攻击、受击等），清理当前移动意图缓存
            if (!IsLocomotionAction(actionConfig))
            {
                _currentLocomotionIntent = MonsterLocomotionIntent.None;
            }

            return true;
        }

        private bool IsLocomotionAction(ActionConfigAsset action)
        {
            var config = LocomotionConfig;
            if (config == null || action == null) return false;
            return action == config.RunStart || action == config.RunLoop || action == config.RunEnd ||
                   action == config.WalkF || action == config.WalkB || action == config.WalkL || action == config.WalkR;
        }

        public CommandFate CheckCommandFate(long commandId)
        {
            return _owner.ActionController?.CheckCommandFate(commandId) ?? CommandFate.Dropped;
        }

        public float GetDistanceToTarget()
        {
            return _owner.TargetFinder?.GetDistanceToTarget() ?? -1f;
        }

        public ActionConfigAsset CurrentPlayingAction => _owner?.ActionController?.CurrentPlayingAction;
        public MonsterLocomotionConfig LocomotionConfig => (_owner?.Config as MonsterConfigAsset)?.locomotionConfig;

        private MonsterLocomotionIntent _currentLocomotionIntent = MonsterLocomotionIntent.None;
        public MonsterLocomotionIntent CurrentLocomotionIntent => _currentLocomotionIntent;

        /// <summary>
        /// 清理当前移动意图（例如被出刀攻击、受击硬直打断时调用）。
        /// </summary>
        public void ClearLocomotionIntent()
        {
            _currentLocomotionIntent = MonsterLocomotionIntent.None;
        }

        /// <summary>
        /// 检查当前动作控制器是否正在播放该移动意图对应的动作。
        /// </summary>
        [Obsolete("微观步态已下放至 MonsterStateBase 状态机自决策，此方法保留仅供向下兼容")]
        public bool IsPlayingLocomotionIntent(MonsterLocomotionIntent intent)
        {
            var config = LocomotionConfig;
            if (config == null) return false;
            var current = CurrentPlayingAction;
            if (current == null) return false;

            return intent switch
            {
                MonsterLocomotionIntent.Run => current == config.RunStart || current == config.RunLoop,
                MonsterLocomotionIntent.StrafeForward => current == config.WalkF,
                MonsterLocomotionIntent.StrafeBackward => current == config.WalkB,
                MonsterLocomotionIntent.StrafeLeft => current == config.WalkL,
                MonsterLocomotionIntent.StrafeRight => current == config.WalkR,
                MonsterLocomotionIntent.Stop => current == config.RunEnd,
                _ => false
            };
        }

        /// <summary>
        /// 核心决策接口（已由 MonsterTacticalContext 与 FSM 接管，保留供旧节点过渡）
        /// </summary>
        [Obsolete("请使用 Context.Strategy / TargetRadius 替代直接设置动作意图")]
        public bool SetLocomotionIntent(MonsterLocomotionIntent newIntent)
        {
            if (newIntent == MonsterLocomotionIntent.None)
            {
                _currentLocomotionIntent = MonsterLocomotionIntent.None;
                return true;
            }

            if (_currentLocomotionIntent == newIntent)
            {
                if (IsPlayingLocomotionIntent(newIntent))
                {
                    return true;
                }
            }

            _currentLocomotionIntent = newIntent;
            long cmdId;
            return newIntent switch
            {
                MonsterLocomotionIntent.Run => PlayRunInternal(out cmdId),
                MonsterLocomotionIntent.StrafeForward => PlayStrafeInternal(StrafeDirection.Forward, out cmdId),
                MonsterLocomotionIntent.StrafeBackward => PlayStrafeInternal(StrafeDirection.Backward, out cmdId),
                MonsterLocomotionIntent.StrafeLeft => PlayStrafeInternal(StrafeDirection.Left, out cmdId),
                MonsterLocomotionIntent.StrafeRight => PlayStrafeInternal(StrafeDirection.Right, out cmdId),
                MonsterLocomotionIntent.Stop => PlayStopRunInternal(out cmdId),
                _ => true
            };
        }

        private bool PlayRunInternal(out long commandId)
        {
            commandId = 0;
            var config = LocomotionConfig;
            if (config == null) return false;

            ActionConfigAsset runAction = config.RunStart != null ? config.RunStart : config.RunLoop;
            if (runAction == null) return false;

            return SendCommand(runAction, out commandId);
        }

        private bool PlayStrafeInternal(StrafeDirection dir, out long commandId)
        {
            commandId = 0;
            var config = LocomotionConfig;
            if (config == null) return false;

            ActionConfigAsset walkAction = dir switch
            {
                StrafeDirection.Forward => config.WalkF,
                StrafeDirection.Backward => config.WalkB,
                StrafeDirection.Left => config.WalkL,
                StrafeDirection.Right => config.WalkR,
                _ => config.WalkF
            };

            if (walkAction == null) return false;

            return SendCommand(walkAction, out commandId);
        }

        private bool PlayStopRunInternal(out long commandId)
        {
            commandId = 0;
            var config = LocomotionConfig;
            if (config == null || config.RunEnd == null) return false;

            return SendCommand(config.RunEnd, out commandId);
        }

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
