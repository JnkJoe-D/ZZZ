using UnityEngine;
using Game.Logic;
using NPBehave;
using System;

namespace Game.Logic.AI.BehaviorTree
{
    /// <summary>
    /// 行为树动作专属代理类，汇聚对 Entity 的各种快捷单帧操作
    /// </summary>
    public class TreeActionAgent
    {
        private MonsterEntity _owner;

        // BTRunner 初始化时创建该类，传入自己所绑定的 Entity
        public TreeActionAgent(MonsterEntity owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 单帧尝试播放 Action。
        /// </summary>
        public bool TryPlayAction(ActionConfigAsset actionConfig, out long commandId)
        {
            commandId = 0;
            if (_owner == null || _owner.ActionController == null) return false;
            
            var command = CharacterCommandFactory.Create(InputCommand.AIAction, CommandPhase.Started, actionConfig);
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

        public void ServiceUpdate(Blackboard bb)
        {
            var target = _owner.TargetFinder?.GetTarget();
            bb["HasTarget"] = target != null;
            bb["DistanceToTarget"] = _owner.TargetFinder?.GetDistanceToTarget() ?? float.MaxValue;
            bb["CurrentAIState"] = _owner.DataModule?.Get<MonSterBehaviorRuntimeData>()?.CurrentState ?? MonsterAIState.Idle;
            bb["AttackCooldownTimer"] = _owner.DataModule?.Get<MonSterBehaviorRuntimeData>()?.AttackCooldownTimer ?? 0f;
        }
        public bool IsPlayingAction(ActionConfigAsset actionConfig)
        {
            if (_owner == null || _owner.ActionController == null) return false;
            return _owner.ActionController.CurrentPlayingAction == actionConfig;
        }

        public bool CheckAIState(MonsterAIState targetState)
        {
            var aiData = _owner?.DataModule?.Get<MonSterBehaviorRuntimeData>();
            if (aiData != null)
            {
                return aiData.CurrentState == targetState;
            }
            return false;
        }

        public void ChangeAIState(MonsterAIState targetState)
        {
            var aiData = _owner?.DataModule?.Get<MonSterBehaviorRuntimeData>();
            aiData?.ChangeState(targetState);
        }

        public void StartAttackCooldown(float cooldown)
        {
            var aiData = _owner?.DataModule?.Get<MonSterBehaviorRuntimeData>();
            aiData?.StartAttackCooldown(cooldown);
        }

        public void EvaluateState(float chaseDistance)
        {
            if (_owner == null || _owner.DataModule == null) return;
            var aiData = _owner.DataModule.Get<MonSterBehaviorRuntimeData>();
            var hitData = _owner.DataModule.Get<HitReactionRuntimeData>();

            if (aiData == null) return;

            if (hitData != null && hitData.CurrentHitStunDuration > 0)
            {
                aiData.ChangeState(MonsterAIState.HitStun);
                return;
            }

            var target = _owner.TargetFinder?.GetTarget();
            if (target == null)
            {
                aiData.ChangeState(MonsterAIState.Idle);
                return;
            }

            float distance = _owner.TargetFinder?.GetDistanceToTarget() ?? float.MaxValue;
            if (distance > chaseDistance)
            {
                aiData.ChangeState(MonsterAIState.Chase);
            }
            else
            {
                // TODO: 结合怪物身上的技能CD做更详细判断，当前简化为近身即周旋
                aiData.ChangeState(MonsterAIState.Adjust);
            }
        }
    }
}
