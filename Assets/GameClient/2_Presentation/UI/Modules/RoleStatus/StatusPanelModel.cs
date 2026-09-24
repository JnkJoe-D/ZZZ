using Game.UI;
using UnityEngine;

namespace Game.UI
{
    public class RoleStatusData
    {
        public float HpPercent;
        public float EnergyPercent;
        public float EnergyThresholdPercent;
        public Sprite RoleIcon;
    }

    public class StatusPanelModel : UIModel
    {
        public const int MaxSlots = 3;

        /// <summary>
        /// 当前队伍实际可见并生效的角色数量（1~3）
        /// </summary>
        public int VisibleMemberCount { get; set; } = 3;

        public RoleStatusData[] RoleData = new RoleStatusData[MaxSlots]
        {
            new RoleStatusData(),
            new RoleStatusData(),
            new RoleStatusData()
        };
    }
}
