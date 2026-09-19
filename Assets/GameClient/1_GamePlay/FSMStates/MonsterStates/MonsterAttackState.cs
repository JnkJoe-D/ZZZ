using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物攻击执行状态。
    /// OnEnter：从 Context 安全消费 PendingAttack 动作资产，瞬时锁定正向并向动作管线压入指令。
    /// OnUpdate：监控动作播放完毕后退回 IdleState。
    /// </summary>
    public class MonsterAttackState : MonsterStateBase
    {
        private ActionConfigAsset _executingAttack;

        public override void OnEnter()
        {
            _executingAttack = Context?.ConsumePendingAttack();
            if (_executingAttack == null)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            // 出刀瞬间瞬时锁定正向
            RotateTowardsTarget(0f);

            // 向实体动作管线压入指令
            SendCommand(_executingAttack);
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterHitStun()) return;

            // 动作播放完毕（或者被动作管线打断/融合），退回待机
            if (_executingAttack != null && Entity.ActionController != null)
            {
                if (Entity.ActionController.CurrentPlayingAction != _executingAttack)
                {
                    Machine.ChangeState<MonsterIdleState>();
                }
            }
        }

        public override void OnExit()
        {
            _executingAttack = null;
        }
    }
}
