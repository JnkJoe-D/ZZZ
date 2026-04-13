using System.Collections.Generic;
using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Action.Config
{
    /// <summary>
    /// 动作所处的状态机环境。每个 ActionConfigAsset 必须显式声明。
    /// </summary>
    public enum ActionState
    {
        Idle = 0,       // GroundState → Idle 子状态
        Jog = 10,       // GroundState → Jog 子状态
        Dash = 20,      // GroundState → Dash 子状态
        Stop = 30,      // GroundState → Stop 子状态
        Skill = 40,     // CharacterSkillState
        Evade = 50,     // CharacterEvadeState
        Hit = 60,       // CharacterHitStunState
    }

    /// <summary>
    /// 动作播放结束后的转换策略。
    /// </summary>
    public enum ActionCompleteMode
    {
        /// <summary>
        /// 动作结束后由状态机默认逻辑决定（通常回 Idle）。
        /// </summary>
        Default = 0,

        /// <summary>
        /// 动作结束后保持当前状态不变（循环动作、由监控器驱动退出）。
        /// </summary>
        Stay = 10,

        /// <summary>
        /// 动作结束后自动衔接 CompleteAction 指定的后续动作。
        /// </summary>
        TransitToAction = 20,
    }

    /// <summary>
    /// 全局动作配置基类。
    /// 用于描述任意动作的基础信息，并关联 SkillTimeline 资产。
    /// </summary>
    /// <summary>
    /// 全局动作配置基类（混合态架构数据载体）
    /// 承载了 Timeline 资源引用、派生路由配置、以及状态机回流提示。
    /// </summary>
    public abstract class ActionConfigAsset : ScriptableObject
    {
        [Header("基础信息")]
        public int ID;
        public string Name;

        [Header("SkillEditor 核心资产")]
        [Tooltip("SkillEditor 生成的标准化时间轴数据，ActionPlayer 会解析并播放它。")]
        public TextAsset TimelineAsset;

        [Header("转向优先级策略")]
        public ActionTurnMode TurnMode = ActionTurnMode.InputDirection;

        [Header("状态转换")]
        [Tooltip("指定此动作执行时的目标状态机主状态/子状态。")]
        public ActionState EnterState = ActionState.Idle;

        [Tooltip("动作正常完成后的后续转换策略。")]
        public ActionCompleteMode CompleteMode = ActionCompleteMode.Default;

        [Tooltip("当 CompleteMode 设为 TransitToAction 时，自动衔接的这个后续动作。")]
        public ActionConfigAsset CompleteAction;

        // ── 统一路由 ──────────────────────────────────────────
        [Header("统一路由")]
        [Tooltip("将玩家指令、生命周期统一后的路由列表。")]
        public List<ActionRoute> Routes = new();

        [Header("统一路由集")]
        [Tooltip("复用化的统一路由集资产。")]
        public List<ActionRouteSetAsset> RouteSets = new();

        // ── 每帧行为标志（v3 新增）────────────────────────────
        [Header("速度配置")]
        [Range(0f, 30f)]
        [Tooltip("旋转平滑角速度。")]
        public float FaceToSpeed = 10f;

        [Tooltip("播放速度")]
        [Range(0f,10f)]
        public float PlaybackSpeed = 1f;

        /// <summary>
        /// 收集此动作上所有有效的统一路由（展开集合资产）。
        /// </summary>
        public void CollectEffectiveRoutes(List<ActionRoute> results)
        {
            if (results == null) return;
            results.Clear();

            if (Routes != null)
            {
                foreach (var route in Routes)
                {
                    if (route != null) results.Add(route);
                }
            }

            if (RouteSets != null)
            {
                foreach (var routeSet in RouteSets)
                {
                    routeSet?.AppendRoutes(results);
                }
            }
        }
    }

    /// <summary>
    /// 转向方式。
    /// </summary>
    public enum ActionTurnMode
    {
        None,
        InputDirection,
        EnemyPriorityThenInput
    }
}
