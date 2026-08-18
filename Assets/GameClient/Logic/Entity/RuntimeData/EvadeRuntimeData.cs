using Game.Logic;

namespace Game.Logic
{
    public class EvadeRuntimeData
    {
        public int EvadeCount { get; private set; }
        public float EvadeTimer { get; private set; }

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
        }

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

        public void Reset()
        {
            EvadeCount = 0;
            EvadeTimer = 0f;
        }
    }
}
