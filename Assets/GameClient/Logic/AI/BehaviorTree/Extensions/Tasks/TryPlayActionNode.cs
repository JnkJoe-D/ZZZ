using Game.Logic;
using ATEditor;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    public class TryPlayActionNode : MonsterTaskNode
    {
        [SerializeField] private MonsterActionConfigAsset _actionAsset;

        public TryPlayActionNode() {}
        public TryPlayActionNode(AIContext context, MonsterActionConfigAsset actionAsset) : base(context)
        {
            _actionAsset = actionAsset;
        }

        protected override void DoStart()
        {
            // 校验阶段：这里检查怪物是否可以执行动作
            // 实际开发中可以通过 Context 去访问 Validator 或者是 StatusModule。
            // 简单示例：如果实体死掉，就不播放直接返回 false。
            if (Context.Owner == null || Context.Owner.ActionPlayer == null)
            {
                Stopped(false);
                return;
            }

            // (可选) TODO: 补充具体的失衡、硬直等异常状态检验
            // if (Context.Owner.StatusModule.HasStatus(StatusType.Stunned))
            // {
            //     Stopped(false);
            //     return;
            // }

            bool success = Context.Owner.ActionPlayer.PlayAction(_actionAsset);
            
            if (success)
            {
                Context.Owner.ActionPlayer.OnActionComplete += OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt += OnActionInterrupt;
            }
            else
            {
                // 如果 PlayAction 失败，直接视为任务失败
                Stopped(false);
            }
        }

        protected override void DoStop()
        {
            if (Context.Owner != null && Context.Owner.ActionPlayer != null)
            {
                Context.Owner.ActionPlayer.OnActionComplete -= OnActionComplete;
                Context.Owner.ActionPlayer.OnActionInterrupt -= OnActionInterrupt;
                
                // 由行为树框架强制打断动作
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
