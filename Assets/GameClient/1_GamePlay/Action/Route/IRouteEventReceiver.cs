using Game.GamePlay;

namespace Game.GamePlay
{
    public interface IRouteEventReceiver
    {
        void OnRouteEventExecuted(ExecuteEvent routeExecuteEvent, CharacterEntity entity);
    }
}
