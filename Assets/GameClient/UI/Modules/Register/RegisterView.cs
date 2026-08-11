using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class RegisterView : UIView
    {
        // 自动生成的UI组件字段
        public TMP_Text AccountHeaderText { get; private set; }
        public TMP_InputField AccountInput { get; private set; }
        public TMP_Text AccountPlaceholder { get; private set; }
        public TMP_Text AccountText { get; private set; }
        public TMP_Text PasswordHeaderText { get; private set; }
        public TMP_InputField PasswordInput { get; private set; }
        public TMP_Text PasswordPlaceholder { get; private set; }
        public TMP_Text PasswordText { get; private set; }
        public TMP_Text RepeatHeaderText { get; private set; }
        public TMP_InputField RepeatInput { get; private set; }
        public TMP_Text RepeatPlaceholder { get; private set; }
        public TMP_Text RepeatText { get; private set; }
        public TMP_Text EmailHeaderText { get; private set; }
        public TMP_InputField EmailInput { get; private set; }
        public TMP_Text EmailPlaceholder { get; private set; }
        public TMP_Text EmailText { get; private set; }
        public Button ConfirmBtn { get; private set; }
        public TMP_Text ConfirmText { get; private set; }
        public Button BackBtn { get; private set; }
        public TMP_Text BackText { get; private set; }

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();

            // 硬编码文本赋值
            AccountHeaderText.text = "账号";
            PasswordHeaderText.text = "密码";
            RepeatHeaderText.text = "确认密码";
            
            AccountPlaceholder.text = "请输入账号...";
            PasswordPlaceholder.text = "请输入密码...";
            RepeatPlaceholder.text = "请再次输入密码...";
            EmailPlaceholder.text = "请输入邮箱...";


            ConfirmText.text = "确认注册";
            BackText.text = "返 回";
        }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            AccountHeaderText = transform.Find("View/Content/Account/AccountHeaderText").GetComponent<TMP_Text>();
            AccountInput = transform.Find("View/Content/Account/AccountInput").GetComponent<TMP_InputField>();
            AccountPlaceholder = transform.Find("View/Content/Account/AccountInput/Text Area/AccountPlaceholder").GetComponent<TMP_Text>();
            AccountText = transform.Find("View/Content/Account/AccountInput/Text Area/AccountText").GetComponent<TMP_Text>();
            
            PasswordHeaderText = transform.Find("View/Content/Password/PasswordHeaderText").GetComponent<TMP_Text>();
            PasswordInput = transform.Find("View/Content/Password/PasswordInput").GetComponent<TMP_InputField>();
            PasswordPlaceholder = transform.Find("View/Content/Password/PasswordInput/Text Area/PasswordPlaceholder").GetComponent<TMP_Text>();
            PasswordText = transform.Find("View/Content/Password/PasswordInput/Text Area/PasswordText").GetComponent<TMP_Text>();
            
            RepeatHeaderText = transform.Find("View/Content/Repeat/RepeatHeaderText").GetComponent<TMP_Text>();
            RepeatInput = transform.Find("View/Content/Repeat/RepeatInput").GetComponent<TMP_InputField>();
            RepeatPlaceholder = transform.Find("View/Content/Repeat/RepeatInput/Text Area/RepeatPlaceholder").GetComponent<TMP_Text>();
            RepeatText = transform.Find("View/Content/Repeat/RepeatInput/Text Area/RepeatText").GetComponent<TMP_Text>();

            EmailHeaderText = transform.Find("View/Content/Email/EmailHeaderText").GetComponent<TMP_Text>();
            EmailInput = transform.Find("View/Content/Email/EmailInput").GetComponent<TMP_InputField>();
            EmailPlaceholder = transform.Find("View/Content/Email/EmailInput/Text Area/EmailPlaceholder").GetComponent<TMP_Text>();
            EmailText = transform.Find("View/Content/Email/EmailInput/Text Area/EmailText").GetComponent<TMP_Text>();

            ConfirmBtn = transform.Find("View/Content/Operations/ConfirmBtn").GetComponent<Button>();
            ConfirmText = transform.Find("View/Content/Operations/ConfirmBtn/ConfirmText").GetComponent<TMP_Text>();
            
            BackBtn = transform.Find("View/Content/Operations/BackBtn").GetComponent<Button>();
            BackText = transform.Find("View/Content/Operations/BackBtn/BackText").GetComponent<TMP_Text>();
        }
    
        public void UpdateView(RegisterModel model)
        {
            if (AccountInput.text != model.Account)
                AccountInput.text = model.Account;
            if (PasswordInput.text != model.Password)
                PasswordInput.text = model.Password;
            if (RepeatInput.text != model.RepeatPassword)
                RepeatInput.text = model.RepeatPassword;
            if(EmailInput.text != model.Email)
                EmailInput.tag = model.Email;
        }
}
}
