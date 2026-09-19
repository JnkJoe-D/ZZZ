namespace Game.GamePlay
{
    /// <summary>
    /// 局内大盘调度系统统一契约（如 TeamManager, MonsterManager, WaveManager）。
    /// </summary>
    public interface IBattleManager
    {
        void Initialize();
        void Shutdown();
        void Reset();
    }
}
