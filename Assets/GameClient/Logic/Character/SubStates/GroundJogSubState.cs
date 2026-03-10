using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundJogSubState : GroundSubState
    {
        private float _stateTime;
        private bool _hasPlayedAnim;
        private const float INPUT_BUFFER_TIME = 0.00f; // 短输入防抖阈值

        public override void OnEnter()
        {
            _stateTime = 0f;
            _hasPlayedAnim = false;
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return;

            // 1. 玩家停止推摇杆：触发由于慢跑产生的物理惯性刹车
            if (!provider.HasMovementInput())
            {
                _ctx.Blackboard.IsFromDash = false;
                ChangeState(_ctx.StopState);
                return;
            }

            // // 2. 玩家在慢跑时扣紧了 Shift，进入冲刺猛跑
            // if (provider.GetActionState(Game.Input.InputActionType.Dash))
            // {
            //     ChangeState(_ctx.DashState);
            //     return;
            // }

            _stateTime += deltaTime;
            
            // 短输入防抖：如果按压时间超过了防抖阈值，才真正开始播放起步大动作
            if (!_hasPlayedAnim && _stateTime >= INPUT_BUFFER_TIME)
            {
                if (_ctx.HostEntity.Config.JogConfig != null)
                {
                    _ctx.HostEntity.ActionPlayer.PlayAction(_ctx.HostEntity.Config.JogConfig);
                    _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.JogMultipier);
                }
                _hasPlayedAnim = true;
            }

            // 3. 执行推移
            Vector2 inputDir = provider.GetMovementDirection();
            Vector3 worldDir = _ctx.CalculateWorldDirection(inputDir);
            
            _ctx.HostEntity.MovementController?.Move(worldDir * _ctx.JogSpeed * deltaTime);
            _ctx.HostEntity.MovementController?.FaceTo(worldDir);
            
            _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.JogMultipier);
        }
    }
}
