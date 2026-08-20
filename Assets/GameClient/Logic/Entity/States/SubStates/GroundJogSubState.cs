using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public class GroundJogSubState : GroundSubState
    {
        private ActionRuntimeData _actionData;
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        private float _stateTime;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _actionData = _ctx.HostEntity.DataModule?.Get<ActionRuntimeData>();
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            if (_actionData != null) _actionData.IsShortMoveInput = true;
            _stateTime = 0f;

            // var config = _ctx.HostEntity.Config;
            // if (config != null)
            // {
            //     var startAction = config.JogStartConfig != null ? config.JogStartConfig : config.JogConfig;
            //     _ctx.HostEntity.ActionController.PlayAction(startAction);
            // }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            // if (!provider.HasMovementInput())
            // {
            //     _ctx.Blackboard.IsFromDash = false;
            //     ChangeState(_ctx.StopState);
            //     return;
            // }

            _stateTime += deltaTime;

            var config = _ctx.HostEntity.Config as RoleConfigAsset;
            if (config != null && _actionData != null)
            {
                _actionData.IsShortMoveInput = _stateTime <= config.JogShortInputThreshold;
            }

            Vector2 inputDir = provider.GetMovementDirection();
            _ctx.HostEntity.CharacterMotor?.FaceTo(inputDir);
        }

        public override void OnExit()
        {
            if (_ctx.HostEntity.Config is RoleConfigAsset roleConfig)
            {
                _ctx.Blackboard.IsShortJog = _stateTime <= roleConfig.JogShortInputThreshold;
            }
        }
    }
}
