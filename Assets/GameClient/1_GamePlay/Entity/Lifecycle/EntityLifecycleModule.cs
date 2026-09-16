using System;
using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色生命周期通用接口
    /// </summary>
    public interface ILifecycleModule
    {
        bool IsAlive { get; }
        bool IsDead { get; }
        event Action<CharacterEntity> OnDied;
        event Action<CharacterEntity> OnRevived;
        void Init(CharacterEntity entity);
        void Die(HitContext? ctx = null);
        void Revive(float hpPercent = 1.0f);
    }

    /// <summary>
    /// 通用实体生命周期管理组件，挂载于 CharacterEntity 上。
    /// 负责管理实体的存活/死亡状态、广播死亡与复活事件、停止动作播放等核心流程。
    /// </summary>
    public class EntityLifecycleModule : MonoBehaviour, ILifecycleModule
    {
        protected CharacterEntity _entity;
        public bool IsDead { get; protected set; } = false;
        public bool IsAlive => !IsDead;

        public event Action<CharacterEntity> OnDied;
        public event Action<CharacterEntity> OnRevived;

        public virtual void Init(CharacterEntity entity)
        {
            _entity = entity;
            IsDead = false;
        }

        public virtual void Die(HitContext? ctx = null)
        {
            if (IsDead) return;
            IsDead = true;

            HandleDeath(ctx);
            OnDied?.Invoke(_entity);

            EventCenter.Publish(new EntityDiedEvent
            {
                Victim = _entity,
                Attacker = ctx.HasValue ? ctx.Value.attacker : null
            });
        }

        public virtual void Revive(float hpPercent = 1.0f)
        {
            if (!IsDead) return;
            IsDead = false;

            if (_entity?.StatusModule?.Attributes != null && _entity.StatusModule.Attributes.Has(AttributeId.MaxHp))
            {
                float maxHp = _entity.StatusModule.Attributes.GetCurrent(AttributeId.MaxHp);
                _entity.StatusModule.Attributes.SetValue(AttributeId.HP, maxHp * Mathf.Clamp01(hpPercent));
            }

            OnRevived?.Invoke(_entity);
        }

        protected virtual void HandleDeath(HitContext? ctx)
        {
            // 通用操作：停止当前动作播放
            _entity?.ActionPlayer?.StopAction();
        }
    }

    /// <summary>
    /// 实体死亡事件
    /// </summary>
    public struct EntityDiedEvent : IGameEvent
    {
        public CharacterEntity Victim;
        public CharacterEntity Attacker;
    }
}
