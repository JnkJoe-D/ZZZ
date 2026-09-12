using System;
using UnityEngine;
using cfg.ZZZ;
using Game.Logic.Combat.Pipeline;

namespace Game.Logic
{
    /// <summary>
    /// 纯无敌效果器 (Invincible)。
    /// 由 Luban 配置的 BuffEffectData (EffectType = Invincible) 驱动。
    /// 用于闪避反击后半段、招架反击攻击段、连携技特写、强化战技起手无敌等。
    /// 受到攻击时纯粹吸收打击并短路受击管线，受击者不扣血、不被打断、不触发硬直。
    /// </summary>
    [BuffEffectBinding(BuffEffectType.Invincible)]
    public class InvincibleBuffEffect : IBuffEffect, IHitDefenseModifier
    {
        private readonly int _priority;
        public int DefensePriority => _priority;

        public InvincibleBuffEffect(BuffEffectData cfg = null)
        {
            _priority = (cfg != null && cfg.ParamInt1 > 0) ? cfg.ParamInt1 : 50;
        }

        public bool TryInterceptHit(HitPipelineContext ctx, BuffInstance ownerBuff)
        {
            if (ctx == null) return false;

            ctx.ResultFlags |= HitResultFlags.Invincible;
            ctx.Abort("Victim is Invincible", HitResultFlags.Invincible);
            return true;
        }

        public void OnApply(BuffInstance buff, CharacterEntity target) { }
        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime) { }
        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }
        public void OnRemove(BuffInstance buff, CharacterEntity target) { }
    }
}
