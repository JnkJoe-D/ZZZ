namespace Game.Framework
{
    /// <summary>
    /// 游戏事件标记接口
    /// 所有事件类型必须实现此接口，推荐使用 struct 以避免 GC
    /// 
    /// 使用示例：
    ///   public struct PlayerDiedEvent : IGameEvent
    ///   {
    ///       public int PlayerId;
    ///       public Vector3 DeathPosition;
    ///   }
    ///
    ///   // 发布
    ///   EventCenter.Publish(new PlayerDiedEvent { PlayerId = 1, DeathPosition = pos });
    ///   
    ///   // 订阅
    ///   EventCenter.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    ///   private void OnPlayerDied(PlayerDiedEvent e) { ... }
    /// </summary>
    public interface IGameEvent { }
}
