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
}
