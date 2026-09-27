namespace ATEditor
{
    public interface IParryWindowHandler : IService
    {
        void OnCaptureWindowEnter(ParryCaptureData data);
        void OnCaptureWindowExit(ParryCaptureData data, bool isInterrupted);
        void OnExecuteWindowEnter(int hitEffectId, float hitStopDuration);
        void OnExecuteWindowExit();
    }
}
