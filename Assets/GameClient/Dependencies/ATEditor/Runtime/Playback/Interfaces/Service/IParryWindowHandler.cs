namespace ATEditor
{
    public interface IParryWindowHandler : IService
    {
        void OnCaptureWindowEnter();
        void OnCaptureWindowExit(bool isInterrupted);
        void OnExecuteWindowEnter(int hitEffectId, float hitStopDuration);
        void OnExecuteWindowExit();
    }
}
