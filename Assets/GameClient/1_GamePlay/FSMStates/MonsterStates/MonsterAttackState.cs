using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物攻击执行状态。
    /// OnEnter：从 Context 安全消费 PendingAttack 动作资产，瞬时锁定正向并向动作管线压入指令。
    /// OnUpdate：两阶段生命周期跟踪，等待动作真正启动播放，并在动作播放完毕后退回 IdleState。
    /// </summary>
    public class MonsterAttackState : MonsterStateBase
    {
        private ActionConfigAsset _executingAttack;
        private bool _hasStarted = false;

        public override void OnEnter()
        {
            _hasStarted = false;
            _executingAttack = Context?.ConsumePendingAttack();
            if (_executingAttack == null)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            // 出刀瞬间瞬时锁定正向
            RotateTowardsTarget(0f);

            // 统一通过 OnInput 压指令驱动出刀动作
            if (!SendCommand(_executingAttack))
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterStun()) return;
            if (TryEnterHitStun()) return;

            if (_executingAttack == null || Entity.ActionController == null)
            {
                Machine.ChangeState<MonsterIdleState>();
                return;
            }

            // 阶段 1：确认动作系统真正开始播放该攻击动作
            if (!_hasStarted)
            {
                if (Entity.ActionController.CurrentPlayingAction == _executingAttack)
                {
                    _hasStarted = true;
                }
                else
                {
                    Machine.ChangeState<MonsterIdleState>();
                    return;
                }
            }
            // 阶段 2：已确认开始播放后，动作播放结束退回待机
            else
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
            _hasStarted = false;
        }
    }
}
