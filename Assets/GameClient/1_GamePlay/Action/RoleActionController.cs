 

namespace Game.GamePlay
{
    public class RoleActionController : ActionController
    {
        private RoleEntity Role => (RoleEntity)_entity;

        private ComboRouteRuntimeData _comboData;

        public RoleActionController(RoleEntity entity, 
                                    IRouteEventReceiver receiver = null,
                                    ISkillCostHandler skillCostHandler = null) 
            : base(entity, 
                   receiver ?? new RoleRouteEventReceiver(),
                   skillCostHandler ?? new DefaultSkillCostHandler())
        {
            _comboData = _entity.DataModule?.Get<ComboRouteRuntimeData>();
        }

        protected override RoleEntity GetRouteEvalActor() => Role;

        protected override void OnActionPlaySucceed(ActionConfigAsset action)
        {
            if (Role.StateMachine == null) return;
            
            if (action is RoleActionConfigAsset roleAction)
            {
                switch (roleAction.EnterState)
                {
                    case ActionState.Idle:
                    case ActionState.Jog:
                    case ActionState.Dash:
                    case ActionState.Stop:
                        if (_actionData != null)
                            _actionData.Set(nameof(_actionData.TargetGroundSubState), roleAction.EnterState);
                        Role.StateMachine.ChangeState<CharacterGroundState>();
                        break;
                    case ActionState.Skill:
                        Role.StateMachine.ChangeState<CharacterSkillState>();
                        break;
                    case ActionState.Evade:
                        Role.StateMachine.ChangeState<CharacterEvadeState>();
                        break;
                    case ActionState.Hit:
                        Role.StateMachine.ChangeState<CharacterHitStunState>();
                        break;
                    case ActionState.Switch:
                        Role.StateMachine.ChangeState<CharacterSwitchState>();
                        break;
                    case ActionState.Parry:
                        Role.StateMachine.ChangeState<CharacterParryState>();
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
