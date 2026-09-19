using UnityEngine;
using Game.GamePlay;

namespace Game.GamePlay
{
    public class GroundDashSubState : GroundSubState
    {
        private IActionCommandHandler _handler;
        public override IActionCommandHandler InputHandler => _handler;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DashInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            bool hasMovementInput = provider.HasMovementInput();

            Vector2 currentInput = provider.GetMovementDirection();
            Vector2 lastInput = provider.GetLastMovementDirection();

            _ctx.HostEntity.MovementComponent?.FaceTo(currentInput);
        }

        public override void OnExit()
        {
        }
    }
}
