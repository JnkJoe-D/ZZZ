using Game.Framework;

namespace Game.GamePlay
{
    public class RoleSkillState : RoleStateBase
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
