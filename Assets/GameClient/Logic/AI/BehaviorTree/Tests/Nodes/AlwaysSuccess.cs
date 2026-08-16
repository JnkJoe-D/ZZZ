using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
[NodeColor("#205b00")]
public class AlwaysSuccess : DecoratorNode
{
    public AlwaysSuccess(Node decoratee) : base(decoratee) { }
    protected override void DoStart() { Decoratee.Start(); }
    protected override void DoStop() { Decoratee.Stop(); }
    public override void ChildStopped(Node child, bool success) { Stopped(true); }
}
}
