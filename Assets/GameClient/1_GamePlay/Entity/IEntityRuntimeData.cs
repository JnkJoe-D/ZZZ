namespace Game.GamePlay
{
    /// <summary>
    /// 实体运行时数据核心契约。
    /// 统一对外暴露泛型 Set/Get 数据访问门面，彻底消灭外部直接访问公开属性用 = 赋值的隐患。
    /// </summary>
    public interface IEntityRuntimeData
    {
        /// <summary>
        /// 统一设值方法：通过属性名或字段Key赋值。
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">属性名称或字段Key</param>
        /// <param name="value">目标新值</param>
        /// <returns>若新旧值不同并成功写入返回 true；若值未变动返回 false</returns>
        bool Set<T>(string key, T value);

        /// <summary>
        /// 统一取值方法：通过属性名或字段Key获取强类型值。
        /// </summary>
        /// <typeparam name="T">期望的值类型</typeparam>
        /// <param name="key">属性名称或字段Key</param>
        /// <param name="defaultValue">未命中时的缺省默认值</param>
        /// <returns>强类型值</returns>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// 尝试获取强类型值。
        /// </summary>
        bool TryGet<T>(string key, out T value);

        /// <summary>
        /// 检查是否包含指定的字段Key。
        /// </summary>
        bool Has(string key);

        /// <summary>
        /// 统一属性变动事件：值真实变动时自动派发 (key, oldValue, newValue)。
        /// </summary>
        event System.Action<string, object, object> OnValueChanged;

        /// <summary>
        /// 统一重置契约（用于实体回池与复用）。
        /// </summary>
        void Reset();
    }
}
