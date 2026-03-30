namespace SkillEditor
{
    [ProcessBinding(typeof(CameraControlClip), PlayMode.EditorPreview)]
    public class EditorCameraControlProcess : ProcessBase<CameraControlClip>
    {
        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 编辑器预览暂不实现复杂逻辑
        }
    }
}
