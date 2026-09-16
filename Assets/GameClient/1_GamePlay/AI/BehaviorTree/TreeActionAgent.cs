using UnityEngine;
using Game.GamePlay;
using NPBehave;
using System;

namespace Game.GamePlay 
{
    /// <summary>
    /// 行为树动作专属代理类，汇聚对 Entity 的各种快捷单帧操作
    /// </summary>
    public class TreeActionAgent : IDisposable
    {
        private MonsterEntity _owner;

        // BTRunner 初始化时创建该类，传入自己所绑定的 Entity
        public TreeActionAgent(MonsterEntity owner)
        {
            _owner = owner;
            if (_owner != null && _owner.HitReactionModule is MonsterHitReactionModule hitModule)
            {
                hitModule.OnHitTimestampChanged += HandleHitTimestampChanged;
            }
        }

        public void Dispose()
        {
            if (_owner != null && _owner.HitReactionModule is MonsterHitReactionModule hitModule)
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
                _owner.BTRunner.RuntimeBlackboard.Set("HitTriggerTimestamp", hitData.HitTriggerTimestamp);
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
        /// 尤其针对 Run：无论是起步 RunStart 还是自动过渡后的 RunLoop，均视为正在奔跑，禁止重头重播！
        /// </summary>
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
        /// 核心决策接口：仅当移动决策改变（或动作异常脱落）时才下发动作指令。
        /// </summary>
        public bool SetLocomotionIntent(MonsterLocomotionIntent newIntent)
        {
            if (newIntent == MonsterLocomotionIntent.None)
            {
                _currentLocomotionIntent = MonsterLocomotionIntent.None;
                return true;
            }

            // 1. 决策未改变：检查底层当前是否仍处于对应动作，若是直接维持，绝不重复发送指令！
            if (_currentLocomotionIntent == newIntent)
            {
                if (IsPlayingLocomotionIntent(newIntent))
                {
                    return true;
                }
            }

            // 2. 决策发生改变，执行对应动作切换
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

            // 优先播 RunStart，若无则播 RunLoop
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
            var target = _owner.TargetFinder?.GetTarget();
            bb["HasTarget"] = target != null;
            bb["DistanceToTarget"] = (target != null && _owner != null)
                ? Vector3.Distance(_owner.transform.position, target.position)
                : float.MaxValue;
            var beheaviorData = _owner.DataModule?.Get<MonSterBehaviorRuntimeData>();
            float cd = beheaviorData?.AttackCooldownTimer ?? 0f;
            bb["AttackCooldownTimer"] = cd;
            bb["AttackIntervalTimer"] = cd;

            var hitData = _owner.DataModule?.Get<HitReactionRuntimeData>();
            bb["InHitReaction"] = hitData != null && hitData.InHitReaction;
            bb["IsStunned"] = false;
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
