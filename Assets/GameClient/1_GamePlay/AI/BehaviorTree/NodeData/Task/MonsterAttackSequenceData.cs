using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay 
{
    [Serializable]
    public class MonsterAttackEntry
    {
        [Tooltip("攻击招式动作配置资产")]
        public ActionConfigAsset action;

        [Tooltip("该招式的有效攻击/命中距离 (米)，例如三连击为 3.2m，冲刺突刺为 8.0m")]
        public float effectiveRange = 3.5f;

        [Tooltip("招式调试描述信息")]
        public string description;
    }

    /// <summary>
    /// 怪物智能记忆连招序列器节点数据。
    /// 直接配置连招招式序列及其独立有效射程，由行为树自主推进并更新黑板。
    /// </summary>
    public class MonsterAttackSequenceData : TaskData
    {
        [Tooltip("连招套路招式序列")]
        public List<MonsterAttackEntry> sequence = new List<MonsterAttackEntry>();

        [Tooltip("整套连招循环打完后的公共攻击间隔 (秒)")]
        public float attackInterval = 2.5f;
    }
}
