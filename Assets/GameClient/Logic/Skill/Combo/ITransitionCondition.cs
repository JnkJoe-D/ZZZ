using Game.Logic.Character;

namespace Game.Logic.Action.Combo
{
    public interface ITransitionCondition
    {
        // 传入角色的上下文 Entity，返回此刻是否满足条件
        bool Check(CharacterEntity actor);
    }
}
