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
        public bool DashOnNextGroundEnter { get; private set; }
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

        /// <summary>
        /// 当前受击保障轴。受击动画的水平 root motion 会投影到这条世界轴上。
        /// </summary>
        public UnityEngine.Vector3 CurrentHitReactionAxis { get; private set; }

        public bool HasHitReactionAxis { get; private set; }


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

        public void SetHitReactionAxis(UnityEngine.Vector3 axis)
        {
            axis.y = 0f;
            if (axis.sqrMagnitude <= 0.0001f)
            {
                ClearHitReactionAxis();
                return;
            }

            CurrentHitReactionAxis = axis.normalized;
            HasHitReactionAxis = true;
        }

        public void ClearHitReactionAxis()
        {
            CurrentHitReactionAxis = UnityEngine.Vector3.zero;
            HasHitReactionAxis = false;
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

        public void SetDashOnNextGroundEnter(bool shouldDash)
        {
            DashOnNextGroundEnter = shouldDash;
        }

        public bool ConsumeDashOnGroundEnter(bool hasMovementInput)
        {
            bool shouldEnterDash = DashOnNextGroundEnter && hasMovementInput;
            DashOnNextGroundEnter = false;
            return shouldEnterDash;
        }

        public void ClearDashContinuation()
        {
            DashOnNextGroundEnter = false;
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
            ClearHitReactionAxis();
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
