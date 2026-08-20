using Game.Logic;

namespace Game.Logic
{
    public enum MonsterAIState
    {
        Idle,       // 默认/发呆
        Wander,     // 巡逻
        Chase,      // 追击（目标较远）
        Adjust,     // 周旋（目标较近，技能冷却中）
        Attack,     // 攻击
        HitStun,    // 受击硬直
        Dead        // 死亡
    }

    public class MonSterBehaviorRuntimeData : IEntityRuntimeData
    {
        public MonsterAIState CurrentState { get; private set; } = MonsterAIState.Idle;
        public float StateTimer { get; private set; } = 0f;
        public float AttackCooldownTimer { get; private set; } = 0f;

        public void Update(float dt)
        {
            StateTimer += dt;
            if (AttackCooldownTimer > 0)
            {
                AttackCooldownTimer -= dt;
            }
        }

        public void StartAttackCooldown(float cooldown)
        {
            AttackCooldownTimer = cooldown;
        }

        public void ChangeState(MonsterAIState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                StateTimer = 0f;
            }
        }
    }
}
