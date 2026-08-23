using Game.FSM;

namespace Game.Logic
{
    public class CharacterSkillState : CharacterStateBase
    {
        private IActionCommandHandler _inputHandler;
        public override IActionCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }
}
