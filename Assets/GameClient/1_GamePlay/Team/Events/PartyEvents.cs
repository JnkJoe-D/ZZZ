using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 小队创建事件，通知表现层初始化共享虚拟相机等周边配置
    /// </summary>
    public struct PartyCreatedEvent : IGameEvent
    {
        public TeamConfigAsset TeamConfig;
        public RoleTeamContext TeamContext;
    }

    /// <summary>
    /// 小队成员生成事件，通知表现层（相机、HUD 等）绑定新生成的角色
    /// </summary>
    public struct PartyMemberSpawnedEvent : IGameEvent
    {
        public RoleEntity Entity;
    }

    /// <summary>
    /// 小队销毁/解散事件，通知表现层销毁共享虚拟相机及相关资源
    /// </summary>
    public struct PartyDestroyedEvent : IGameEvent
    {
    }
}
