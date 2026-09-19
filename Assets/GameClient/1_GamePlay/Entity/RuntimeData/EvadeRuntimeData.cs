using Game.GamePlay;

namespace Game.GamePlay
{
    public class EvadeRuntimeData : EntityRuntimeDataBase
    {
        public int EvadeCount { get; private set; }
        public float EvadeTimer { get; private set; }

        /// <summary>
        /// 是否处于极限闪避触发的子弹时间反击判定窗口期内（数据驱动单一真理源）
        /// </summary>
        public bool IsInBulletTimeWindow => BulletTimeWindowTimer > 0f;

        /// <summary>
        /// 子弹时间反击窗口剩余倒计时
        /// </summary>
        public float BulletTimeWindowTimer { get; set; }

        public void Update(float deltaTime)
        {
            if (EvadeTimer > 0f)
            {
                EvadeTimer -= deltaTime;
                if (EvadeTimer <= 0f)
                {
                    EvadeCount = 0;
                    EvadeTimer = 0f;
                }
            }

            if (BulletTimeWindowTimer > 0f)
            {
                BulletTimeWindowTimer -= deltaTime;
                if (BulletTimeWindowTimer <= 0f)
                {
                    BulletTimeWindowTimer = 0f;
                }
            }
        }

        public void Tick(float deltaTime) => Update(deltaTime);

        public bool CanEvade(CharacterConfigAsset config)
        {
            if (config is RoleConfigAsset roleConfig)
            {
                if (EvadeCount >= roleConfig.evadeLimitedTimes && EvadeTimer > 0f)
                {
                    return false;
                }
            }
            return true;
        }

        public void RecordEvade(CharacterConfigAsset config)
        {
            if (config is RoleConfigAsset roleConfig)
            {
                EvadeCount++;
                EvadeTimer = roleConfig.evadeCoolDown;
            }
            else
            {
                EvadeCount++;
                EvadeTimer = 1f;
            }
        }

        public override void Reset()
        {
            base.Reset();
            EvadeCount = 0;
            EvadeTimer = 0f;
            BulletTimeWindowTimer = 0f;
        }
    }
}
