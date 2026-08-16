using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#f50c0c")]
    public class DynamicInterrupt : DecoratorNode
{
    private System.Func<bool> _interruptCondition;
    
    public DynamicInterrupt(System.Func<bool> interruptCondition, Node decoratee) : base(decoratee)
    {
        _interruptCondition = interruptCondition;
    }

    protected override void DoStart()
    {
        Tree.blackboard.Clock.AddTimer(0.1f, 0, -1, CheckInterrupt);
        base.DoStart();
    }

    protected override void DoStop()
    {
        Tree.blackboard.Clock.RemoveTimer(CheckInterrupt);
        base.DoStop();
    }

    public override void ChildStopped(Node child, bool success)
    {
        Tree.blackboard.Clock.RemoveTimer(CheckInterrupt);
        base.ChildStopped(child, success);
    }

    private void CheckInterrupt()
    {
        if (CurrentState == NodeState.Active && _interruptCondition != null && _interruptCondition())
        {
            float dist = Tree.blackboard.Get<float>("Distance");
            Debug.Log($"[BT Interrupt] Condition met! Interrupting {Decoratee.GetType().Name}. Current Distance: {dist}");
            Decoratee.Stop();
        }
    }
}
}
