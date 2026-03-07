using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundStopSubState : GroundSubState
    {
        public override void OnEnter()
        {
            AnimationClip playClip = null;
            float fadeDuration = 0;
            float lockTime = 0f;

            var animSet = _ctx.HostEntity.CurrentAnimSet;
            if (animSet != null)
            {
                if (_ctx.Blackboard.IsFromDash)
                {
                    if (_ctx.Blackboard.IsDashStable && animSet.DashStop.clip != null)
                    {
                        playClip = animSet.DashStop.clip;
                        fadeDuration = animSet.DashStop.fadeDuration;
                        lockTime = animSet.DashStopLockTime;
                    }
                    else if (animSet.JogStop != null)
                    {
                        playClip = animSet.JogStop.clip;
                        fadeDuration = animSet.JogStop.fadeDuration;
                        lockTime = animSet.JogStopLockTime;
                    }
                }
                else
                {
                    if (animSet.JogStop != null)
                    {
                        playClip = animSet.JogStop.clip;
                        fadeDuration = animSet.JogStop.fadeDuration;
                        lockTime = animSet.JogStopLockTime;
                    }
                }
            }

            if (playClip != null)
            {
                // 设置推摇杆霸体保护（物理硬直）
                _ctx.SetMoveLock(lockTime);
                _ctx.HostEntity.AnimController?.PlayAnim(playClip, 0.2f, null, OnStopAnimFinished, forceResetTime: true);
            }
            else
            {
                _ctx.ClearMoveLock();
                OnStopAnimFinished();
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return;

            // 刹车期间如果物理硬直已过，只要玩家推摇杆便一刀斩断后摇，立刻切去新步态（打断动画）
            if (provider.HasMovementInput() && !_ctx.IsMoveLocked)
            {
                if (provider.GetActionState(Game.Input.InputActionType.Dash))
                    ChangeState(_ctx.DashState);
                else
                    ChangeState(_ctx.JogState);
            }
        }

        private void OnStopAnimFinished()
        {
            // 防抖保障：只有还在刹车态里才会自然过渡（因为如果中途推摇杆打断了退出该状态，回调再跑不应切 Idle）
            if (_ctx.CurrentSubState == this)
            {
                ChangeState(_ctx.IdleState);
            }
        }
    }
}
