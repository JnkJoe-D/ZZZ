using Game.Logic.Action.Config;
using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundStopSubState : GroundSubState
    {
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DefaultInputCommandHandler(context.HostEntity);
        }

        private SkillEditor.SkillRunner _currentRunner;

        public override void OnEnter()
        {
            ActionConfigAsset playConfig = null;
            float lockTime = 0f;

            var config = _ctx.HostEntity.Config;
            if (config != null)
            {
                if (_ctx.Blackboard.IsFromDash)
                {
                    if (config.DashStopConfig != null)
                    {
                        playConfig = config.DashStopConfig;
                        lockTime = 0.5f; // 可以将这些转移到 LocomotionConfigSO，暂时写死或从旧配置拿
                    }
                    else if (config.JogStopConfig != null)
                    {
                        playConfig = config.JogStopConfig;
                        lockTime = 0.3f;
                    }
                }
                else
                {
                    if (config.JogStopConfig != null)
                    {
                        playConfig = config.JogStopConfig;
                        lockTime = 0.3f;
                    }
                }
            }

            if (playConfig != null)
            {
                // 设置推摇杆霸体保护（物理硬直）
                _ctx.SetMoveLock(lockTime);
                _currentRunner = _ctx.HostEntity.ActionPlayer.PlayAction(playConfig);
                if (_currentRunner != null) 
                {
                    _currentRunner.OnComplete -= OnStopAnimFinished;
                    _currentRunner.OnComplete += OnStopAnimFinished;
                }
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
                if (_currentRunner != null)
                {
                    _currentRunner.OnComplete -= OnStopAnimFinished;
                    _currentRunner = null;
                }

                if (provider.GetActionState(Game.Input.InputActionType.Dash))
                    ChangeState(_ctx.DashState);
                else
                    ChangeState(_ctx.JogState);
            }
        }

        private void OnStopAnimFinished()
        {
            if (_currentRunner != null)
            {
                _currentRunner.OnComplete -= OnStopAnimFinished;
                _currentRunner = null;
            }

            // 防抖保障：只有还在刹车态里才会自然过渡（因为如果中途推摇杆打断了退出该状态，回调再跑不应切 Idle）
            if (_ctx.CurrentSubState == this)
            {
                ChangeState(_ctx.IdleState);
            }
        }
    }
}
