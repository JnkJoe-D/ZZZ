using Game.FSM;
using Game.Input;
using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public class CharacterEvadeState : CharacterStateBase
    {
        private IActionCommandHandler _inputHandler;
        private EvadeRuntimeData _evadeData;
        public override IActionCommandHandler InputHandler => _inputHandler;

        public override bool CanEnter()
        {
            return Entity != null && _evadeData != null && _evadeData.CanEvade(Entity.Config);
        }

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _evadeData = Entity.DataModule?.Get<EvadeRuntimeData>();
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _evadeData?.RecordEvade(Entity.Config);
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnExit()
        {
        }
    }
}
