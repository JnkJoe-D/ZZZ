using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(RotationClip), PlayMode.Runtime)]
    public class RuntimeRotationProcess : ProcessBase<RotationClip>
    {
        private ISkillTransformHandler _transformHandler;

        public override void OnEnable()
        {
            _transformHandler = context.GetService<ISkillTransformHandler>();
        }

        public override void OnEnter()
        {
            if (_transformHandler == null) return;

            if (clip.updateFrequency == UpdateFrequency.OnceAtEnter)
            {
                ApplyRotation(1.0f); // 立即应用或按最大步长，对于 Once 来说通常按当前帧处理
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (_transformHandler == null || clip.updateFrequency == UpdateFrequency.OnceAtEnter) return;

            ApplyRotation(deltaTime);
        }

        private void ApplyRotation(float deltaTime)
        {
            Transform target = _transformHandler.GetTarget();
            Vector3 offset = clip.localRotationOffset;
            bool immediate = clip.rotationMode == RotationMode.Immediate;
            float speed = -1f; // 使用 MovementController 的默认转向速度

            switch (clip.referenceDirection)
            {
                case RotationReference.Input:
                    RotateToDirection(_transformHandler.GetInputDirection(false), immediate, offset, speed);
                    break;
                case RotationReference.InputWithCamera:
                    RotateToDirection(_transformHandler.GetInputDirection(true), immediate, offset, speed);
                    break;
                case RotationReference.Target:
                    if (target != null)
                    {
                        if (immediate) _transformHandler.FaceToTargetImmediately(target, offset);
                        else _transformHandler.FaceToTarget(target, speed, offset);
                    }
                    break;
                case RotationReference.TargetThenInput:
                    if (target != null)
                    {
                        if (immediate) _transformHandler.FaceToTargetImmediately(target, offset);
                        else _transformHandler.FaceToTarget(target, speed, offset);
                    }
                    else
                    {
                        RotateToDirection(_transformHandler.GetInputDirection(false), immediate, offset, speed);
                    }
                    break;
                case RotationReference.TargetThenInputWithCamera:
                    if (target != null)
                    {
                        if (immediate) _transformHandler.FaceToTargetImmediately(target, offset);
                        else _transformHandler.FaceToTarget(target, speed, offset);
                    }
                    else
                    {
                        RotateToDirection(_transformHandler.GetInputDirection(true), immediate, offset, speed);
                    }
                    break;
            }
        }

        private void RotateToDirection(Vector3 direction, bool immediate, Vector3 offset, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            if (immediate)
            {
                _transformHandler.RotateToImmediately(direction, offset);
            }
            else
            {
                _transformHandler.RotateTo(direction, speed, offset);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _transformHandler = null;
        }
    }
}
