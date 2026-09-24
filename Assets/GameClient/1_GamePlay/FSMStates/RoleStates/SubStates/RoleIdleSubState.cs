using Game.GamePlay;

namespace Game.GamePlay
{
    public class RoleIdleSubState : RoleSubState
    {
        private IActionCommandHandler _handler;
        public override IActionCommandHandler InputHandler => _handler;

        public override void Initialize(RoleGroundState context)
        {
            base.Initialize(context);
            _handler = new DefaultInputCommandHandler(context.HostEntity);
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
