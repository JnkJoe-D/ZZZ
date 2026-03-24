using Game.Logic.Action.Combo;

namespace Game.Logic.Character.SubStates
{
    public class GroundStopSubState : GroundSubState
    {
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        private SkillEditor.SkillRunner _currentRunner;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        public override void OnEnter()
        {
            _ctx.HostEntity.RuntimeData.CurrentCommandContext = CommandContextType.GroundStop;
            
            StateActionType actionType = StateActionType.GroundJogStop;
            if (_ctx.Blackboard.IsFromDash)
            {
                actionType = StateActionType.GroundDashStop;
            }
            else if (_ctx.Blackboard.IsShortJog)
            {
                actionType = StateActionType.GroundJogStartEnd;
            }

            if (_ctx.HostEntity.Config != null)
            {
                _currentRunner = _ctx.HostEntity.ActionController.PlayStateAction(actionType);
                if (_currentRunner != null)
                {
                    _currentRunner.OnComplete -= OnStopAnimFinished;
                    _currentRunner.OnComplete += OnStopAnimFinished;
                }
                else
                {
                    OnStopAnimFinished();
                }
            }
            else
            {
                OnStopAnimFinished();
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
                if (_currentRunner != null)
                {
                    _currentRunner.OnComplete -= OnStopAnimFinished;
                    _currentRunner = null;
                }

                ChangeState(_ctx.JogState);
            }
        }

        private void OnStopAnimFinished()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnStopAnimFinished;
                _currentRunner = null;
            }

            if (_ctx.CurrentSubState == this)
            {
                ChangeState(_ctx.IdleState);
            }
        }
    }
}
