namespace SkillEditor
{
    public interface ISkillMotionWindowHandler
    {
        void OnMotionWindowEnter(MotionWindowClip clip, ProcessContext context, float enterTime);
        void OnMotionWindowUpdate(MotionWindowClip clip, float currentTime, float deltaTime);
        void OnMotionWindowExit(MotionWindowClip clip);
        void OnMotionWindowCancel(MotionWindowClip clip);
        bool TryGetActiveWindow(out MotionWindowRuntimeData runtimeData);
    }
}
