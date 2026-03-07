using Game.UI;
using Game.Framework;
using Game.UI.Modules.Common;
using Game.Network;
using Game.Network.Protocol;
using UnityEngine;

namespace Game.UI.Modules.Register
{
    // 假设你的注册面板预制体放在对应目录
    [UIPanel(ViewPrefab = "Assets/Resources/Prefab/UI/PanelView/Register/RegisterPanel.prefab", Layer = UILayer.Window)]
    public class RegisterModule : UIModule<RegisterView, RegisterModel>
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            // 双向绑定输入事件
            View.AccountInput.onValueChanged.AddListener(val => Model.Account = val);
            View.PasswordInput.onValueChanged.AddListener(val => Model.Password = val);
            View.RepeatInput.onValueChanged.AddListener(val => Model.RepeatPassword = val);
            View.EmailInput.onValueChanged.AddListener(val => Model.Email = val);

            // 绑定按钮事件
            View.ConfirmBtn.onClick.AddListener(OnConfirmClick);
            View.BackBtn.onClick.AddListener(OnBackClick);

            // 监听网络响应
            NetworkManager.Instance.Dispatcher.Register<S2C_Register>(MsgId.Register, OnRegisterResponse);

            RefreshView();
        }

        private void RefreshView()
        {
            if (View.AccountInput.text != Model.Account)
                View.AccountInput.text = Model.Account;
            if (View.PasswordInput.text != Model.Password)
                View.PasswordInput.text = Model.Password;
            if (View.RepeatInput.text != Model.RepeatPassword)
                View.RepeatInput.text = Model.RepeatPassword;
            if(View.EmailInput.text != Model.Email)
                View.EmailInput.tag = Model.Email;
        }

        private void OnConfirmClick()
        {
            string account = Model.Account;
            string password = Model.Password;
            string repeat = Model.RepeatPassword;
            string email = Model.Email;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(repeat))
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "注册提示",
                    Content = "账号或密码不能为空！",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
                return;
            }

            if (password != repeat)
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "注册提示",
                    Content = "两次输入的密码不一致！",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
                return;
            }
            if(string.IsNullOrEmpty(email))
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "注册提示",
                    Content = "邮箱不能为空",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
                return;
            }

            Debug.Log($"[RegisterModule] 提交注册请求 Account: {account}");

            var req = new C2S_Register
            {
                Username = account,
                Password = password,
                Email = email
            };
            NetworkManager.Instance.SendTcp(MsgId.Register, req);

            // 发包后清空输入保护安全
            Model.Password = "";
            Model.RepeatPassword = "";
            RefreshView();

            UIManager.Instance.Open<NetWaitModule>(new NetWaitModel() { TipMessage = "注册中..." });
        }

        private void OnRegisterResponse(S2C_Register response)
        {
            Debug.Log($"[RegisterModule] 收到注册响应 Code: {response.Code}, Message: {response.Message}");
            // 收到任意结果，第一时间关闭转圈模块
            UIManager.Instance.Close<NetWaitModule>();
            if (response.Code == (int)ErrorCode.Success)
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "注册成功",
                    Content = "您的账号已成功创立，可以直接返回登录界面登入游戏！",
                    ConfirmText = "好的",
                    CancelText = "",
                    OnCancel = null,
                    OnConfirm = () =>
                    {
                        // 注册成功自动回到登录界面
                        UIManager.Instance.Close(this);
                        UIManager.Instance.Open<Login.LoginModule>();
                    }
                });
            }
            else
            {
                UIManager.Instance.Open<MessageBoxModule>(new MessageBoxModel
                {
                    Title = "注册失败",
                    Content = $"[{response.Code}] {response.Message}",
                    ConfirmText = "确定",
                    CancelText = "",
                    OnCancel = null
                });
            }
        }

        private void OnBackClick()
        {
            // 关闭注册界面，重现登录输入框
            UIManager.Instance.Close(this);
            UIManager.Instance.Open<Login.LoginModule>();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            View.AccountInput.onValueChanged.RemoveAllListeners();
            View.PasswordInput.onValueChanged.RemoveAllListeners();
            View.RepeatInput.onValueChanged.RemoveAllListeners();
            View.ConfirmBtn.onClick.RemoveAllListeners();
            View.BackBtn.onClick.RemoveAllListeners();

            if (NetworkManager.Instance != null && NetworkManager.Instance.Dispatcher != null)
            {
                NetworkManager.Instance.Dispatcher.Unregister<S2C_Register>(MsgId.Register, OnRegisterResponse);
            }
        }
    }
}
