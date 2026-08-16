using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#effb01")]
    public class CustomAction : LeafNode
{
    private System.Func<bool> _actionFunc;
    public CustomAction(System.Func<bool> actionFunc) { _actionFunc = actionFunc; }
    protected override void DoStart() { Stopped(_actionFunc != null && _actionFunc()); }
    protected override void DoStop() { }
}
}
