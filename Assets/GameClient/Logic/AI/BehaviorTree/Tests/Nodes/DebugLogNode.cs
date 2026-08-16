using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#f0f3ec")]
    public class DebugLogNode : LeafNode
{
    private string _msg;
    public DebugLogNode(string msg) { _msg = msg; }
    protected override void DoStart() { Debug.Log("[BT Debug] " + _msg); Stopped(true); }
    protected override void DoStop() { }
}
}
