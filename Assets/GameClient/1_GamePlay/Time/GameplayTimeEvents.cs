using cfg.ZZZ;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 实体受击打断事实事件（纯领域事件，零堆分配）
    /// 由受击裁决管道在判定实体动作打断后发布，时间系统等订阅者据此自发决定是否打醒子弹时间等
    /// </summary>
    public readonly struct EntityHitInterruptedEvent : IGameEvent
    {
        public readonly CharacterEntity Attacker;
        public readonly CharacterEntity Victim;
        public readonly HitReactionType ReactionType;

        public EntityHitInterruptedEvent(CharacterEntity attacker, CharacterEntity victim, HitReactionType reactionType)
        {
            Attacker = attacker;
            Victim = victim;
            ReactionType = reactionType;
        }
    }

    /// <summary>
    /// 极限闪避触发事实事件
    /// 由闪避 Buff 等业务逻辑在判定成功后发布，驱动时间管理器自发进入定向子弹时间
    /// </summary>
    public readonly struct PerfectEvadeTriggeredEvent : IGameEvent
    {
        public readonly CharacterEntity Evader;
        public readonly CharacterEntity Attacker;
        public readonly float Scale;
        public readonly float Duration;
        public readonly bool SmoothRecover;

        public PerfectEvadeTriggeredEvent(CharacterEntity evader, CharacterEntity attacker, float scale, float duration, bool smoothRecover)
        {
            Evader = evader;
            Attacker = attacker;
            Scale = scale;
            Duration = duration;
            SmoothRecover = smoothRecover;
        }
    }

    /// <summary>
    /// 受击顿帧（Hit-Stop）表现请求事件
    /// 由战斗反馈管道在需要打击感卡肉时发布，由时间管理器统一调度实体私有时钟
    /// </summary>
    public readonly struct HitStopRequestEvent : IGameEvent
    {
        public readonly TimeClock AttackerClock;
        public readonly TimeClock VictimClock;
        public readonly float Duration;
        public readonly float Scale;

        public HitStopRequestEvent(TimeClock attackerClock, TimeClock victimClock, float duration, float scale)
        {
            AttackerClock = attackerClock;
            VictimClock = victimClock;
            Duration = duration;
            Scale = scale;
        }
    }
}
