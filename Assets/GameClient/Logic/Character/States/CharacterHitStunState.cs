using Game.FSM;

namespace Game.Logic.Character
{
    /// <summary>
    /// 受击硬直状态。角色在此状态期间无法执行任何输入动作。
    /// 通过 ActionPlayer 播放受击 Timeline，超时后回到 GroundState。
    /// </summary>
    public class CharacterHitStunState : CharacterStateBase
    {
        private float _stunTimer;
        private float _stunDuration;

        public override IInputCommandHandler InputHandler => NullInputHandler; // 受击中禁止输入

        public override void OnEnter()
        {
            Entity.RuntimeData.CurrentCommandContext = CommandContextType.HitStun;
            Entity.RuntimeData.ClearDashContinuation();
            _stunDuration = Entity.RuntimeData.CurrentHitStunDuration;
            _stunTimer = 0f;

            // 通过 ActionPlayer 播放受击动画
            if (Entity!=null && Entity.Config != null && Entity.Config.hitReactionConfig != null)
            {
                // TODO: 根据 hitDirection 选择前/后/左/右受击动画变体
                var hitAnim = Entity.Config.hitReactionConfig.hitAnimLight;
                if (hitAnim != null)
                {
                    Entity.ActionPlayer?.PlayAction(hitAnim);
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            _stunTimer += deltaTime;
            if (_stunTimer >= _stunDuration)
            {
                Machine.ChangeState<CharacterGroundState>();
            }
        }

        public override void OnExit()
        {
            // 确保受击结束后恢复 ActionPlayer 速度
            Entity.ActionPlayer?.SetPlaySpeed(1f);
            Entity.RuntimeData.ClearHitReactionAxis();
        }
    }
}
