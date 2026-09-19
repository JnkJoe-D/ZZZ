namespace Game.GamePlay
{
    /// <summary>
    /// 所有挂载于实体 GameObject 上的 MonoBehaviour 组件必须实现的统一契约。
    /// 规范表现与引擎适配层的生命周期。
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary> 所属实体聚合根 </summary>
        CharacterEntity OwnerEntity { get; }

        /// <summary> 组件装配初始化（由 Entity 在 Awake/Init 阶段显式调用） </summary>
        void OnComponentInit(CharacterEntity owner);

        /// <summary> 实体复用/出池唤醒（重置物理与动效状态） </summary>
        void OnComponentSpawn();

        /// <summary> 实体回收/入池休眠（清理物理状态与临时效果） </summary>
        void OnComponentDespawn();
    }
}
