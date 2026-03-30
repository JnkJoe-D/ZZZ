using Game.FSM;

namespace Game.Logic.Character
{
    /// <summary>
    /// 动作后摇状态。
    /// 当前动作的可执行窗口已经结束，命令所有权切换到 Backswing 上下文，
    /// 由上下文路由决定接下来的攻击、技能、闪避入口。
    /// </summary>
    public class CharacterActionBackswingState : CharacterStateBase
    {
        private IInputCommandHandler _inputHandler;
        public override IInputCommandHandler InputHandler => _inputHandler;

        public override void OnInit(FSMSystem<CharacterEntity> fsm)
        {
            base.OnInit(fsm);
            _inputHandler = new DefaultInputCommandHandler(Entity);
        }

        public override void OnEnter()
        {
            Entity.RuntimeData.CurrentCommandContext = CommandContextType.Backswing;

            if (Machine.PreviousState is not CharacterEvadeState)
            {
                Entity.RuntimeData.ClearDashContinuation();
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            if (Entity.InputProvider != null && Entity.InputProvider.HasMovementInput())
            {
                Machine.ChangeState<CharacterGroundState>();
            }
        }

        public override void OnExit()
        {
        }
    }
}
