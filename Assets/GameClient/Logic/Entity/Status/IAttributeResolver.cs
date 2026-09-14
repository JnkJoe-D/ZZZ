using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 属性所属的作用域（生命周期与物理宿主类型）
    /// </summary>
    public enum AttributeScope
    {
        Entity, // 角色/怪物个体私有属性（默认，如 HP, Energy, Decibel, Daze 等）
        Team,   // 小队全局共享资源（如 AssistPoint）
    }

    /// <summary>
    /// 属性元数据作用域注册表。
    /// 集中声明哪些属性具有小队级共享特征。未显式登记的属性 100% 默认归属于 Entity。
    /// </summary>
    public static class AttributeScopeRegistry
    {
        private static readonly Dictionary<AttributeId, AttributeScope> _scopeMap = new()
        {
            // 当前绝区零版本中，全队跨角色的唯一核心战斗资源为支援点数 (AssistPoint)
            { AttributeId.AssistPoint, AttributeScope.Team },
            // 注意：Decibel (喧响值) 为单角色独立私有，走默认的 AttributeScope.Entity，无需在此登记
        };

        /// <summary>
        /// 查询属性的作用域。未显式登记的属性（HP, Energy, Decibel, Daze 等）100% 默认归属于 Entity。
        /// </summary>
        public static AttributeScope GetScope(AttributeId attrId)
        {
            return _scopeMap.TryGetValue(attrId, out var scope) ? scope : AttributeScope.Entity;
        }
    }

    /// <summary>
    /// 实体属性解析门面契约，统一屏蔽个体私有属性与跨层级小队资源的底层访问差异。
    /// </summary>
    public interface IAttributeResolver
    {
        /// <summary>获取属性当前数值</summary>
        float GetAttribute(AttributeId attrId);

        /// <summary>修改属性数值（正增负减）</summary>
        void ModifyAttribute(AttributeId attrId, float delta);

        /// <summary>检查实体是否具有该属性</summary>
        bool HasAttribute(AttributeId attrId);
    }

    /// <summary>
    /// 通用实体基础属性解析器（仅访问自身 StatusModule.Attributes）
    /// </summary>
    public class EntityAttributeResolver : IAttributeResolver
    {
        protected readonly CharacterEntity _entity;

        public EntityAttributeResolver(CharacterEntity entity)
        {
            _entity = entity;
        }

        public virtual float GetAttribute(AttributeId attrId)
        {
            return _entity?.StatusModule?.Attributes?.GetCurrent(attrId) ?? 0f;
        }

        public virtual void ModifyAttribute(AttributeId attrId, float delta)
        {
            _entity?.StatusModule?.Attributes?.Modify(attrId, delta);
        }

        public virtual bool HasAttribute(AttributeId attrId)
        {
            return _entity?.StatusModule?.Attributes?.Has(attrId) ?? false;
        }
    }

    /// <summary>
    /// 玩家角色属性解析器：多态代理自身个体属性与小队共享资源 (TeamRuntimeData)
    /// </summary>
    public class RoleAttributeResolver : EntityAttributeResolver
    {
        private readonly RoleEntity _role;

        public RoleAttributeResolver(RoleEntity role) : base(role)
        {
            _role = role;
        }

        public override float GetAttribute(AttributeId attrId)
        {
            var scope = AttributeScopeRegistry.GetScope(attrId);
            return scope switch
            {
                AttributeScope.Team => TeamRuntimeData.Instance.GetAttribute(attrId),
                _ => base.GetAttribute(attrId)
            };
        }

        public override void ModifyAttribute(AttributeId attrId, float delta)
        {
            var scope = AttributeScopeRegistry.GetScope(attrId);
            switch (scope)
            {
                case AttributeScope.Team:
                    TeamRuntimeData.Instance.ModifyAttribute(attrId, delta);
                    break;
                default:
                    base.ModifyAttribute(attrId, delta);
                    break;
            }
        }

        public override bool HasAttribute(AttributeId attrId)
        {
            if (AttributeScopeRegistry.GetScope(attrId) == AttributeScope.Team)
            {
                return TeamRuntimeData.Instance.HasAttribute(attrId);
            }
            return base.HasAttribute(attrId);
        }
    }

    /// <summary>
    /// 怪物实体属性解析器：专注怪物自身属性（HP, Daze, MaxDaze 等），安全隔离小队属性
    /// </summary>
    public class MonsterAttributeResolver : EntityAttributeResolver
    {
        public MonsterAttributeResolver(MonsterEntity monster) : base(monster)
        {
        }

        public override float GetAttribute(AttributeId attrId)
        {
            // 怪物若被查询小队属性，安全返回 0
            if (AttributeScopeRegistry.GetScope(attrId) == AttributeScope.Team) return 0f;
            return base.GetAttribute(attrId);
        }

        public override void ModifyAttribute(AttributeId attrId, float delta)
        {
            // 怪物若被请求修改小队属性，安全忽略
            if (AttributeScopeRegistry.GetScope(attrId) == AttributeScope.Team) return;
            base.ModifyAttribute(attrId, delta);
        }

        public override bool HasAttribute(AttributeId attrId)
        {
            if (AttributeScopeRegistry.GetScope(attrId) == AttributeScope.Team) return false;
            return base.HasAttribute(attrId);
        }
    }
}
