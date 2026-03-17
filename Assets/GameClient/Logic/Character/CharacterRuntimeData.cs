using Game.Logic.Action.Config;
using Game.Logic.Character.Config;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 角色运行时临时数据托管类
    /// 负责管理闪避计数、冷却、增益状态等不随状态机切换而丢失的数据。
    /// </summary>
    public class CharacterRuntimeData
    {
        public bool ForceDashNextFrame { get; set; }
        public ActionConfigAsset NextActionToCast { get; set; }

        public int EvadeCount { get; private set; }
        public float EvadeTimer { get; private set; }

        /// <summary>
        /// 更新运行时数据逻辑（由 Entity.Update 驱动）
        /// </summary>
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

        /// <summary>
        /// 是否可以执行闪避
        /// </summary>
        public bool CanEvade(CharacterConfigAsset config)
        {
            if (config == null) return false;
            // 如果次数达到上限且计时器还在跑，则不允许闪避
            if (EvadeCount >= config.evadeLimitedTimes && EvadeTimer > 0f) return false;
            return true;
        }

        /// <summary>
        /// 记录一次闪避
        /// </summary>
        public void RecordEvade(CharacterConfigAsset config)
        {
            if (config == null) return;
            EvadeCount++;
            EvadeTimer = config.evadeCoolDown;
        }

        /// <summary>
        /// 重置数据
        /// </summary>
        public void Reset()
        {
            EvadeCount = 0;
            EvadeTimer = 0f;
            CurrentHitStunDuration = 0f;
        }

        // --- 受击相关 ---
        /// <summary>
        /// 当前受击硬直时长（由 HitReactionModule 写入，CharacterHitStunState 读取）
        /// </summary>
        public float CurrentHitStunDuration { get; set; }

    }
}
