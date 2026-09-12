using System;
using UnityEngine;
using cfg.ZZZ;
using Game.Logic.Combat.Pipeline;

namespace Game.Logic
{
    /// <summary>
    /// 极限闪避效果器 (Perfect Evade)。
    /// 由 Luban 配置的 BuffEffectData (EffectType = PerfectEvade) 驱动。
    /// 受到攻击命中时：阻断伤害与受击 -> 触发全局玩法子弹时间（激活 TimeManager.IsBulletTimeActive） -> 锁定目标朝向 -> 播放视听特效。
    /// 绝不主动打断或切换动作，角色丝滑完成闪避滑步；反击完全依赖玩家后续输入。
    /// </summary>
    [BuffEffectBinding(BuffEffectType.PerfectEvade)]
    public class PerfectEvadeBuffEffect : IBuffEffect, IHitDefenseModifier
    {
        private readonly int _priority;
        public int DefensePriority => _priority;

        /// <summary>子弹时间内的玩法时钟流速 (0.1 = 10% 速度)</summary>
        public float BulletTimeScale { get; }

        /// <summary>子弹时间持续时长 (真实秒数)</summary>
        public float BulletTimeDuration { get; }

        /// <summary>是否在子弹时间结束前平滑缓出恢复</summary>
        public bool SmoothRecover { get; }

        /// <summary>音效资源 Key (可选)</summary>
        public string SfxKey { get; }

        public PerfectEvadeBuffEffect(BuffEffectData cfg)
        {
            if (cfg != null)
            {
                _priority = cfg.ParamInt1 > 0 ? cfg.ParamInt1 : 300;
                BulletTimeScale = cfg.ParamFloat1 > 0f ? cfg.ParamFloat1 : 0.1f;
                BulletTimeDuration = cfg.ParamFloat2 > 0f ? cfg.ParamFloat2 : 0.8f;
                SmoothRecover = cfg.ParamBool1;
                SfxKey = cfg.ParamStr1;
            }
            else
            {
                _priority = 300;
                BulletTimeScale = 0.1f;
                BulletTimeDuration = 0.8f;
                SmoothRecover = true;
                SfxKey = string.Empty;
            }
        }

        public bool TryInterceptHit(HitPipelineContext ctx, BuffInstance ownerBuff)
        {
            if (ctx == null) return false;

            // 0. 若当前攻击挂载了持续威胁会话，将自身标记为已解决，会话继续存活供队友切入招架
            ctx.ThreatSession?.MarkResolved(ctx.Victim);

            // 1. 阻断受击管线，标记命中结果为 Evaded
            ctx.ResultFlags |= HitResultFlags.Evaded;
            ctx.Abort("Perfect Evaded by Target", HitResultFlags.Evaded);

            // 2. 触发玩法全局子弹时间（UI 保持独立全速；激活 TimeManager.IsBulletTimeActive 并记录触发者）
            if (TimeManager.Instance != null && BulletTimeDuration > 0f)
            {
                TimeManager.Instance.TriggerBulletTime(BulletTimeScale, BulletTimeDuration, ctx.Victim, SmoothRecover);
            }

            // 3. 锁定目标朝向（供后续按键派生闪避反击时精准校准攻击方向）
            if (ctx.Victim != null && ctx.Attacker != null)
            {
                ctx.Victim.SetCombatContextTarget(ctx.Attacker);
            }

            // 4. 【绝不主动派发路由事件】角色平滑完成当前的闪避滑步动作！

            Debug.Log($"<color=cyan>[PerfectEvade] 角色 {ctx.Victim?.name} 触发极限闪避！进入子弹时间 ({BulletTimeDuration}s @ {BulletTimeScale}x)，锁定攻击者: {ctx.Attacker?.name}</color>");
            return true;
        }

        public void OnApply(BuffInstance buff, CharacterEntity target) { }
        public void OnTick(BuffInstance buff, CharacterEntity target, float deltaTime) { }
        public void OnStack(BuffInstance buff, CharacterEntity target, int newStack) { }
        public void OnRemove(BuffInstance buff, CharacterEntity target) { }
    }
}
