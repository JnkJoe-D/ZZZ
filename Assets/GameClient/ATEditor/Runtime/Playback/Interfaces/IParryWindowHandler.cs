namespace ATEditor
{
    public interface IParryWindowHandler
    {
        void OnParryWindowEnter();
        void OnParryWindowExit(bool isInterrupted);
        void SetParryWindowActive(bool active);
    }
}
