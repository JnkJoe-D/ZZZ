using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 地表子状态基类。
    /// </summary>
    public abstract class RoleSubState
    {
        protected RoleGroundState _ctx;

        public virtual void Initialize(RoleGroundState context)
        {
            _ctx = context;
        }

        public virtual IActionCommandHandler InputHandler => RoleStateBase.InputHandlerStatic;

        public virtual bool CanEnter() { return true; }
        public virtual bool CanExit() { return true; }

        public virtual void OnEnter() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnExit() { }
        
        /// <summary>
        /// 方便子状态请求父容器切换状态
        /// </summary>
        protected bool ChangeState(RoleSubState newState)
        {
            return _ctx.ChangeSubState(newState);
        }
    }
}
