using UnityEngine;
using Game.Logic.Action.Combo;

namespace Game.Logic.Action.Config
{
    /// <summary>
    /// 全局行为配置基类
    /// 用于配置任意动作参数（移动，技能，受击等），并关联 SkillTimeline 资产
    /// </summary>
    public abstract class ActionConfigSO : ScriptableObject
    {
        [Header("Base Info")]
        public int ID;
        public string Name;

        [Header("Skill Editor Asset")]
        [Tooltip("技能编辑器产出的时间轴数据")]
        public TextAsset TimelineAsset;

        [Header("转向优先级")]
        public ActionTurnMode TurnMode = ActionTurnMode.InputDirection;

        [Header("派生与连段出口")]
        [Tooltip("根据玩家输入的指令优先级匹配跳转栈（可配置特殊技、强化攻击、重击等派生）")]
        public System.Collections.Generic.List<ComboTransition> OutTransitions = new System.Collections.Generic.List<ComboTransition>();
    }
    /// <summary>
    /// 转向方式
    /// </summary>
    public enum ActionTurnMode
    {
        ///<!--不转向-->
        None,
        ///<!--朝当前移动方向转向-->
        InputDirection,
        ///<!--优先朝敌人转向其次移动方向-->
        EnemyPriorityThenInput
    }
}
