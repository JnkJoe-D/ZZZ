using Game.Logic.Action.Combo;
using Game.Logic.Action.Config;
using Game.Logic.Character.Config;

namespace Game.Logic.Character
{
    /// <summary>
    /// 角色运行时临时数据。
    /// 负责管理闪避计数、冷却、当前指令上下文，以及最近一次路由命中信息。
    /// </summary>
    public class CharacterRuntimeData
    {
        public bool ForceDashNextFrame { get; private set; }
        public bool DashContinuationCandidate { get; private set; }
        public ActionConfigAsset NextActionToCast { get; set; }
        public CommandContextType CurrentCommandContext { get; set; }

        public CommandRouteSource LastRouteSource { get; private set; }
        public CommandContextType LastRouteContext { get; private set; }
        public string LastRouteTag { get; private set; }
        public CommandType LastResolvedCommandType { get; private set; }
        public CommandPhase LastResolvedCommandPhase { get; private set; }
        public int LastResolvedActionId { get; private set; } = -1;

        public int EvadeCount { get; private set; }
        public float EvadeTimer { get; private set; }

        /// <summary>
        /// 当前受击硬直时长，由 HitReactionModule 写入，CharacterHitStunState 读取。
        /// </summary>
        public float CurrentHitStunDuration { get; set; }

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
            if (config == null)
            {
                return false;
            }

            if (EvadeCount >= config.evadeLimitedTimes && EvadeTimer > 0f)
            {
                return false;
            }

            return true;
        }

        public void RecordEvade(CharacterConfigAsset config)
        {
            if (config == null)
            {
                return;
            }

            EvadeCount++;
            EvadeTimer = config.evadeCoolDown;
        }

        public void SetDashContinuationCandidate(bool canContinue)
        {
            DashContinuationCandidate = canContinue;
            ForceDashNextFrame = false;
        }

        public void ResolveDashContinuation(bool hasMovementInput)
        {
            ForceDashNextFrame = DashContinuationCandidate && hasMovementInput;
            DashContinuationCandidate = false;
        }

        public void ConsumeDashContinuation()
        {
            ForceDashNextFrame = false;
        }

        public void ClearDashContinuation()
        {
            ForceDashNextFrame = false;
            DashContinuationCandidate = false;
        }

        public void RecordResolvedRoute(
            CommandRouteSource routeSource,
            string routeTag,
            CommandType commandType,
            CommandPhase commandPhase,
            ActionConfigAsset action)
        {
            LastRouteSource = routeSource;
            LastRouteContext = CurrentCommandContext;
            LastRouteTag = routeTag;
            LastResolvedCommandType = commandType;
            LastResolvedCommandPhase = commandPhase;
            LastResolvedActionId = action != null ? action.ID : -1;
        }

        public void Reset()
        {
            EvadeCount = 0;
            EvadeTimer = 0f;
            CurrentHitStunDuration = 0f;
            CurrentCommandContext = CommandContextType.None;
            LastRouteSource = CommandRouteSource.None;
            LastRouteContext = CommandContextType.None;
            LastRouteTag = null;
            LastResolvedCommandType = CommandType.None;
            LastResolvedCommandPhase = CommandPhase.Started;
            LastResolvedActionId = -1;
            ClearDashContinuation();
        }
    }
}
