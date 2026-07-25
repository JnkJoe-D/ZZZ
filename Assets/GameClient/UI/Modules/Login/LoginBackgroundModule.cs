using UnityEngine;

namespace Game.UI
{
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/LOgin/LoginBackgroundPanel.prefab", Layer = UILayer.Background, IsFullScreen = true)]
    public class LoginBackgroundModule : UIModule<LoginBackgroundView, LoginBackgroundModel>
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("[LoginBackgroundModule] 独立登录视效背景已开启");
            // 这里以后可以读取配置加载不同画面的背景图/视频逻辑
        }
    }
}
