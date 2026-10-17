namespace ATEditor.Editor
{
    /// <summary>编辑器拖动仅显示范围，不做物理检测或业务结算。</summary>
    [ProcessBinding(typeof(AttackClip), PlayMode.EditorPreview)]
    public sealed class EditorAttackProcess : ProcessBase<AttackClip>
    {
        private UnityEngine.Vector3 _position;
        private UnityEngine.Quaternion _rotation;
        public override void OnEnter() => clip.scope.CaptureMatrix(context.OwnerTransform, context.GetService<IBoneGetter>(), out _position, out _rotation);
        public void GetMatrix(out UnityEngine.Vector3 position, out UnityEngine.Quaternion rotation)
            => clip.scope.CalculateMatrix(context.OwnerTransform, context.GetService<IBoneGetter>(), _position, _rotation, out position, out rotation);
        public override void OnUpdate(float currentTime, float deltaTime) { }
    }
}
