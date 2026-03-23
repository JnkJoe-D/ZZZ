using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundJogSubState : GroundSubState
    {
        private enum JogStage
        {
            WaitInput,
            Starting,
            Looping,
            Stopped
        }

        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        private JogStage _stage = JogStage.WaitInput;
        private SkillEditor.SkillRunner _currentRunner;
        private float _stateTime;
        private const float InputBufferTime = 0.08f;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            _ctx.HostEntity.RuntimeData.CurrentCommandContext = CommandContextType.GroundJog;
            _stateTime = 0f;
            _stage = JogStage.WaitInput;
            _currentRunner = null;
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null)
            {
                return;
            }

            if (!provider.HasMovementInput())
            {
                CleanupRunner();
                _ctx.Blackboard.IsFromDash = false;
                ChangeState(_ctx.StopState);
                return;
            }

            _stateTime += deltaTime;
            if (_stage == JogStage.WaitInput && _stateTime >= InputBufferTime)
            {
                StartJog();
            }

            Vector2 inputDir = provider.GetMovementDirection();
            _ctx.HostEntity.MovementController?.FaceTo(inputDir);

            if (_ctx.HostEntity.Config != null)
            {
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.JogMultipier);
            }
        }

        private void StartJog()
        {
            var config = _ctx.HostEntity.Config;
            if (config != null && config.JogStartConfig != null)
            {
                _stage = JogStage.Starting;
                _currentRunner = _ctx.HostEntity.ComboController.PlayStateAction(StateActionType.GroundJogStart);
                if (_currentRunner != null)
                {
                    _currentRunner.OnComplete -= HandleStartComplete;
                    _currentRunner.OnComplete += HandleStartComplete;
                }
                else
                {
                    PlayLoop();
                }
            }
            else
            {
                PlayLoop();
            }
        }

        private void HandleStartComplete()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= HandleStartComplete;
                _currentRunner = null;
            }

            if (_stage == JogStage.Starting && _ctx.CurrentSubState == this)
            {
                PlayLoop();
            }
        }

        private void PlayLoop()
        {
            _stage = JogStage.Looping;
            var config = _ctx.HostEntity.Config;
            if (config != null && config.JogConfig != null)
            {
                _ctx.HostEntity.ComboController.PlayStateAction(StateActionType.GroundJogLoop);
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(config.JogMultipier);
            }
        }

        private void CleanupRunner()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= HandleStartComplete;
                _currentRunner = null;
            }
        }

        public override void OnExit()
        {
            CleanupRunner();
            _stage = JogStage.Stopped;
            if (_ctx.HostEntity.Config != null)
            {
                _ctx.Blackboard.IsShortJog = _stateTime <= _ctx.HostEntity.Config.JogShortInputThreshold;
            }
        }
    }
}
