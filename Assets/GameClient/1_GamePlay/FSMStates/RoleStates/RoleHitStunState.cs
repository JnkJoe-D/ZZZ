using Game.Framework;

namespace Game.GamePlay
{
    public class RoleHitStunState : RoleStateBase
    {
        private float _stunTimer;
        private float _stunDuration;
        private HitReactionRuntimeData _hitData;
        public override IActionCommandHandler InputHandler => NullInputHandler;

        public override void OnEnter()
        {
            base.OnEnter();
            _hitData = Entity.DataModule?.Get<HitReactionRuntimeData>();
            if (_hitData != null)
            {
                _stunDuration = _hitData.CurrentHitStunDuration;
            }
            else
            {
                _stunDuration = 0.5f;
            }
            _stunTimer = 0f;
        }

        public override void OnUpdate(float deltaTime)
        {
            _stunTimer += deltaTime;
            if (_stunTimer >= _stunDuration)
            {
                Machine.ChangeState<RoleGroundState>();
            }
        }

        public override void OnExit()
        {
            _hitData?.ClearHitReactionAxis();
        }
    }
}
