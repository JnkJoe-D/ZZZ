using System.Collections.Generic;
using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic
{
    /// <summary>
    /// Buff 运行时实例。每次施加创建一个，由 BuffContainer 管理生命周期。
    /// 遵循数据驱动与动态上下文分离：持有不可变的 Luban 配置原型，私有维护运行时层数、持续时间与多态效果执行器。
    /// </summary>
    public class BuffInstance
    {
        private static int _nextRuntimeId;

        /// <summary>运行时唯一 ID，用于修改器的 SourceId 追踪。</summary>
        public int RuntimeId { get; }

        /// <summary>Buff 定义（来自 Luban 静态只读配置表）。</summary>
        public cfg.ZZZ.Buff Definition { get; }

        /// <summary>施加时的动态上下文（鸣徽加成、时间轴 Clip 来源等）。</summary>
        public BuffApplyContext Context { get; }

        /// <summary>最大持续时间（秒）。-1 表示永久或完全由外部时间轴片段控制。</summary>
        public float MaxDuration { get; private set; }

        /// <summary>剩余持续时间（秒）。</summary>
        public float RemainingTime { get; set; }

        /// <summary>当前叠加层数。</summary>
        public int CurrentStack { get; set; }

        /// <summary>剩余可生效充能次数（仅 ChargeCountDriven 模式生效）。</summary>
        public int RemainingCharges { get; private set; }

        /// <summary>施加者（来源实体）。</summary>
        public CharacterEntity Source => Context?.Instigator;

        /// <summary>是否为时间轴片段/外部手动驱动生命周期。</summary>
        public bool IsManualDriven => Definition.LifetimeType == BuffLifetimeType.ManualDriven;

        /// <summary>是否为永久 Buff。</summary>
        public bool IsPermanent => IsManualDriven || MaxDuration < 0f;

        /// <summary>是否已过期。</summary>
        public bool IsExpired => !IsPermanent && RemainingTime <= 0f;

        /// <summary>该 Buff 实例当前激活的效果执行器列表。</summary>
        public List<IBuffEffect> ActiveEffects { get; }

        public BuffInstance(cfg.ZZZ.Buff definition, BuffApplyContext context)
        {
            RuntimeId = ++_nextRuntimeId;
            Definition = definition;
            Context = context ?? BuffApplyContext.Default;

            // 1. 计算最大持续时间（基础配表 × 鸣徽/上下文动态修正）
            if (definition.LifetimeType == BuffLifetimeType.ManualDriven)
            {
                MaxDuration = -1f;
            }
            else if (Context.OverrideDuration.HasValue)
            {
                MaxDuration = Context.OverrideDuration.Value;
            }
            else if (definition.BaseDuration < 0f)
            {
                MaxDuration = -1f;
            }
            else
            {
                MaxDuration = Mathf.Max(0.01f, (definition.BaseDuration + Context.DurationAddition) * Context.DurationMultiplier);
            }

            RemainingTime = MaxDuration;
            CurrentStack = 1;
            RemainingCharges = Context.OverrideChargeCount ?? definition.ChargeCount;

            // 2. 借助特性去中心化注册表实例化多态执行器（0 个 switch-case）
            ActiveEffects = BuffEffectRegistry.CreateEffects(definition.Effects, Context);
        }

        /// <summary>
        /// 驱动该 Buff 存活期间的帧逻辑
        /// </summary>
        public void Tick(float deltaTime, CharacterEntity target)
        {
            if (!IsPermanent && RemainingTime > 0f)
            {
                RemainingTime -= deltaTime;
            }

            for (int i = 0; i < ActiveEffects.Count; i++)
            {
                ActiveEffects[i].OnTick(this, target, deltaTime);
            }
        }

        /// <summary>刷新持续时间为最大持续时间。</summary>
        public void RefreshDuration()
        {
            if (!IsPermanent)
            {
                RemainingTime = MaxDuration;
            }
        }

        /// <summary>尝试叠加。返回是否成功增加了层数。</summary>
        public bool TryStack()
        {
            int max = Context.OverrideMaxStack ?? Definition.MaxStack;
            if (CurrentStack >= max)
            {
                return false;
            }
            CurrentStack++;
            return true;
        }

        /// <summary>消耗充能次数</summary>
        public void ConsumeCharge(int count = 1)
        {
            if (RemainingCharges <= 0) return;
            RemainingCharges -= count;
        }
    }
}
