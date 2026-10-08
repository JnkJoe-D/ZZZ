namespace Game.GamePlay
{
    /// <summary>
    /// 角色受击硬直与失衡领域。
    /// 承载受击硬直计时、轴向位移衰减以及受击期间的输入阻断防护。
    /// </summary>
    public class RoleHitReactionDomain : ActionDomain
    {
        public override ActionDomainId DomainId => ActionDomainId.HitReaction;

        private float _stunTimer;
        private float _stunDuration;
        private HitReactionRuntimeData _hitData;

        public float RemainingStunDuration => UnityEngine.Mathf.Max(0f, _stunDuration - _stunTimer);
        public bool IsStunFinished => _stunTimer >= _stunDuration;

        public override void Initialize(CharacterEntity entity, ActionDomainContextModule context)
        {
            base.Initialize(entity, context);
            _hitData = entity?.DataModule?.Get<HitReactionRuntimeData>();
        }

        public override void OnEnter(ActionConfigAsset newAction, ActionDomain previousDomain)
        {
            base.OnEnter(newAction, previousDomain);
            _hitData ??= Entity?.DataModule?.Get<HitReactionRuntimeData>();

            _stunDuration = _hitData != null && _hitData.CurrentHitStunDuration > 0f
                ? _hitData.CurrentHitStunDuration
                : 0.5f;
            _stunTimer = 0f;
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            _stunTimer += deltaTime;
            // 硬直自然衰减仅记录时间供 IsStunFinished 查询；
            // 动作回流完全遵从受击动作自身的 CompleteMode（自然过渡回 Idle），避免提前掐断动画导致的抽搐
        }

        public override void OnExit(ActionDomain nextDomain)
        {
            base.OnExit(nextDomain);
            _hitData?.ClearHitReactionAxis();
        }

        public override bool CanAcceptCommand(CharacterCommand command)
        {
            // 受击硬直期间禁止普通连招与移动指令（特定极限反击或换人指令由外部紧急仲裁拦截）
            return false;
        }
    }
}
