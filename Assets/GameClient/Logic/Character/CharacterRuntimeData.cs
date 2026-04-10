using Game.Logic.Action.Combo;
using Game.Logic.Action.Config;
using Game.Logic.Character.Config;

namespace Game.Logic.Character
{
    /// <summary>
    /// 【角色运行时数据中枢】
    /// 承载了当前指令上下文、各系统资源状态（闪避/受击）、以及 v3 架构特有的动作回流提示。
    /// </summary>
    public class CharacterRuntimeData
    {
        /// <summary>
        /// <summary>
        /// 当从 ActionController 切入 GroundState 时，携带的子状态目标。
        /// 读取后应消费并重置为 Idle。
        /// </summary>
        public ActionState TargetGroundSubState { get; set; } = ActionState.Idle;

        /// <summary>
        /// 基础攻击是否处于按住蓄力状态。
        /// </summary>
        public bool IsBasicAttackHold { get; set; }

        /// <summary>
        /// 当前正在执行或即将执行的动作资产。
        /// </summary>
        public ActionConfigAsset NextActionToCast { get; set; }


        /// <summary>
        /// 移动输入是否处于“短输入”判定范围内（由 JogState 维护）。
        /// </summary>
        public bool IsShortMoveInput { get; set; }

        // ── 全链路追踪字段 (用于调试与回溯) ──
        public CommandRouteSource LastRouteSource { get; private set; }
        public string LastRouteTag { get; private set; }
        public InputCommand LastResolvedCommandType { get; private set; }
        public CommandPhase LastResolvedCommandPhase { get; private set; }
        public int LastResolvedActionId { get; private set; } = -1;

        /// <summary>
        /// 闪避计数与冷却计时。
        /// </summary>
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


        /// <summary>
        /// 记录一次成功的路由解析结果。
        /// </summary>
        public void RecordResolvedRoute(
            CommandRouteSource routeSource,
            string routeTag,
            InputCommand commandType,
            CommandPhase commandPhase,
            ActionConfigAsset action)
        {
            LastRouteSource = routeSource;
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
            LastRouteSource = CommandRouteSource.None;
            LastRouteTag = null;
            LastResolvedCommandType = InputCommand.None;
            LastResolvedCommandPhase = CommandPhase.Started;
            LastResolvedActionId = -1;
            TargetGroundSubState = ActionState.Idle;
            IsBasicAttackHold = false;
            IsShortMoveInput = false;
        }
    }
}
