using Game.UI;

namespace Game.UI
{
    public class RegisterModel : UIModel
    {
        public string Account { get; set; } = "";
        public string Password { get; set; } = "";
        public string RepeatPassword { get; set; } = "";
        public string Email {get;set;} = "";
    }
}
