using Game.Input;
using UnityEngine;

namespace Game.Logic.Character.SubStates
{
    public class GroundDashSubState : GroundSubState
    {
        private IInputCommandHandler _handler;
        public override IInputCommandHandler InputHandler => _handler;

        public override void Initialize(CharacterGroundState context)
        {
            base.Initialize(context);
            _handler = new DashInputCommandHandler(context.HostEntity);
        }

        private bool _hasPlayedAnim;
        public override void OnEnter()
        {
            _hasPlayedAnim = false;
            DoDash();
        }

        public override void OnUpdate(float deltaTime)
        {
            var provider = DoDash();
            if(provider==null)return;
            // 3. 强力冲刺移动
            Vector2 inputDir = provider.GetMovementDirection();
            Vector3 worldDir = _ctx.CalculateWorldDirection(inputDir);

            _ctx.HostEntity.MovementController?.Move(worldDir * _ctx.DashSpeed * deltaTime);
            _ctx.HostEntity.MovementController?.FaceTo(worldDir);
            
            // 实时速率同步（可选，因为已经在 OnEnter 里设置过了，但如果配置热更有改变就更新）
            _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.DashMultipier);
        }
        IInputProvider DoDash()
        {
            var provider = _ctx.HostEntity.InputProvider;
            if (provider == null) return null;

            // 1. 松摇杆触发物理急停
            if (!provider.HasMovementInput())
            {
                _ctx.Blackboard.IsFromDash = true;
                ChangeState(_ctx.StopState);
                return null;
            }
            if(_hasPlayedAnim) return provider;
            
            // 如果存在起步配置，先播起步（暂时不实现复杂的起步接循环，直接播循环，后续通过Timeline自身控制）
            // 目前简化为直接播 DashConfig
            if (_ctx.HostEntity.Config.DashConfig != null)
            {
                _ctx.HostEntity.ActionPlayer.PlayAction(_ctx.HostEntity.Config.DashConfig);
                _ctx.HostEntity.ActionPlayer.SetPlaySpeed(_ctx.HostEntity.Config.DashMultipier);
            }
            
            _hasPlayedAnim=true;
            return provider;
        }
    }
}
