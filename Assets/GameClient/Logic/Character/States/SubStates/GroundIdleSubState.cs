using Game.Logic.Action.Combo;

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
            _ctx.HostEntity.RuntimeData.CurrentCommandContext = CommandContextType.GroundIdle;
            if (_ctx.HostEntity.Config.IdleConfig != null)
            {
                _ctx.HostEntity.ComboController.PlayStateAction(StateActionType.GroundIdle);
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(1.0f);
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            if (provider.HasMovementInput())
            {
                ChangeState(_ctx.JogState);
            }
        }
    }
}
