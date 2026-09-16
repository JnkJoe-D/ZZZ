using Game.Framework;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 全局网络请求等待遮罩层
    /// 位于 System 层级，阻断用户一切输入（预制体根节点背景需带 RaycastTarget）
    /// </summary>
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/Common/NetWaitPanel.prefab", Layer = UILayer.System)]
    public class NetWaitModule : UIModule<NetWaitView, NetWaitModel>
    {
        private const float RotateSpeed = 360f; // 旋转速度（度/秒）

        protected override void OnCreate()
        {
            // 初始化若有其它需要挂载的逻辑
        }

        protected override void OnShow(object data)
        {
            if (data is NetWaitModel model)
            {
                Model.TipMessage = model.TipMessage;
            }
            else if (data is string msg) // 为了方便调用也可以直接传string
            {
                Model.TipMessage = msg;
            }
            else
            {
                Model.TipMessage = "加载中...";
            }

            View?.UpdateView(Model);

            if (View != null)
            {
                View.StartRotate(RotateSpeed);
            }
        }

        protected override void OnHide()
        {
            if (View != null)
            {
                View.StopRotate();
            }
        }

        
    }
}
