using System;

namespace ATEditor
{
    public enum HitBoxType 
    { 
        Sphere, 
        Box, 
        Capsule, 
        Sector, 
        Ring 
    }



    public enum Frequency 
    { 
        Once, 
        Times 
    }

    public enum TargetSortMode 
    { 
        None, 
        Closest, 
        Random 
    }

    /// <summary>命中效果类型</summary>
    public enum HitEffectType
    {
        /// <summary>造成伤害（扣 HP + 累积喧响）</summary>
        Damage,
        /// <summary>修改任意属性（加减指定值）</summary>
        ModifyAttribute,
        /// <summary>施加 Buff</summary>
        ApplyBuff,
    }

    /// <summary>效果施加目标</summary>
    public enum EffectTarget
    {
        /// <summary>被击者（默认）</summary>
        Victim,
        /// <summary>攻击者自己</summary>
        Attacker,
    }

    /// <summary>命中效果在多段打击中的触发策略</summary>
    public enum EffectTriggerPolicy
    {
        /// <summary>每次打击都触发</summary>
        EveryHit,
        /// <summary>仅第一段打击触发</summary>
        FirstHitOnly,
        /// <summary>仅最后一段打击触发</summary>
        LastHitOnly
    }
}
