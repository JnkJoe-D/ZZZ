using UnityEngine;
using Game.Logic;

namespace Game.Logic
{
    public class GroundDashSubState : GroundSubState
    {
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

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

            // if (!hasMovementInput)
            // {
            //     _ctx.Blackboard.IsFromDash = true;
            //     ChangeState(_ctx.StopState);
            //     return;
            // }

            Vector2 currentInput = provider.GetMovementDirection();
            Vector2 lastInput = provider.GetLastMovementDirection();

            _ctx.HostEntity.CharacterMotor?.FaceTo(currentInput);
        }

        public override void OnExit()
        {
        }
    }
}
