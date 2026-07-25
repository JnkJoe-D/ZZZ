using System;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 消耗属性效果。路由执行时扣除指定属性的指定数量。
    /// 用于实现"消耗 2 层霜冻 → 变为特殊攻击"等机制。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("消耗属性 (ConsumeAttribute)")]
    public class ConsumeAttributeEffect : IRouteEffect
    {
        [Tooltip("要消耗的属性。")]
        public AttributeId TargetAttribute;

        [Tooltip("消耗数量（正数表示扣除）。")]
        public float Amount = 1f;

        public void Execute(CharacterEntity actor)
        {
            var attrSet = actor?.StatusModule?.Attributes;
            if (attrSet == null || !attrSet.Has(TargetAttribute)) return;

            attrSet.Modify(TargetAttribute, -Mathf.Abs(Amount));
            Debug.Log($"<color=cyan>[ConsumeAttribute] {actor.name} 消耗 {TargetAttribute} x{Amount}, " +
                      $"剩余: {attrSet.GetCurrent(TargetAttribute)}</color>");
        }
    }

    /// <summary>
    /// 施加 Buff 效果。路由执行时对自身施加指定 Buff。
    /// 用于实现"进入特殊状态时自动获得增益"等机制。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("施加 Buff (ApplyBuff)")]
    public class ApplyBuffRouteEffect : IRouteEffect
    {
        [Tooltip("要施加的 Buff 定义。")]
        public BuffDefAsset BuffDef;

        public void Execute(CharacterEntity actor)
        {
            if (actor?.StatusModule == null || BuffDef == null) return;

            if (actor.StatusModule.IsBuffImmune(BuffDef))
            {
                Debug.Log($"<color=grey>[ApplyBuffRoute] {actor.name} 免疫 Buff '{BuffDef.DisplayName}'</color>");
                return;
            }

            actor.StatusModule.Buffs.AddBuff(BuffDef, actor);
            Debug.Log($"<color=green>[ApplyBuffRoute] {actor.name} 施加 Buff '{BuffDef.DisplayName}'</color>");
        }
    }

    /// <summary>
    /// 移除 Buff 效果。路由执行时移除自身的指定 Buff。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("移除 Buff (RemoveBuff)")]
    public class RemoveBuffRouteEffect : IRouteEffect
    {
        [Tooltip("要移除的 Buff 定义。")]
        public BuffDefAsset BuffDef;

        public void Execute(CharacterEntity actor)
        {
            if (actor?.StatusModule?.Buffs == null || BuffDef == null) return;

            bool removed = actor.StatusModule.Buffs.RemoveBuff(BuffDef);
            if (removed)
            {
                Debug.Log($"<color=yellow>[RemoveBuffRoute] {actor.name} 移除 Buff '{BuffDef.DisplayName}'</color>");
            }
        }
    }

    /// <summary>
    /// 设置属性值效果。路由执行时将指定属性设置为固定值。
    /// 用于实现"满层消耗后清零"等机制。
    /// </summary>
    [Serializable]
    [SubclassDisplayName("设置属性值 (SetAttribute)")]
    public class SetAttributeRouteEffect : IRouteEffect
    {
        [Tooltip("目标属性。")]
        public AttributeId TargetAttribute;

        [Tooltip("设置的目标值。")]
        public float Value = 0f;

        public void Execute(CharacterEntity actor)
        {
            var attrSet = actor?.StatusModule?.Attributes;
            if (attrSet == null || !attrSet.Has(TargetAttribute)) return;

            attrSet.SetValue(TargetAttribute, Value);
            Debug.Log($"<color=cyan>[SetAttribute] {actor.name} 设置 {TargetAttribute} = {Value}</color>");
        }
    }
}
