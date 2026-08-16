using Game.Logic;

namespace Game.Logic
{
    public interface ITransitionCondition
    {
        // 传入角色的上下文 Entity，返回此刻是否满足条件
        bool Check(RoleEntity actor);
    }
}
