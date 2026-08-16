using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using Game.Logic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Logic.AI.BehaviorTree
{
    [NodeColor("#a30cf5")]
    public class PlayBlackboardActionNode : MonsterTaskNode
    {
        private string _blackboardKey;
        public PlayBlackboardActionNode() {}
        public PlayBlackboardActionNode(AIContext context, string blackboardKey) : base(context)
        {
            _blackboardKey = blackboardKey;
        }
    
    protected override void DoStart()
    {
        var action = Tree.blackboard.Get<MonsterActionConfigAsset>(_blackboardKey);
        if (action == null || Context.Owner == null || Context.Owner.ActionPlayer == null)
        {
            Stopped(false);
            return;
        }
        
        bool success = Context.Owner.ActionPlayer.PlayAction(action);
        if (success)
        {
            Context.Owner.ActionPlayer.OnActionComplete += OnComplete;
            Context.Owner.ActionPlayer.OnActionInterrupt += OnInterrupt;
        }
        else
        {
            Stopped(false);
        }
    }
    
    private void OnComplete()
    {
        if (Context.Owner != null && Context.Owner.ActionPlayer != null)
        {
            Context.Owner.ActionPlayer.OnActionComplete -= OnComplete;
            Context.Owner.ActionPlayer.OnActionInterrupt -= OnInterrupt;
        }
        Stopped(true);
    }
    
    private void OnInterrupt()
    {
        if (Context.Owner != null && Context.Owner.ActionPlayer != null)
        {
            Context.Owner.ActionPlayer.OnActionComplete -= OnComplete;
            Context.Owner.ActionPlayer.OnActionInterrupt -= OnInterrupt;
        }
        Stopped(false);
    }
    
    protected override void DoStop()
    {
        if (Context.Owner != null && Context.Owner.ActionPlayer != null)
        {
            Context.Owner.ActionPlayer.OnActionComplete -= OnComplete;
            Context.Owner.ActionPlayer.OnActionInterrupt -= OnInterrupt;
            Context.Owner.ActionPlayer.StopAction();
        }
        Stopped(false);
    }
}
}
