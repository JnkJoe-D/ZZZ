namespace Game.Framework
{
    /// <summary>
    /// 标准化 FSM 状态接口
    /// 定义了状态的基本生命周期
    /// </summary>
    public interface IFSMState<T>
    {
        /// <summary>被注册进状态机时的初始化</summary>
        void OnInit(FSMSystem<T> fsm);
        
        /// <summary>能否退出当前状态</summary>
        bool CanExit();
        
        /// <summary>能否进入该状态</summary>
        bool CanEnter();

        /// <summary>进入状态</summary>
        void OnEnter();
        
        /// <summary>逻辑帧更新</summary>
        void OnUpdate(float deltaTime);
        
        /// <summary>物理帧更新</summary>
        void OnFixedUpdate(float fixedDeltaTime);
        
        /// <summary>退出状态</summary>
        void OnExit();
        
        /// <summary>状态机销毁时的清理操作</summary>
        void OnDestroy();
    }
}
