namespace ATEditor
{
    public interface IParryWindowHandler : IService
    {
        void OnParryWindowEnter(int hitEffectId = 0, float hitStopDuration = 0.1f, int heavyHitEffectId = 0, float heavyHitStopDuration = 0f);
        void OnParryWindowExit(bool isInterrupted);
        void SetParryWindowActive(bool active);

        // 新增两段式解耦支持
        void OnCaptureWindowEnter();
        void OnCaptureWindowExit(bool isInterrupted);
        void OnExecuteWindowEnter(ParryExecuteClip clip);
        void OnExecuteWindowExit();
    }
}
