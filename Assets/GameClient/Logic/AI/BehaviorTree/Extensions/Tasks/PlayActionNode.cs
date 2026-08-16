using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class PlayActionNode : MonsterTaskNode
    {
        [SerializeField] private MonsterActionConfigAsset _actionAsset;

        public PlayActionNode() {}
        public PlayActionNode(AIContext context, MonsterActionConfigAsset actionAsset) : base(context)
        {
            _actionAsset = actionAsset;
        }

        protected override void DoStart()
        {
            // 不进行前置状态校验，直接强制下发动作指令
            if (Context.Owner == null || Context.Owner.ActionPlayer == null)
            {
                Stopped(false);
                return;
            }

            bool success = Context.Owner.ActionPlayer.PlayAction(_actionAsset);
            
            if (success)
            {
                Context.Owner.ActionPlayer.OnActionComplete += OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt += OnActionInterrupt;
            }
            else
            {
                Stopped(false);
            }
        }

        protected override void DoStop()
        {
            if (Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.OnActionComplete -= OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt -= OnActionInterrupt;
                
                Context.Owner.ActionPlayer.StopAction();
            }
            
            Stopped(false);
        }

        private void OnActionComplete()
        {
            if (Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.OnActionComplete -= OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt -= OnActionInterrupt;
            }
            
            if (CurrentState == NodeState.Active)
            {
                Stopped(true);
            }
        }

        private void OnActionInterrupt()
        {
            if (Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.OnActionComplete -= OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt -= OnActionInterrupt;
            }
            
            if (CurrentState == NodeState.Active)
            {
                Stopped(false);
            }
        }
    }
}
