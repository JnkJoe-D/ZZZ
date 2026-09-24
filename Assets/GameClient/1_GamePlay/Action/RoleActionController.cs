 

namespace Game.GamePlay
{
    public class RoleActionController : ActionController
    {
        private RoleEntity Role => (RoleEntity)_entity;

        private ComboRouteRuntimeData _comboData;

        public override void Initialize(CharacterEntity owner)
        {
            base.Initialize(owner);
            _routeEventReceiver ??= new RoleRouteEventReceiver();
            _skillCostHandler ??= new DefaultSkillCostHandler();
            _comboData = _entity.DataModule?.Get<ComboRouteRuntimeData>();
        }

        protected override CharacterEntity GetRouteEvalActor() => Role;

        protected override void OnActionPlaySucceed(ActionConfigAsset action)
        {
            if (Role.StateMachine == null) return;
            
            if (action is RoleActionConfigAsset roleAction)
            {
                switch (roleAction.EnterState)
                {
                    case ActionState.Idle:
                    case ActionState.Walk:
                    case ActionState.Run:
                    case ActionState.Stop:
                        if (_actionData != null)
                            _actionData.Set(nameof(_actionData.TargetGroundSubState), roleAction.EnterState);
                        Role.StateMachine.ChangeState<RoleGroundState>();
                        break;
                    case ActionState.Skill:
                        Role.StateMachine.ChangeState<RoleSkillState>();
                        break;
                    case ActionState.Evade:
                        Role.StateMachine.ChangeState<RoleEvadeState>();
                        break;
                    case ActionState.Hit:
                        Role.StateMachine.ChangeState<RoleHitStunState>();
                        break;
                    case ActionState.Switch:
                        Role.StateMachine.ChangeState<CharacterSwitchState>();
                        break;
                    case ActionState.Parry:
                        Role.StateMachine.ChangeState<RoleParryState>();
                        break;
                }
            }
        }

        protected override void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action)
        {
            _comboData?.RecordResolvedRoute(source, tag, payload, action);
        }
    }
}
