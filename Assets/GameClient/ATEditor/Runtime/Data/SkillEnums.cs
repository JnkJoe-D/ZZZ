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

    /// <summary>受击方向计算模式</summary>
    public enum HitDirectionMode
    {
        /// <summary>攻击者根节点 -> 目标 (适合常规近战向外辐射击退，默认)</summary>
        AttackerToTarget,
        /// <summary>检测盒中心 -> 目标 (适合范围AOE/爆炸中心外弹)</summary>
        BoxToTarget,
        /// <summary>片段进入首帧(OnEnter)锁定的相对方向 (基于首帧攻击者局部坐标系XZ平面，如 <0,1> 为攻击者正前方)</summary>
        OnEnterCustomRelative
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
