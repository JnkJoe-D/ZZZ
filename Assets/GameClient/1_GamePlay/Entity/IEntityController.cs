namespace Game.GamePlay
{
    /// <summary>
    /// 状态控制与路由决策中枢契约。
    /// 调度跨模块流转、指令路由解析与状态机流转。严禁继承 MonoBehaviour！
    /// </summary>
    public interface IEntityController
    {
        void Initialize(CharacterEntity owner);
        void LogicTick(float logicDeltaTime);
        void ResetController();
    }
    public static class EntityControllerFactory
    {
        public static T Create<T>(CharacterEntity owner) where T : IEntityController, new()
        {
            var controller = new T();
            controller.Initialize(owner);
            return controller;
        }
    }
}
