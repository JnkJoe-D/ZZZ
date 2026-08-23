using Game.FSM;
using Game.Input;
using ATEditor;

namespace Game.Logic
{
    public class RoleActionController : ActionController
    {
        private RoleEntity Role => (RoleEntity)_entity;

        private ComboRouteRuntimeData _comboData;

        public RoleActionController(RoleEntity entity) : base(entity)
        {
            _comboData = _entity.DataModule?.Get<ComboRouteRuntimeData>();
        }

        protected override RoleEntity GetRouteEvalActor() => Role;

        protected override void OnActionStateSwitch(ActionConfigAsset action)
        {
            if (Role.Machine == null) return;
            
            if (action is RoleActionConfigAsset roleAction)
            {
                switch (roleAction.EnterState)
                {
                    case ActionState.Idle:
                    case ActionState.Jog:
                    case ActionState.Dash:
                    case ActionState.Stop:
                        if (_actionData != null)
                            _actionData.TargetGroundSubState = roleAction.EnterState;
                        Role.Machine.ChangeState<CharacterGroundState>();
                        break;
                    case ActionState.Skill:
                        Role.Machine.ChangeState<CharacterSkillState>();
                        break;
                    case ActionState.Evade:
                        Role.Machine.ChangeState<CharacterEvadeState>();
                        break;
                    case ActionState.Hit:
                        Role.Machine.ChangeState<CharacterHitStunState>();
                        break;
                    case ActionState.Switch:
                        Role.Machine.ChangeState<CharacterSwitchState>();
                        break;
                }
            }
        }

        protected override void OnRouteEventCommit(ExecuteEvent routeExecuteEvent)
        {
            if(routeExecuteEvent == ExecuteEvent.SwitchCaptureSucceed)
            {
                Game.Framework.EventCenter.Publish(new ActionRouteExecuteEvent
                {
                    SourceEntity = Role,
                    Event = routeExecuteEvent,
                    TargetSlotHint = -1
                });
            }
        }

        protected override void RecordComboRoute(CommandRouteSource source, string tag, ICommandPayload payload, ActionConfigAsset action)
        {
            _comboData?.RecordResolvedRoute(source, tag, payload, action);
        }
    }
}
