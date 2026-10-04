namespace Game.GamePlay
{
    /// <summary>
    /// 实体生命周期运行时状态数据。
    /// 遵循四维正交解耦规范，独立私有、易变、可重置。
    /// 供各业务管线与系统权威只读访问实体的存活与死亡状态。
    /// </summary>
    public class LifecycleRuntimeData : EntityRuntimeDataBase
    {
        public bool IsDead => Get<bool>(nameof(IsDead), false);
        public bool IsAlive => !IsDead;

        public override void Reset()
        {
            base.Reset();
            Set(nameof(IsDead), false);
        }
    }
}
