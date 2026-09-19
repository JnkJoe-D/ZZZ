namespace Game.GamePlay
{
    /// <summary>
    /// 角色表现层契约。
    /// 供玩法层、换人管线及小队管理器调度角色网格显隐、碰撞体开闭及相机表现，
    /// 彻底阻断 GamePlay 对具体 Presentation 组件的直接依赖。
    /// </summary>
    public interface IRolePresentation : IEntityComponent
    {
        bool IsPresentationVisible { get; }
        ICameraController CameraController { get; }
        CameraPointBinder CameraPointBinder { get; }
        IVisualOffsetPresenter VisualOffsetPresenter { get; }
        void SetPresentationVisible(bool visible);
        void SetColliderActive(bool active);
        void SetCameraActive(bool active, bool enableInput = true);
        void CachePresentationState();
    }

    /// <summary>
    /// 角色表现层装配注册表（依赖倒置防腐契约）。
    /// 位于纯玩法层 (1_GamePlay)，允许表现层 (2_Presentation) 在启动时注册实体表现装配工厂，
    /// 实现玩法实体与表现组件的解耦装配，严禁玩法层反向依赖表现层具体类。
    /// </summary>
    public static class RolePresentationRegistry
    {
        private static System.Func<UnityEngine.GameObject, RoleEntity, IRolePresentation> _binder;

        /// <summary>
        /// 注册表现层组件装配工厂。由表现层在运行时初始化时显式或自动调用。
        /// </summary>
        public static void Register(System.Func<UnityEngine.GameObject, RoleEntity, IRolePresentation> binder)
        {
            _binder = binder;
        }

        /// <summary>
        /// 为给定的角色 GameObject 与 RoleEntity 实体装配表现层契约组件。
        /// </summary>
        public static IRolePresentation Bind(UnityEngine.GameObject go, RoleEntity entity)
        {
            return _binder?.Invoke(go, entity);
        }

        /// <summary>
        /// 清理注册状态（用于单元测试或域重载安全）。
        /// </summary>
        public static void Clear()
        {
            _binder = null;
        }
    }
}
