using System.Collections.Generic;
using UnityEngine;

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
    /// 异源共享叠加模式：同 BuffId 不区分施加者，共享叠层与持续时间。
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
        /// 施加一个 Buff。根据叠加规则处理已存在的同类 Buff。
        /// </summary>
        public BuffInstance AddBuff(BuffDefAsset definition, CharacterEntity source = null)
        {
            if (definition == null) return null;

            // 查找已存在的同 BuffId 实例
            BuffInstance existing = FindBuff(definition.BuffId);

            if (existing != null)
            {
                return HandleExistingBuff(existing, definition, source);
            }

            // 新施加
            return ApplyNewBuff(definition, source);
        }

        /// <summary>按 BuffId 移除 Buff。</summary>
        public bool RemoveBuff(int buffId, BuffRemoveReason reason = BuffRemoveReason.Manual)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].Definition.BuffId == buffId)
                {
                    RemoveAtIndex(i, reason);
                    return true;
                }
            }
            return false;
        }

        /// <summary>按定义移除 Buff。</summary>
        public bool RemoveBuff(BuffDefAsset definition, BuffRemoveReason reason = BuffRemoveReason.Manual)
        {
            if (definition == null) return false;
            return RemoveBuff(definition.BuffId, reason);
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

        /// <summary>是否拥有指定定义的 Buff。</summary>
        public bool HasBuff(BuffDefAsset definition)
        {
            if (definition == null) return false;
            return HasBuff(definition.BuffId);
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

                // 驱动效果 Tick
                if (buff.Definition.Effects != null)
                {
                    for (int j = 0; j < buff.Definition.Effects.Count; j++)
                    {
                        buff.Definition.Effects[j]?.OnTick(buff, _owner, deltaTime);
                    }
                }

                // 更新持续时间
                if (!buff.IsPermanent)
                {
                    buff.RemainingTime -= deltaTime;
                    if (buff.IsExpired)
                    {
                        _pendingRemove.Add(buff);
                    }
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

        // ────────────────── 内部 ──────────────────

        private BuffInstance FindBuff(int buffId)
        {
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Definition.BuffId == buffId)
                {
                    return _activeBuffs[i];
                }
            }
            return null;
        }

        private BuffInstance HandleExistingBuff(BuffInstance existing, BuffDefAsset definition, CharacterEntity source)
        {
            switch (definition.StackBehavior)
            {
                case StackBehavior.StackAndRefresh:
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

                case StackBehavior.StackNoRefresh:
                    if (existing.TryStack())
                    {
                        NotifyStack(existing);
                    }
                    return existing;

                case StackBehavior.Reject:
                    // 已存在则拒绝
                    return existing;

                case StackBehavior.Replace:
                    RemoveBuff(existing.Definition.BuffId, BuffRemoveReason.Replaced);
                    return ApplyNewBuff(definition, source);

                default:
                    return existing;
            }
        }

        private BuffInstance ApplyNewBuff(BuffDefAsset definition, CharacterEntity source)
        {
            BuffInstance buff = new BuffInstance(definition, source);
            _activeBuffs.Add(buff);

            // 执行 OnApply
            if (definition.Effects != null)
            {
                for (int i = 0; i < definition.Effects.Count; i++)
                {
                    definition.Effects[i]?.OnApply(buff, _owner);
                }
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
            if (buff.Definition.Effects != null)
            {
                for (int i = 0; i < buff.Definition.Effects.Count; i++)
                {
                    buff.Definition.Effects[i]?.OnRemove(buff, _owner);
                }
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
            if (buff.Definition.Effects == null) return;
            for (int i = 0; i < buff.Definition.Effects.Count; i++)
            {
                buff.Definition.Effects[i]?.OnStack(buff, _owner, buff.CurrentStack);
            }
        }
    }
}
