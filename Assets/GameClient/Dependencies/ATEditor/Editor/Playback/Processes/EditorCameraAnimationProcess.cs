namespace ATEditor
{
    [ProcessBinding(typeof(CameraAnimationClip), PlayMode.EditorPreview)]
    public class EditorCameraAnimationProcess : ProcessBase<CameraAnimationClip>
    {
        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 编辑器预览暂不实现复杂逻辑
        }
    }
}
