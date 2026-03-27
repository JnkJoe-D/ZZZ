using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(RotationClip), PlayMode.Runtime)]
    public class RuntimeRotationProcess : ProcessBase<RotationClip>
    {
        private ISkillTransformHandler transformHandler;

        public override void OnEnable()
        {
            transformHandler = context.GetService<ISkillTransformHandler>();
        }

        public override void OnEnter()
        {
            if (transformHandler == null) return;

            if (clip.updateFrequency == UpdateFrequency.OnceAtEnter)
            {
                ApplyRotation(1.0f); // 立即应用或按最大步长，对于 Once 来说通常按当前帧处理
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (transformHandler == null || clip.updateFrequency == UpdateFrequency.OnceAtEnter) return;

            ApplyRotation(deltaTime);
        }

        private void ApplyRotation(float deltaTime)
        {
            Quaternion targetRot = CalculateTargetRotation();
            if (targetRot == Quaternion.identity && clip.referenceDirection == RotationReference.Target) return;

            if (clip.rotationMode == RotationMode.Immediate)
            {
                transformHandler.SetRotation(targetRot);
            }
            else
            {
                // 这里假设 RotateTowards 内部会处理平滑旋转，或者我们在这里传一个速度
                // 如果片段有旋转速度字段更好，目前没有，可以暂时用一个默认高初值
                transformHandler.RotateTowards(targetRot, 15f); 
            }
        }

        private Quaternion CalculateTargetRotation()
        {
            Vector3 lookDir = Vector3.zero;
            Transform target = transformHandler.GetTarget();

            switch (clip.referenceDirection)
            {
                case RotationReference.Input:
                    lookDir = transformHandler.GetInputDirection(false);
                    break;
                case RotationReference.InputWithCamera:
                    lookDir = transformHandler.GetInputDirection(true);
                    break;
                case RotationReference.Target:
                    if (target != null) lookDir = target.position - transformHandler.GetPosition();
                    break;
                case RotationReference.TargetThenInput:
                    if (target != null) lookDir = target.position - transformHandler.GetPosition();
                    else lookDir = transformHandler.GetInputDirection(false);
                    break;
                case RotationReference.TargetThenInputWithCamera:
                    if (target != null) lookDir = target.position - transformHandler.GetPosition();
                    else lookDir = transformHandler.GetInputDirection(true);
                    break;
            }

            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(lookDir);
            }

            return transformHandler.GetRotation();
        }

        public override void Reset()
        {
            base.Reset();
            transformHandler = null;
        }
    }
}
