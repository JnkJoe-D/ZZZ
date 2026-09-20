using Game.GamePlay;
using UnityEngine;

namespace Game.GamePlay
{
    public class GroundJogSubState : GroundSubState
    {
        private ActionRuntimeData _actionData;
        private IActionCommandHandler _handler;
        public override IActionCommandHandler InputHandler => _handler;

        private float _stateTime;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _actionData = _ctx.HostEntity.DataModule?.Get<ActionRuntimeData>();
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            if (_actionData != null) _actionData.Set(nameof(_actionData.IsShortMoveInput), true);
            _stateTime = 0f;
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            _stateTime += deltaTime;

            var config = _ctx.HostEntity.Config;
            if (config != null && _actionData != null)
            {
                _actionData.Set(nameof(_actionData.IsShortMoveInput), _stateTime <= config.InputConfig.MoveShortInputThreshold);
            }

            Vector2 inputDir = provider.GetMovementDirection();
            _ctx.HostEntity.MovementComponent?.FaceTo(inputDir);
        }

        public override void OnExit()
        {
            if (_ctx.HostEntity.Config is RoleConfigAsset roleConfig)
            {
                _ctx.Blackboard.IsShortJog = _stateTime <= roleConfig.InputConfig.MoveShortInputThreshold;
            }
        }
    }
}
