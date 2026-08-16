using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#0c42f5")]
    public class SelectAttackNode : LeafNode
{
    private List<AttackActionConfig> _attacks;
    
    public SelectAttackNode(List<AttackActionConfig> attacks)
    {
        _attacks = attacks;
    }
    
    protected override void DoStart()
    {
        if (_attacks == null || _attacks.Count == 0)
        {
            Stopped(false);
            return;
        }
        
        // 简单的等概率随机（可以根据 weight 升级成带权随机）
        var chosen = _attacks[Random.Range(0, _attacks.Count)];
        Tree.blackboard.Set("SelectedAttackAction", chosen.action);
        Tree.blackboard.Set("OptimalAttackDistance", chosen.attackDistance);
        Stopped(true);
    }
    protected override void DoStop() { }
}




        [System.Serializable]
        public struct AttackActionConfig
        {
            public MonsterActionConfigAsset action;
            [Tooltip("该攻击动作的最佳释放距离")]
            public float attackDistance;
            [Tooltip("攻击随机权重")]
            public int weight;
        }
}
