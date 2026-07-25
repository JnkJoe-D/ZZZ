namespace Game.Logic
{
    /// <summary>
    /// 路由执行副作用接口。
    /// 当 ActionRoute 被选中并执行时，依次调用所有挂载的 IRouteEffect.Execute()。
    /// 用于实现：消耗属性、施加/移除 Buff、播放音效等路由附带行为。
    /// 使用 [SerializeReference, SubclassSelector] 在 Inspector 中多态配置。
    /// </summary>
    public interface IRouteEffect
    {
        /// <summary>
        /// 路由被选中执行时调用。
        /// </summary>
        /// <param name="actor">执行路由的角色实体。</param>
        void Execute(CharacterEntity actor);
    }
}
