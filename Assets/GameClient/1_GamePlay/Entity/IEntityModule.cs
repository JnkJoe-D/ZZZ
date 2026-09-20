namespace Game.GamePlay
{
    /// <summary>
    /// 所有纯 C# 领域业务模块必须实现的统一契约。
    /// 严禁继承 MonoBehaviour！
    /// </summary>
    public interface IEntityModule
    {
        /// <summary> 模块初始化，注入实体依赖与初始配置 </summary>
        void Initialize(CharacterEntity owner);

        /// <summary> 受控 60Hz 逻辑步长更新（若不需要时钟驱动可留空） </summary>
        void LogicTick(float logicDeltaTime);

        /// <summary> 模块清理与内存解绑 </summary>
        void Dispose();
    }
    public static class EntityModuleFactory
    {
        public static T Create<T>(CharacterEntity owner) where T : IEntityModule, new()
        {
            var module = new T();
            module.Initialize(owner);
            return module;
        }
    }
}
