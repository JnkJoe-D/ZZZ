using Game.FSM;

namespace Game.Logic.Character
{
    /// <summary>
    /// 玩家自身行为节点基类
    /// 封装快捷属性以便派生出各种子状态(Run, Attack, Dash)用
    /// </summary>
    public abstract class CharacterStateBase : IFSMState<CharacterEntity>
    {
        protected FSMSystem<CharacterEntity> Machine;
        protected CharacterEntity Entity => Machine.Owner;

        public virtual void OnInit(FSMSystem<CharacterEntity> fsm)
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
}
