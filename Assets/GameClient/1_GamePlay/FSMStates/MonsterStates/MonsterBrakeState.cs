using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物急停刹车状态。
    /// 播放 RunEnd 动画表现急停惯性；动画结束后自决策切入 WalkState。
    /// 收到攻击指令或受击时允许瞬切打断。
    /// </summary>
    public class MonsterBrakeState : MonsterStateBase
    {
        private ActionConfigAsset _brakeAction;

        public override void OnEnter()
        {
            _brakeAction = LocoConfig?.RunEnd;
            if (_brakeAction != null)
            {
                SendCommand(_brakeAction);
            }
            else
            {
                Machine.ChangeState<MonsterWalkState>();
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (TryEnterHitStun()) return;
            if (TryEnterAttackFromContext()) return;

            // 刹车动作播放完毕，平滑切入走位
            if (Entity.ActionController != null && Entity.ActionController.CurrentPlayingAction != _brakeAction)
            {
                Machine.ChangeState<MonsterWalkState>();
            }
        }

        public override void OnExit()
        {
            _brakeAction = null;
        }
    }
}
