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

        [Header("派生指令")]
        public List<LocalActionRoute> LocalRoutes = new();

        [Header("派生指令集")]
        public List<LocalRouteSetAsset> LocalRouteSets = new();

        public void CollectEffectiveLocalRoutes(List<LocalActionRoute> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            if (LocalRoutes != null)
            {
                foreach (LocalActionRoute route in LocalRoutes)
                {
                    if (route != null)
                    {
                        results.Add(route);
                    }
                }
            }

            if (LocalRouteSets == null)
            {
                return;
            }

            foreach (LocalRouteSetAsset routeSet in LocalRouteSets)
            {
                routeSet?.AppendRoutes(results);
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
