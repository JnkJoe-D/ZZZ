

namespace Game.GamePlay
{
    public class ComboRouteRuntimeData : EntityRuntimeDataBase
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

        public override void Reset()
        {
            base.Reset();
            LastRouteSource = CommandRouteSource.None;
            LastRouteTag = null;
            LastResolvedPayload = null;
            LastResolvedActionId = -1;
        }
    }
}
