using Game.UI;
using UnityEngine;

namespace Game.UI.Modules.RoleStatus
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
        public RoleStatusData[] RoleData = new RoleStatusData[3]
        {
            new RoleStatusData(),
            new RoleStatusData(),
            new RoleStatusData()
        };
    }
}
