using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public class CharacterEvadeState : CharacterStateBase
    {
        private SkillRunner _currentRunner;
        private SkillConfigAsset currentSkill;
        private bool isBackswingStarted;

        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;
        private bool isFrontEvade;

        public override bool CanEnter()
        {
            return Entity != null && Entity.RuntimeData != null && Entity.RuntimeData.CanEvade(Entity.Config);
        }

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            Entity.RuntimeData.CurrentCommandContext = CommandContextType.Evade;
            isBackswingStarted = false;
            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            Entity.RuntimeData.RecordEvade(Entity.Config);
            isBackswingStarted = false;

            var skillConfig = Entity.RuntimeData.NextActionToCast;
            if (skillConfig == null)
            {
                return;
            }

            _currentRunner = Entity.ActionController.PlayPendingAction();
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }

            isFrontEvade = false;
            if (Entity.Config.evadeFront != null)
            {
                foreach (var ev in Entity.Config.evadeFront)
                {
                    if (ev == skillConfig)
                    {
                        isFrontEvade = true;
                        break;
                    }
                }
            }

            Entity.RuntimeData.SetDashContinuationCandidate(isFrontEvade);
            Entity.ActionPlayer.SetPlaySpeed(Entity.Config.DodgeMultipier);

            currentSkill = skillConfig as SkillConfigAsset;
        }

        public override void OnUpdate(float deltaTime)
        {
            if (isBackswingStarted)
            {
                return;
            }

            if (!Entity.ActionPlayer.IsPlaying)
            {
                Machine.ChangeState<CharacterGroundState>();
                return;
            }

            if (!isFrontEvade)
            {
                return;
            }

            var provider = Entity.InputProvider;
            if (provider == null)
            {
                return;
            }

            Vector2 inputDir = provider.GetMovementDirection();
            Entity.MovementController?.FaceTo(inputDir, 5f);
        }

        public override void OnExit()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner = null;
            }

            if (Machine.NextState is not CharacterActionBackswingState)
            {
                Entity.RuntimeData.ClearDashContinuation();
            }

            if (Machine.NextState is CharacterActionBackswingState)
            {
            }
            else
            {
                Entity.ActionPlayer.StopAction();
            }

            currentSkill = null;
        }

        private void OnSkillEnd()
        {
            currentSkill = null;
            Entity.RuntimeData.ClearDashContinuation();
            Entity.Machine.ChangeState<CharacterGroundState>();
        }
    }
}
