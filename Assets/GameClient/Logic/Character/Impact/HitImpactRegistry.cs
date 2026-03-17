using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 命中效果注册表。管理 eventTag → IHitImpact 的映射。
    /// SkillHitHandler 通过此注册表查找并执行对应的 Impact 逻辑。
    /// </summary>
    public static class HitImpactRegistry
    {
        private static readonly Dictionary<string, IHitImpact> _registry = new Dictionary<string, IHitImpact>();
        private static IHitImpact _fallbackImpact;

        /// <summary>
        /// 注册一个 eventTag 对应的 Impact 实现。
        /// </summary>
        public static void Register(string eventTag, IHitImpact impact)
        {
            if (string.IsNullOrEmpty(eventTag) || impact == null) return;
            _registry[eventTag] = impact;
        }

        /// <summary>
        /// 注销一个 eventTag 的映射。
        /// </summary>
        public static void Unregister(string eventTag)
        {
            if (string.IsNullOrEmpty(eventTag)) return;
            _registry.Remove(eventTag);
        }

        /// <summary>
        /// 设置当没有匹配到 eventTag 时使用的兜底 Impact。
        /// </summary>
        public static void SetFallback(IHitImpact fallback)
        {
            _fallbackImpact = fallback;
        }

        /// <summary>
        /// 根据 eventTag 查找对应的 IHitImpact。
        /// 找不到时返回 fallback，仍然找不到返回 null。
        /// </summary>
        public static IHitImpact Resolve(string eventTag)
        {
            if (!string.IsNullOrEmpty(eventTag) && _registry.TryGetValue(eventTag, out var impact))
            {
                return impact;
            }
            return _fallbackImpact;
        }

        /// <summary>
        /// 清空所有注册。
        /// </summary>
        public static void Clear()
        {
            _registry.Clear();
            _fallbackImpact = null;
        }

        /// <summary>
        /// 注册默认的 Impact 集合。在游戏启动时调用。
        /// </summary>
        public static void RegisterDefaults()
        {
            Register("Hit_Default", new DefaultDamageImpact());
            Register("Hit_Light", new DefaultDamageImpact());
            Register("Hit_Heavy", new HeavyDamageImpact());
            Register("Hit_Knockback", new KnockbackImpact());
            Register("Hit_Launch", new LaunchImpact());

            SetFallback(new DefaultDamageImpact());
        }
    }
}
