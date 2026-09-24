using Game.GamePlay;

namespace Game.GamePlay
{
    public interface ITransitionCondition
    {
        // 传入角色的上下文 Entity，返回此刻是否满足条件
        bool Check(CharacterEntity actor);
        /// <summary>
        /// 路由被最终 Commit（确认执行）时回调。
        /// 用于执行 Check 阶段不应产生的副作用（如清除一次性状态标记）。
        /// 默认为空实现，仅需副作用的条件才覆写。
        /// </summary>
        void OnCommit(CharacterEntity actor) { }
    }
}
