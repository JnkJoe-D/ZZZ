using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
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
        Switch = 70,    // CharacterSwitchState
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
    /// 当收集有效路由时，当前动作对于前置动作路由的继承策略。
    /// </summary>
    public enum RouteInheritMode
    {
        [InspectorName("不继承 (None)")]
        None = 0,
        [InspectorName("继承 (继承优先于自身) (InheritPrioritizeInherited)")]
        InheritPrioritizeInherited = 10,
        [InspectorName("继承 (自身优先于继承) (InheritPrioritizeSelf)")]
        InheritPrioritizeSelf = 20,
        [InspectorName("继承 (完全覆盖自身) (InheritAndOverrideSelf)")]
        InheritAndOverrideSelf = 30,
    }

    /// <summary>
    /// 全局动作配置基类。
    /// 用于描述任意动作的基础信息，并关联 ActionTimeline 资产。
    /// </summary>
    /// <summary>
    /// 全局动作配置基类（混合态架构数据载体）
    /// 承载了 Timeline 资源引用、派生路由配置、以及状态机回流提示。
    /// </summary>
    public abstract class ActionConfigAsset : GameConfigAsset
    {
        [Header("基础信息")]
        public int ID;
        public string Name;

        [Header("SkillEditor 核心资产")]
        [Tooltip("SkillEditor 生成的标准化时间轴数据，ActionPlayer 会解析并播放它。")]
        public TextAsset TimelineAsset;
        [Tooltip("SkillEditor 生成的 ScriptableObject 格式时间轴数据（支持运行时热更），ActionPlayer 会优先解析此资源。")]
        public ATEditor.ActionTimeline actionTimelineSO;



        [Header("状态转换")]
        [Tooltip("指定此动作执行时的目标状态机主状态/子状态。")]
        public ActionState EnterState = ActionState.Idle;

        [Tooltip("动作正常完成后的后续转换策略。")]
        public ActionCompleteMode CompleteMode = ActionCompleteMode.Default;

        [Tooltip("当 CompleteMode 设为 TransitToAction 时，自动衔接的这个后续动作。")]
        public ActionConfigAsset CompleteAction;

        [Game.Framework.ShowIf("CompleteMode", ActionCompleteMode.TransitToAction)]
        [Tooltip("-1表示使用下个动作自身设定的混合时间，>=0则强制覆盖混合时间。")]
        public float CompleteTransitCrossfade = -1f;

        // ── 速度配置 ────────────────────────────
        [Header("速度配置")]


        [Tooltip("播放速度")]
        [Range(0f,10f)]
        public float PlaybackSpeed = 1f;

        /// <summary>
        /// 收集此动作上所有有效的统一路由（展开集合资产）。
        /// 基础动作不具备连招路由，仅做空实现，由派生类（如 RoleActionConfigAsset）实现具体逻辑。
        /// </summary>
        public virtual void CollectEffectiveRoutes(List<ActionRoute> results, CharacterEntity actor = null)
        {
        }
    }


}
