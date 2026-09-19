using Game.GamePlay;

namespace Game.GamePlay
{
    public class MonSterBehaviorRuntimeData : EntityRuntimeDataBase
    {
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

        public void Tick(float dt) => Update(dt);

        public void StartAttackCooldown(float cooldown)
        {
            AttackCooldownTimer = cooldown;
        }
    }
}
