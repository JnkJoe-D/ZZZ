using Game.FSM;

namespace Game.Logic
{
    /// <summary>
    /// 受击硬直状态。角色在此状态期间无法执行任何输入动作�?
    /// 通过 ActionPlayer 播放受击 Timeline，超时后回到 GroundState�?
    /// </summary>
    public class CharacterHitStunState : CharacterStateBase
    {
        private float _stunTimer;
        private float _stunDuration;

        public override IInputCommandHandler InputHandler => NullInputHandler; // 受击中禁止输�?

        public override void OnEnter()
        {
            base.OnEnter();
            
            _stunDuration = Entity.HitData.CurrentHitStunDuration;
            _stunTimer = 0f;

            // 通过 ActionPlayer 播放受击动画
            if (Entity != null && Entity.Config != null && Entity.Config.hitReactionConfig != null)
            {
                var reactType = Entity.HitData.CurrentReactionType;
                ActionConfigAsset hitAnim = null;

                switch (reactType)
                {
                    case cfg.ZZZ.HitReactionType.Light:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimLight;
                        break;
                    case cfg.ZZZ.HitReactionType.Heavy:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimHeavy;
                        break;
                    case cfg.ZZZ.HitReactionType.Stay:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimStay;
                        break;
                    case cfg.ZZZ.HitReactionType.Launch:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimKnowAway;
                        break;
                    case cfg.ZZZ.HitReactionType.KnockDown:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimKnockDown;
                        break;
                    case cfg.ZZZ.HitReactionType.Shake:
                        hitAnim = Entity.Config.hitReactionConfig.hitAnimShake;
                        break;
                }

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
            // 确保受击结束后恢�?ActionPlayer 速度
            Entity.ActionPlayer?.SetPlaySpeed(1f);
            Entity.HitData.ClearHitReactionAxis();
        }
    }
}
