namespace Game.Framework
{
    /// <summary>
    /// 消息ID定义（与服务端 MsgId.cs 完全同步）
    ///
    /// 范围规划:
    ///   0x0001 - 0x00FF: 系统消息（心跳、错误等）
    ///   0x0100 - 0x01FF: 登录/认证
    ///   0x0200 - 0x02FF: 角色管理
    ///   0x0300 - 0x03FF: 大世界/场景
    ///   0x0400 - 0x04FF: 房间/战斗
    ///   0x0500 - 0x05FF: 社交
    ///   0x0600 - 0x06FF: 背包/道具
    ///   0x0700 - 0x07FF: 任务/成就
    /// </summary>
    public static class MsgId
    {
        // ========== 系统消息 ==========
        public const ushort Heartbeat       = 0x0001;
        public const ushort Error           = 0x0002;

        // ========== 登录/认证 ==========
        public const ushort Login           = 0x0100;
        public const ushort Register        = 0x0101;
        public const ushort Logout          = 0x0102;
        public const ushort Reconnect       = 0x0103;

        // ========== 角色管理 ==========
        public const ushort GetPlayerList   = 0x0200;
        public const ushort CreatePlayer    = 0x0201;
        public const ushort SelectPlayer    = 0x0202;
        public const ushort DeletePlayer    = 0x0203;

        // ========== 大世界/场景 ==========
        public const ushort EnterScene      = 0x0300;
        public const ushort LeaveScene      = 0x0301;
        public const ushort Move            = 0x0302;
        public const ushort PlayerMove      = 0x0303;
        public const ushort PlayerEnterAOI  = 0x0304;
        public const ushort PlayerLeaveAOI  = 0x0305;
        public const ushort AOIPlayers      = 0x0306;

        // ========== 房间/战斗 ==========
        public const ushort CreateRoom      = 0x0400;
        public const ushort GetRoomList     = 0x0401;
        public const ushort JoinRoom        = 0x0402;
        public const ushort LeaveRoom       = 0x0403;
        public const ushort PlayerJoinRoom  = 0x0404;
        public const ushort PlayerLeaveRoom = 0x0405;
        public const ushort Ready           = 0x0406;
        public const ushort PlayerReady     = 0x0407;
        public const ushort StartGame       = 0x0408;
        public const ushort GameStart       = 0x0409;
        public const ushort FrameInput      = 0x040A;
        public const ushort FrameData       = 0x040B;
        public const ushort GameEnd         = 0x040C;

        // ========== 匹配 ==========
        public const ushort StartMatch         = 0x0450;
        public const ushort CancelMatch        = 0x0451;
        public const ushort MatchSuccess       = 0x0452;
        public const ushort MatchConfirm       = 0x0453;
        public const ushort MatchConfirmResult = 0x0454;

        // ========== 社交 ==========
        public const ushort Chat            = 0x0500;
        public const ushort ChatMessage     = 0x0501;
        public const ushort GetFriendList   = 0x0510;
        public const ushort AddFriend       = 0x0511;
        public const ushort DeleteFriend    = 0x0512;
        public const ushort FriendRequest   = 0x0513;
        public const ushort GetLeaderboard  = 0x0520;

        // ========== 背包/道具 ==========
        public const ushort GetInventory    = 0x0600;
        public const ushort UseItem         = 0x0601;
        public const ushort DropItem        = 0x0602;
        public const ushort EquipItem       = 0x0610;
        public const ushort UnequipItem     = 0x0611;

        // ========== 任务/成就 ==========
        public const ushort GetQuestList    = 0x0700;
        public const ushort AcceptQuest     = 0x0701;
        public const ushort CompleteQuest   = 0x0702;
        public const ushort QuestProgress   = 0x0703;
        public const ushort GetAchievements = 0x0710;
    }
}
