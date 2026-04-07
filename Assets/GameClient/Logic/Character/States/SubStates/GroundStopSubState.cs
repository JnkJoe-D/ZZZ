using Game.Logic.Action.Combo;

namespace Game.Logic.Character.SubStates
{
    public class GroundStopSubState : GroundSubState
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
            _ctx.HostEntity.RuntimeData.CurrentCommandContext = CommandContextType.GroundStop;
            
            // if (_ctx.HostEntity.Config != null)
            // {
            //     var config = _ctx.HostEntity.Config;
            //     var action = config.JogStopConfig;

            //     if (_ctx.Blackboard.IsFromDash)
            //     {
            //         action = config.DashStopConfig ?? config.JogStopConfig;
            //     }
            //     else if (_ctx.Blackboard.IsShortJog)
            //     {
            //         action = config.JogStartEndConfig ?? config.JogStopConfig;
            //     }

            //     if (action != null)
            //     {
            //         _ctx.HostEntity.ActionController.PlayAction(action);
            //     }
            // }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            // if (provider.HasMovementInput())
            // {
            //     ChangeState(_ctx.JogState);
            // }
        }
    }
}
