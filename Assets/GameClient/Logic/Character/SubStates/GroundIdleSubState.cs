using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundIdleSubState : GroundSubState
    {
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            if (_ctx.HostEntity.Config.IdleConfig != null)
            {
                _ctx.HostEntity.ActionPlayer.PlayAction(_ctx.HostEntity.Config.IdleConfig);
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(1.0f);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return;

            // 只要推了摇杆，马上要破冰
            if (provider.HasMovementInput())
            {
                // 如果恰巧这刻他还死死按着 Shift，直接进入爆发冲刺
                if (provider.GetActionState(Game.Input.InputActionType.Dash))
                {
                    ChangeState(_ctx.DashState);
                }
                else
                {
                    // 否则自然转入慢跑
                    ChangeState(_ctx.JogState);
                }
            }
        }
    }
}
