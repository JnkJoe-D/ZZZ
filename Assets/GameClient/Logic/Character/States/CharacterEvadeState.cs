using Game.FSM;
using Game.Input;
using Game.Logic.Action.Config;
using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    public class CharacterEvadeState : CharacterStateBase
    {
        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

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
            Entity.RuntimeData.RecordEvade(Entity.Config);
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = Entity.InputProvider;

            if (Entity?.ActionController != null &&
                Entity.ActionController.HasMovementCancelableWindow() &&
                provider != null &&
                provider.HasMovementInput())
            {
                Machine.ChangeState<CharacterGroundState>();
                return;
            }
        }

        public override void OnExit()
        {
        }
    }
}
