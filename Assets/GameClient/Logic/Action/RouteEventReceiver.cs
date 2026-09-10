using ATEditor;

namespace Game.Logic
{
    public class DefaultRouteEventReceiver : IRouteEventReceiver
    {
        public void OnRouteEventExecuted(ExecuteEvent routeExecuteEvent, CharacterEntity entity)
        {
            if (entity == null || entity.ActionPlayer == null) return;

            if (routeExecuteEvent == ExecuteEvent.TimelineRewind)
            {
                entity.ActionPlayer?.SendTimelineMessage(ExecuteEvent.TimelineRewind.ToString());
                return;
            }
            if(routeExecuteEvent == ExecuteEvent.TimelineSkip)
            {
                entity.ActionPlayer?.SetTimelineFlag(ExecuteEvent.TimelineSkip.ToString());
                return;
            }
        }
    }
    public class RoleRouteEventReceiver : IRouteEventReceiver
    {
        public void OnRouteEventExecuted(ExecuteEvent routeExecuteEvent, CharacterEntity entity)
        {
            if (entity == null || entity.ActionPlayer == null) return;

            if (routeExecuteEvent == ExecuteEvent.TimelineRewind)
            {
                entity.ActionPlayer?.SendTimelineMessage(ExecuteEvent.TimelineRewind.ToString());
                return;
            }
            if (routeExecuteEvent == ExecuteEvent.TimelineSkip)
            {
                entity.ActionPlayer?.SetTimelineFlag(ExecuteEvent.TimelineSkip.ToString());
                return;
            }
            if (routeExecuteEvent == ExecuteEvent.SwitchCaptureSucceed ||
                routeExecuteEvent == ExecuteEvent.ParryAidStart ||
                routeExecuteEvent == ExecuteEvent.EvasionAidStart ||
                routeExecuteEvent == ExecuteEvent.ChainAttackStart ||
                routeExecuteEvent == ExecuteEvent.QuickAidStart)
            {
                Game.Framework.EventCenter.Publish(new ActionRouteExecuteEvent
                {
                    SourceEntity = entity as RoleEntity,
                    Event = routeExecuteEvent,
                    TargetSlotHint = -1
                });
            }
        }
    }
    public class MonsterRouteEventReceiver : IRouteEventReceiver
    {
        public void OnRouteEventExecuted(ExecuteEvent routeExecuteEvent, CharacterEntity entity)
        {
            if (entity == null || entity.ActionPlayer == null) return;
        }
    }
}
