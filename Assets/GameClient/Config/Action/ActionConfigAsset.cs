using System.Collections.Generic;
using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Action.Config
{
    /// <summary>
    /// 全局动作配置基类。
    /// 用于描述任意动作的基础信息，并关联 SkillTimeline 资产。
    /// </summary>
    public abstract class ActionConfigAsset : ScriptableObject
    {
        [Header("Base Info")]
        public int ID;
        public string Name;

        [Header("Skill Editor Asset")]
        [Tooltip("SkillEditor 生成的时间轴数据。")]
        public TextAsset TimelineAsset;

        [Header("Turn Priority")]
        public ActionTurnMode TurnMode = ActionTurnMode.InputDirection;

        [Header("Local Routes")]
        [Tooltip("当前动作自己的局部派生路由，例如普攻连段、冲刺普攻、闪避取消。")]
        public List<LocalActionRoute> LocalRoutes = new();
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
