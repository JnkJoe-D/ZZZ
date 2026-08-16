using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#f5900c")]
    public class RandomSelector : CompositeNode
{
    public RandomSelector(params Node[] children) : base(children) { }

    protected override void DoStart()
    {
        if (Children.Count == 0) { Stopped(false); return; }
        int r = Random.Range(0, Children.Count);
        Children[r].Start();
    }

    protected override void DoStop()
    {
        foreach (var child in Children)
        {
            if (child.CurrentState == NodeState.Active) child.Stop();
        }
    }

    public override void ChildStopped(Node child, bool success)
    {
        Stopped(success);
    }
}
}
