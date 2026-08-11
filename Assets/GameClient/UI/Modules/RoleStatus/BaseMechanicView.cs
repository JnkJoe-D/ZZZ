using UnityEngine;
using Game.Logic;

namespace Game.UI.Modules.RoleStatus
{
    /// <summary>
    /// 所有角色专属机制 UI 的基类
    /// 具体的 UI 预制体（如艾莲的急冻层数，狼哥的蓄力条）上挂载继承自此类的脚本
    /// </summary>
    public abstract class BaseMechanicView : MonoBehaviour
    {
        /// <summary>
        /// 当预制体被实例化后，由模块主动调用，用于初始化绑定节点等
        /// </summary>
        public virtual void OnInit()
        {
        }

        /// <summary>
        /// 由 StatusPanelModule 根据收到的 MechanicData 数据驱动子类进行表现更新
        /// </summary>
        public abstract void UpdateView(MechanicData data);
    }
}
