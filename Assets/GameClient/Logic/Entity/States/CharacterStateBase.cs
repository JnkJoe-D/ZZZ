using Game.FSM;

namespace Game.Logic
{
    /// <summary>
    /// 玩家自身行为节点基类
    /// 封装快捷属性以便派生出各种子状态(Run, Attack, Dash)用
    /// </summary>
    public abstract class CharacterStateBase : IFSMState<RoleEntity>
    {
        protected FSMSystem<RoleEntity> Machine;
        protected RoleEntity Entity => Machine.Owner;

        // --- 指令路由 ---
        public static readonly IInputCommandHandler NullInputHandler = new NullInputCommandHandler();
        public static IInputCommandHandler InputHandlerStatic => NullInputHandler;
        public virtual IInputCommandHandler InputHandler => NullInputHandler;

        public virtual void OnInit(FSMSystem<RoleEntity> fsm)
        {
            Machine = fsm;
        }

        public virtual bool CanEnter() { return true; }
        public virtual bool CanExit() { return true; }

        public virtual void OnEnter() { }

        public virtual void OnUpdate(float deltaTime) { }

        public virtual void OnFixedUpdate(float fixedDeltaTime) { }

        public virtual void OnExit() { }

        public virtual void OnDestroy() { }
    }

    public sealed class CharacterSwitchState : CharacterStateBase
    {
        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<RoleEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new ComboInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {

        }
    }
}
