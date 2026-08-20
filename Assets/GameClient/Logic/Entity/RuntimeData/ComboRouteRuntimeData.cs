using Game.Logic;
using Game.Input;

namespace Game.Logic
{
    public class ComboRouteRuntimeData : IEntityRuntimeData
    {
        public CommandRouteSource LastRouteSource { get; private set; }
        public string LastRouteTag { get; private set; }
        public InputCommand LastResolvedCommandType { get; private set; }
        public CommandPhase LastResolvedCommandPhase { get; private set; }
        public int LastResolvedActionId { get; private set; } = -1;

        public void RecordResolvedRoute(
            CommandRouteSource routeSource,
            string routeTag,
            InputCommand commandType,
            CommandPhase commandPhase,
            ActionConfigAsset action)
        {
            LastRouteSource = routeSource;
            LastRouteTag = routeTag;
            LastResolvedCommandType = commandType;
            LastResolvedCommandPhase = commandPhase;
            LastResolvedActionId = action != null ? action.ID : -1;
        }

        public void Reset()
        {
            LastRouteSource = CommandRouteSource.None;
            LastRouteTag = null;
            LastResolvedCommandType = InputCommand.None;
            LastResolvedCommandPhase = CommandPhase.Started;
            LastResolvedActionId = -1;
        }
    }
}
