namespace ATEditor
{
    public interface IRouteWindowHandler : IService
    {
        void OnComboWindowEnter(string comboTag, object windowToken = null);
        void OnComboWindowExit(string comboTag, object windowToken = null);
    }
}
