using Game.FSM;
using Game.Input;
using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public class CharacterEvadeState : CharacterStateBase
    {
        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override bool CanEnter()
        {
            return Entity != null && Entity.EvadeData != null && Entity.EvadeData.CanEvade(Entity.Config);
        }

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Entity.EvadeData.RecordEvade(Entity.Config);
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnExit()
        {
        }
    }
}
