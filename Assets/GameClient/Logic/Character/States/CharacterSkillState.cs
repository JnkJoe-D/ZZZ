using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public class CharacterSkillState : CharacterStateBase
    {
        private float _skillStartTime;
        public bool IsBasicAttackHold { get; private set; }
        private SkillRunner _currentRunner;

        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            Entity.RuntimeData.CurrentCommandContext = CommandContextType.Skill;
            Entity.RuntimeData.ClearDashContinuation();
            IsBasicAttackHold = false;
            PlayCurrentSkill();
        }

        private void PlayCurrentSkill()
        {
            _skillStartTime = Time.time;

            var skillConfig = Entity.RuntimeData.NextActionToCast;
            if (skillConfig == null)
            {
                return;
            }

            Debug.Log($"<color=#0FFFFF>[Combo] PlayCurrentSkill {skillConfig.Name}</color>");
            _currentRunner = Entity.ComboController.PlayPendingAction();

            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner.OnComplete += OnSkillEnd;
            }

            Entity.ActionPlayer.SetPlaySpeed(
                (skillConfig is SkillConfigAsset s &&
                 (s.Category == SkillCategory.LightAttack || s.Category == SkillCategory.DashAttack || s.Category == SkillCategory.HeavyAttack))
                    ? Entity.Config.AttackMultipier
                    : Entity.Config.SkillMultipier);

            Debug.Log($"<color=#E2243C>PlaySkill: {skillConfig.Name}</color>");
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnExit()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnSkillEnd;
                _currentRunner = null;
            }

            if (Machine.NextState is CharacterActionBackswingState)
            {
            }
            else
            {
                Entity.ActionPlayer.StopAction();
            }
        }

        private void OnSkillEnd()
        {
            Entity.Machine.ChangeState<CharacterGroundState>();
        }
    }
}
