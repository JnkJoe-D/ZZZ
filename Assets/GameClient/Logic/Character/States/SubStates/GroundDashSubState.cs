using UnityEngine;
using Game.Logic.Action.Combo;

namespace Game.Logic.Character.SubStates
{
    public class GroundDashSubState : GroundSubState
    {
        private enum DashStage
        {
            WaitInput,
            Starting,
            Looping,
            Turning180,
            Stopped
        }

        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        private DashStage _stage = DashStage.WaitInput;
        private SkillEditor.SkillRunner _currentRunner;
        private float _stateTime;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DashInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            _ctx.HostEntity.RuntimeData.CurrentCommandContext = CommandContextType.GroundDash;
            _stateTime = 0f;
            _stage = DashStage.WaitInput;
            _currentRunner = null;
            StartDash();
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
                _ctx.Blackboard.IsFromDash = true;
                ChangeState(_ctx.StopState);
                return;
            }

            _stateTime += deltaTime;

            if (_stage == DashStage.Looping)
            {
                Vector2 currentInput = provider.GetMovementDirection();
                Vector2 lastInput = provider.GetLastMovementDirection();

                if (currentInput.sqrMagnitude > 0.1f && lastInput.sqrMagnitude > 0.1f)
                {
                    if (Vector2.Angle(lastInput, currentInput) > 170f)
                    {
                        StartDashTurn180();
                        return;
                    }
                }
            }

            if (_stage != DashStage.Turning180)
            {
                Vector2 inputDir = provider.GetMovementDirection();
                _ctx.HostEntity.MovementController?.FaceTo(inputDir);
            }

            if (_ctx.HostEntity.Config != null)
            {
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.DashMultipier);
            }
        }

        private void StartDash()
        {
            var config = _ctx.HostEntity.Config;
            if (config != null && config.DashStartConfig != null)
            {
                _stage = DashStage.Starting;
                _currentRunner = _ctx.HostEntity.ActionController.PlayStateAction(StateActionType.GroundDashStart);
                HookOnComplete(HandleStartComplete);
            }
            else
            {
                PlayLoop();
            }
        }

        private void StartDashTurn180()
        {
            var config = _ctx.HostEntity.Config;
            if (config != null && config.DashTurnBackConfig != null)
            {
                _stage = DashStage.Turning180;
                CleanupRunner();
                _currentRunner = _ctx.HostEntity.ActionController.PlayStateAction(StateActionType.GroundDashTurnBack);
                HookOnComplete(HandleTurn180Complete);
            }
        }

        private void HandleStartComplete()
        {
            if (_stage == DashStage.Starting && _ctx.CurrentSubState == this)
            {
                PlayLoop();
            }
        }

        private void HandleTurn180Complete()
        {
            if (_stage == DashStage.Turning180 && _ctx.CurrentSubState == this)
            {
                PlayLoop();
            }
        }

        private void PlayLoop()
        {
            _stage = DashStage.Looping;
            if (_ctx.HostEntity.Config?.DashConfig != null)
            {
                _ctx.HostEntity.ActionController.PlayStateAction(StateActionType.GroundDashLoop);
            }
        }

        private void HookOnComplete(System.Action callback)
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= callback;
                _currentRunner.OnComplete += callback;
            }
        }

        private void CleanupRunner()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= HandleStartComplete;
                _currentRunner.OnComplete -= HandleTurn180Complete;
                _currentRunner = null;
            }
        }

        public override void OnExit()
        {
            CleanupRunner();
            _stage = DashStage.Stopped;
        }
    }
}
