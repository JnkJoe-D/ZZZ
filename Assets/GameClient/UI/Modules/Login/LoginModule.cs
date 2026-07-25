using Game.Network;
using Game.Network.Protocol;
using UnityEngine;

namespace Game.UI
{
    // 假设你的登录预制体放在 Assets/Resources/Prefab/UI/PanelView/Login/LoginPanel.prefab，可根据实际情况修改
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/Login/LoginPanel.prefab", Layer = UILayer.Window)]
    public class LoginModule : UIModule<LoginView, LoginModel>
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            // 监听输入框变化实时同步到 Model
            View.AccountInput.onValueChanged.AddListener(val => Model.Account = val);
            View.PasswordInput.onValueChanged.AddListener(val => Model.Password = val);

            // 绑定按钮事件
            View.LoginBtn.onClick.AddListener(OnLoginClick);
            View.RegisterBtn.onClick.AddListener(OnRegisterClick);

            // 监听网络回包
            NetworkManager.Instance.Dispatcher.Register<S2C_Login>(MsgId.Login, OnLoginResponse);

            RefreshView();
        }

        private void RefreshView()
        {
            // 初始化阶段把 Model 中的默认空值或记忆值同步给界面
            if (View.AccountInput.text != Model.Account)
            {
                View.AccountInput.text = Model.Account;
            }
            if (View.PasswordInput.text != Model.Password)
            {
                View.PasswordInput.text = Model.Password;
            }
        }

        private void OnLoginClick()
        {
            string account = Model.Account;
            string password = Model.Password;

            // 客户端基础判定
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "登录提示",
                    Content = "账号或密码不能为空！",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
                return;
            }

            Debug.Log($"[LoginModule] 请求登录 Account: {account} （由于安全原因隐去了密码打印）");
            
            // 下发登录请求到服务器
            var req = new C2S_Login
            {
                Username = account,
                Password = password,
                DeviceId = SystemInfo.deviceUniqueIdentifier
            };

            // 发送网络请求前，打开全屏转圈模块
            UIManager.Instance.Open<NetWaitModule>(new NetWaitModel() { TipMessage = "正在登录..." });
            NetworkManager.Instance.SendTcp(MsgId.Login, req);
            
            // 为了防止狂点，可以在这里把按钮置灰或显示“登录中”
            Model.Password = "";
        }

        private void OnLoginResponse(S2C_Login response)
        {
            // 收到任意结果，第一时间关闭转圈模块
            UIManager.Instance.Close<NetWaitModule>();

            Debug.Log($"[LoginModule] 收到登录响应 Code: {response.Code}, Message: {response.Message}");

            if (response.Code == (int)ErrorCode.Success)
            {
                // 登录成功
                // 保存 Token 供断线重连使用
                NetworkManager.Instance.SetToken(response.Token);

                // 在当前业务逻辑层主动控制开启过渡加载 UI
                UIManager.Instance.Open<LoadingModule>();
                
                // 为了演示效果，此处模拟请求进入大厅或正式副本场景
                // 假设目标场景名为 "MainLobby"
                Scene.SceneManager.Instance.ChangeScene(new Scene.SceneTransitionParams
                {
                    SceneName = "MainLobby", // 替换为您实际打好的地图名字
                    RequiredAssets = new System.Collections.Generic.List<string>(), // 后续如果有依赖角色模型放这里
                    ShowLoading = true
                });

                UIManager.Instance.Close(this);
                UIManager.Instance.Close<LoginBackgroundModule>();
                // 后续对接进入大厅： EventCenter.Publish(new TriggerLobbyStageEvent());
            }
            else
            {
                // 登录失败
                RefreshView(); // 此时 Model.Password 已经在发包时被清空
                
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "登录失败",
                    Content = $"[{response.Code}] {response.Message}",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
            }
        }

        private void OnRegisterClick()
        {
            Debug.Log("[LoginModule] 点击注册，打开独立注册面板...");
            UIManager.Instance.Close<Game.UI.LoginModule>();
            UIManager.Instance.Open<Game.UI.RegisterModule>();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            View.AccountInput.onValueChanged.RemoveAllListeners();
            View.PasswordInput.onValueChanged.RemoveAllListeners();
            View.LoginBtn.onClick.RemoveAllListeners();
            View.RegisterBtn.onClick.RemoveAllListeners();

            if (NetworkManager.Instance != null && NetworkManager.Instance.Dispatcher != null)
            {
                NetworkManager.Instance.Dispatcher.Unregister<S2C_Login>(MsgId.Login, OnLoginResponse);
            }
        }
    }
}
