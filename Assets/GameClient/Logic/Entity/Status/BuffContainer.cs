using System.Collections.Generic;
using UnityEngine;
using cfg.ZZZ;

namespace Game.Logic
{
    /// <summary>Buff 移除原因。</summary>
    public enum BuffRemoveReason
    {
        Expired,    // 到期
        Dispelled,  // 驱散
        Replaced,   // 替换
        Manual,     // 手动移除
    }

    /// <summary>
    /// Buff 容器。管理一个 Entity 上所有活跃 Buff 的生命周期。
    /// 异源共享叠加模式：同 BuffId 共享叠层与持续时间。
    /// 彻底基于 Luban cfg.ZZZ.Buff 配置原型与动态施加上下文 (BuffApplyContext) 驱动。
    /// </summary>
    public class BuffContainer
    {
        private readonly List<BuffInstance> _activeBuffs = new(8);
        private readonly List<BuffInstance> _pendingRemove = new(4);
        private CharacterEntity _owner;

        public IReadOnlyList<BuffInstance> ActiveBuffs => _activeBuffs;

        public void Init(CharacterEntity owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 依据配置 ID 施加一个 Buff（自动检索 Luban 表）。
        /// </summary>
        public BuffInstance AddBuff(int buffId, BuffApplyContext context = null)
        {
            var def = ConfigManager.Instance?.Tables?.TbBuff?.GetOrDefault(buffId);
            if (def == null)
            {
                Debug.LogWarning($"[BuffContainer] 未找到 ID 为 {buffId} 的 Buff 配置！");
                return null;
            }
            return AddBuff(def, context);
        }

        /// <summary>
        /// 依据 Luban Buff 定义资产施加一个 Buff。
        /// </summary>
        public BuffInstance AddBuff(cfg.ZZZ.Buff definition, BuffApplyContext context = null)
        {
            if (definition == null) return null;

            context ??= BuffApplyContext.Default;

            // 查找已存在的同 Id 实例
            BuffInstance existing = FindBuff(definition.Id);

            if (existing != null)
            {
                return HandleExistingBuff(existing, definition, context);
            }

            // 新施加
            return ApplyNewBuff(definition, context);
        }

        /// <summary>按 BuffId 移除 Buff。</summary>
        public bool RemoveBuff(int buffId, BuffRemoveReason reason = BuffRemoveReason.Manual)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].Definition.Id == buffId)
                {
                    RemoveAtIndex(i, reason);
                    return true;
                }
            }
            return false;
        }

        /// <summary>移除所有包含指定 Tag 的 Buff。</summary>
        public int RemoveByTag(string tag, BuffRemoveReason reason = BuffRemoveReason.Dispelled)
        {
            if (string.IsNullOrEmpty(tag)) return 0;
            int removed = 0;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].Definition.Tags != null && _activeBuffs[i].Definition.Tags.Contains(tag))
                {
                    RemoveAtIndex(i, reason);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>是否拥有指定 BuffId 的 Buff。</summary>
        public bool HasBuff(int buffId)
        {
            return FindBuff(buffId) != null;
        }

        /// <summary>获取指定 Buff 的当前叠加层数。不存在返回 0。</summary>
        public int GetStack(int buffId)
        {
            BuffInstance inst = FindBuff(buffId);
            return inst?.CurrentStack ?? 0;
        }

        /// <summary>是否拥有包含指定 Tag 的任意 Buff。</summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Definition.Tags != null && _activeBuffs[i].Definition.Tags.Contains(tag))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>每帧驱动：更新持续时间，到期移除，驱动效果 Tick。</summary>
        public void Tick(float deltaTime)
        {
            _pendingRemove.Clear();

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                BuffInstance buff = _activeBuffs[i];

                buff.Tick(deltaTime, _owner);

                if (buff.IsExpired)
                {
                    _pendingRemove.Add(buff);
                }
            }

            // 统一移除到期 Buff（避免迭代中修改列表）
            for (int i = 0; i < _pendingRemove.Count; i++)
            {
                int index = _activeBuffs.IndexOf(_pendingRemove[i]);
                if (index >= 0)
                {
                    RemoveAtIndex(index, BuffRemoveReason.Expired);
                }
            }

            _pendingRemove.Clear();
        }

        /// <summary>移除所有 Buff。</summary>
        public void Clear()
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                RemoveAtIndex(i, BuffRemoveReason.Manual);
            }
        }

        // ────────────────── 内部实现 ──────────────────

        private BuffInstance FindBuff(int buffId)
        {
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Definition.Id == buffId)
                {
                    return _activeBuffs[i];
                }
            }
            return null;
        }

        private BuffInstance HandleExistingBuff(BuffInstance existing, cfg.ZZZ.Buff definition, BuffApplyContext context)
        {
            switch (definition.StackBehavior)
            {
                case BuffStackBehavior.RefreshDuration:
                    if (existing.TryStack())
                    {
                        existing.RefreshDuration();
                        NotifyStack(existing);
                    }
                    else
                    {
                        // 已满层，仅刷新时间
                        existing.RefreshDuration();
                    }
                    return existing;

                case BuffStackBehavior.NoRefresh:
                    if (existing.TryStack())
                    {
                        NotifyStack(existing);
                    }
                    return existing;

                case BuffStackBehavior.Reject:
                    // 已存在则拒绝
                    return existing;

                case BuffStackBehavior.Replace:
                    RemoveBuff(existing.Definition.Id, BuffRemoveReason.Replaced);
                    return ApplyNewBuff(definition, context);

                default:
                    return existing;
            }
        }

        private BuffInstance ApplyNewBuff(cfg.ZZZ.Buff definition, BuffApplyContext context)
        {
            BuffInstance buff = new BuffInstance(definition, context);
            _activeBuffs.Add(buff);

            // 执行 OnApply
            for (int i = 0; i < buff.ActiveEffects.Count; i++)
            {
                buff.ActiveEffects[i]?.OnApply(buff, _owner);
            }

            // 发布事件
            Game.Framework.EventCenter.Publish(new BuffAppliedEvent
            {
                Target = _owner,
                Buff = buff
            });

            return buff;
        }

        private void RemoveAtIndex(int index, BuffRemoveReason reason)
        {
            BuffInstance buff = _activeBuffs[index];
            _activeBuffs.RemoveAt(index);

            // 执行 OnRemove
            for (int i = 0; i < buff.ActiveEffects.Count; i++)
            {
                buff.ActiveEffects[i]?.OnRemove(buff, _owner);
            }

            // 发布事件
            Game.Framework.EventCenter.Publish(new BuffRemovedEvent
            {
                Target = _owner,
                Definition = buff.Definition,
                Reason = reason
            });
        }

        private void NotifyStack(BuffInstance buff)
        {
            for (int i = 0; i < buff.ActiveEffects.Count; i++)
            {
                buff.ActiveEffects[i]?.OnStack(buff, _owner, buff.CurrentStack);
            }
        }

        /// <summary>
        /// 收集当前实体身上所有生效的防御拦截器（零 GC 填充进传入的 buffer 列表中，并按优先级降序排列）。
        /// </summary>
        public void GetActiveDefenseModifiers(List<(IHitDefenseModifier Modifier, BuffInstance Buff)> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();

            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                var buff = _activeBuffs[i];
                var effects = buff.ActiveEffects;
                if (effects == null) continue;

                for (int j = 0; j < effects.Count; j++)
                {
                    if (effects[j] is IHitDefenseModifier modifier)
                    {
                        buffer.Add((modifier, buff));
                    }
                }
            }

            if (buffer.Count > 1)
            {
                buffer.Sort((a, b) => b.Modifier.DefensePriority.CompareTo(a.Modifier.DefensePriority));
            }
        }
    }
}
