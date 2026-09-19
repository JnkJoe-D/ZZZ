using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 索敌与目标仲裁接口，继承 IEntityModule 规范领域模块生命周期。
    /// 内聚常规索敌目标、临时战斗上下文目标（招架/弹刀反击）及最终有效目标仲裁。
    /// </summary>
    public interface ITargetFinder : IEntityModule
    {
        /// <summary>
        /// 当前动作/战斗上下文关联的临时交互目标实体（如招架黄光攻击者、弹刀反击目标）。
        /// 优先级高于常规索敌目标。
        /// </summary>
        CharacterEntity CombatContextTarget { get; set; }

        /// <summary> 设置当前战斗上下文目标 </summary>
        void SetCombatContextTarget(CharacterEntity target);

        /// <summary> 清空当前战斗上下文目标 </summary>
        void ClearCombatContextTarget();

        /// <summary> 获取常规索敌目标 </summary>
        Transform GetTarget();

        /// <summary> 获取至常规目标的距离 </summary>
        float GetDistanceToTarget();

        /// <summary>
        /// 获取当前生效的目标 Transform：优先返回存活的 CombatContextTarget，无上下文目标时回退至常规索敌目标。
        /// </summary>
        Transform GetEffectiveTarget();
    }
}
