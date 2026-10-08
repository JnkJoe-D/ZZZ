using System;

namespace Game.GamePlay
{
    /// <summary>
    /// 宏观动作领域基类（多态设计）。
    /// 取代原空心 FSM，承担移动转向阻尼、闪避无敌计时、连招生命周期、受击硬直衰减等真实业务逻辑。
    /// </summary>
    public abstract class ActionDomain : IDisposable
    {
        public abstract ActionDomainId DomainId { get; }

        public CharacterEntity Entity { get; private set; }
        public ActionDomainContextModule Context { get; private set; }
        public ActionConfigAsset CurrentAction { get; protected set; }

        public virtual void Initialize(CharacterEntity entity, ActionDomainContextModule context)
        {
            Entity = entity;
            Context = context;
        }

        /// <summary>
        /// 当从其他领域切入本领域时调用。
        /// </summary>
        public virtual void OnEnter(ActionConfigAsset newAction, ActionDomain previousDomain)
        {
            CurrentAction = newAction;
        }

        /// <summary>
        /// 当在相同领域内切换动作时调用（例如 Combat 内由 Attack01 切至 Attack02）。
        /// 避免频繁 OnExit -> OnEnter 导致的运行时状态被意外重置。
        /// </summary>
        public virtual void OnActionChangedWithinDomain(ActionConfigAsset previousAction, ActionConfigAsset newAction)
        {
            CurrentAction = newAction;
        }

        /// <summary>
        /// 受控自顶向下推进逻辑帧更新。
        /// </summary>
        public virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// 当脱离本领域切入其他领域时调用。
        /// </summary>
        public virtual void OnExit(ActionDomain nextDomain)
        {
            CurrentAction = null;
        }

        /// <summary>
        /// 当前领域是否接收外部指令（例如受击硬直时静默吞噬输入）。
        /// </summary>
        public virtual bool CanAcceptCommand(CharacterCommand command) => true;

        public virtual void Dispose() { }
    }
}
