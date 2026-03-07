using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundDashSubState : GroundSubState
    {
        private float _stateTime;
        private bool _hasPlayedAnim;
        private bool _isDashStable = false;
        private const float INPUT_BUFFER_TIME = 0.08f; // 短输入防抖阈值

        public override void OnEnter()
        {
            _stateTime = 0f;
            _hasPlayedAnim = false;
            _isDashStable = false; // 每次新切入 Dash，稳定锁防抖重新复位
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return;

            // 1. 松摇杆触发物理急停
            if (!provider.HasMovementInput())
            {
                // if(_isDashStable)
                // {
                //     _ctx.StopState.SetBrakeParams(isFromDash: true, isDashStable: _isDashStable);
                //     ChangeState(_ctx.StopState);
                // }
                // else
                // {
                //     ChangeState(_ctx.IdleState);
                // }
                _ctx.Blackboard.IsFromDash = true;
                _ctx.Blackboard.IsDashStable = _isDashStable;
                ChangeState(_ctx.StopState);
                return;
            }

            // // 2. 长按 Shift 折断：松手了但还在滑推摇杆，降级落回 Jog
            // if (!provider.GetActionState(Game.Input.InputActionType.Dash))
            // {
            //     ChangeState(_ctx.JogState);
            //     return;
            // }

            _stateTime += deltaTime;
            
            // 短输入防抖：避免冲刺点按抽搐
            if (!_hasPlayedAnim && _stateTime >= INPUT_BUFFER_TIME)
            {
                _hasPlayedAnim = true;
                _isDashStable = true;
                var startClip = _ctx.HostEntity.CurrentAnimSet?.DashStart.clip;
                if (startClip != null)
                {
                    _ctx.HostEntity.AnimController?.PlayAnim(
                        startClip,
                        _ctx.HostEntity.CurrentAnimSet.Dash.fadeDuration,
                        forceResetTime: true
                    );

                    _ctx.HostEntity.AnimController?.AddEventToCurrentAnim(
                        Mathf.Max(0, startClip.length - _ctx.HostEntity.CurrentAnimSet.Dash.fadeDuration), 
                        () => 
                        {
                            _ctx.HostEntity.AnimController?.PlayAnim(
                                _ctx.HostEntity.CurrentAnimSet.Dash.clip, 
                                _ctx.HostEntity.CurrentAnimSet.Dash.fadeDuration
                            );
                        }
                    );
                }
                else if (_ctx.HostEntity.CurrentAnimSet?.Dash.clip != null)
                {
                    _ctx.HostEntity.AnimController?.PlayAnim(
                        _ctx.HostEntity.CurrentAnimSet.Dash.clip, 
                        _ctx.HostEntity.CurrentAnimSet.Dash.fadeDuration
                    );
                }
            }

            // 3. 强力冲刺移动
            Vector2 inputDir = provider.GetMovementDirection();
            Vector3 worldDir = _ctx.CalculateWorldDirection(inputDir);
            
            _ctx.HostEntity.MovementController?.Move(worldDir * _ctx.DashSpeed * deltaTime);
            _ctx.HostEntity.MovementController?.FaceTo(worldDir);
            _ctx.HostEntity.AnimController?.SetSpeed(0, _ctx.HostEntity.Config.DashMultipier);
        }
    }
}
