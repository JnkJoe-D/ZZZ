using Game.Logic;
using Game.Input;

namespace Game.Logic
{
    public class ComboRouteRuntimeData : IEntityRuntimeData
    {
        public CommandRouteSource LastRouteSource { get; private set; }
        public string LastRouteTag { get; private set; }
        public ICommandPayload LastResolvedPayload { get; private set; }
        public int LastResolvedActionId { get; private set; } = -1;

        public void RecordResolvedRoute(
            CommandRouteSource routeSource,
            string routeTag,
            ICommandPayload payload,
            ActionConfigAsset action)
        {
            LastRouteSource = routeSource;
            LastRouteTag = routeTag;
            LastResolvedPayload = payload;
            LastResolvedActionId = action != null ? action.ID : -1;
        }

        public void Reset()
        {
            LastRouteSource = CommandRouteSource.None;
            LastRouteTag = null;
            LastResolvedPayload = null;
            LastResolvedActionId = -1;
        }
    }
}
