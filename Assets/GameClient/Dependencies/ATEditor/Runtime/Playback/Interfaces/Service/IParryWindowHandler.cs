namespace ATEditor
{
    public interface IParryWindowHandler : IService
    {
        void OnParryWindowEnter(ParryWindowClip clip);
        void OnParryWindowExit(ParryWindowClip clip, bool isInterrupted);
    }
}
