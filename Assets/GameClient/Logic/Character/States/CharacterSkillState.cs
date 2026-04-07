using Game.FSM;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public class CharacterSkillState : CharacterStateBase
    {
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
            if (Entity.RuntimeData != null)
            {
                Entity.RuntimeData.IsBasicAttackHold = false;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Entity?.ActionController != null &&
                Entity.ActionController.HasMovementCancelableWindow() &&
                Entity.InputProvider != null &&
                Entity.InputProvider.HasMovementInput())
            {
                Machine.ChangeState<CharacterGroundState>();
            }
        }

        public override void OnExit()
        {
        }
    }
}
