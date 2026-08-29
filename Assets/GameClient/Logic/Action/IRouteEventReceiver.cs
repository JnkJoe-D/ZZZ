using Game.Logic;

namespace Game.Logic
{
    public interface IRouteEventReceiver
    {
        void OnRouteEventExecuted(ExecuteEvent routeExecuteEvent, CharacterEntity entity);
    }
}
