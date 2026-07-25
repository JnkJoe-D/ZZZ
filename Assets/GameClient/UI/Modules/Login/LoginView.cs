using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class LoginView : UIView
    {
        // 自动生成的UI组件字段 (已修正重名变量)
        public TMP_Text AccountHeaderText { get; private set; }
        public TMP_InputField AccountInput { get; private set; }
        public TMP_Text AccountPlaceholder { get; private set; }
        public TMP_Text AccountText { get; private set; }
        public TMP_Text PasswordHeaderText { get; private set; }
        public TMP_InputField PasswordInput { get; private set; }
        public TMP_Text PasswordPlaceholder { get; private set; }
        public TMP_Text PasswordText { get; private set; }
        public Button LoginBtn { get; private set; }
        public TMP_Text LoginText { get; private set; }
        public Button RegisterBtn { get; private set; }
        public TMP_Text RegisterText { get; private set; }

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();

            // 硬编码文本赋值
            AccountHeaderText.text = "账号";
            PasswordHeaderText.text = "密码";
            AccountPlaceholder.text = "请输入账号...";
            PasswordPlaceholder.text = "请输入密码...";
            LoginText.text = "登 录";
            RegisterText.text = "注 册";
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
            
            LoginBtn = transform.Find("View/Content/Operations/LoginBtn").GetComponent<Button>();
            LoginText = transform.Find("View/Content/Operations/LoginBtn/LoginText").GetComponent<TMP_Text>();
            
            RegisterBtn = transform.Find("View/Content/Operations/RegisterBtn").GetComponent<Button>();
            RegisterText = transform.Find("View/Content/Operations/RegisterBtn/RegisterText").GetComponent<TMP_Text>();
        }
    }
}
