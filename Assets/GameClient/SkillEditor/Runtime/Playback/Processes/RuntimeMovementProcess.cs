using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(MovementClip), PlayMode.Runtime)]
    public class RuntimeMovementProcess : ProcessBase<MovementClip>
    {
        private ISkillTransformHandler transformHandler;
        private Vector3 startPosition;
        private Vector3 lastTargetPos;
        private LayerMask originalExcludeLayers;
        private bool hasSetLayer;
        private Vector3 fixedTargetPos; // 用于 Fixed 模式下的目标点缓存
        private Vector3 stableDirection; // 用于 Target 模式下的稳定参考方向

        public override void OnEnable()
        {
            transformHandler = context.GetService<ISkillTransformHandler>();
        }

        public override void OnEnter()
        {
            if (transformHandler == null) return;

            startPosition = transformHandler.GetPosition();

            // 计算固定目标/回滚目标点
            Transform owner = context.OwnerTransform;
            fixedTargetPos = owner.position + owner.rotation * clip.targetPosition;

            // 碰撞层级处理
            if (clip.displacementType == DisplacementType.Continuous && clip.ignoreLayerMask != 0)
            {
                originalExcludeLayers = transformHandler.GetExcludeLayers();
                transformHandler.SetExcludeLayers(clip.ignoreLayerMask);
                hasSetLayer = true;
            }

            // 执行逻辑判断
            if (clip.referenceDestination == ReferenceDestination.Fixed)
            {
                if (clip.displacementType == DisplacementType.Instant)
                {
                    transformHandler.SetPosition(fixedTargetPos);
                }
            }
            else // Target
            {
                // 初始化稳定方向
                Transform target = transformHandler.GetTarget();
                if (target != null)
                {
                    Vector3 D1 = startPosition;
                    Vector3 D2 = target.position;
                    D2.y = D1.y;
                    stableDirection = (D2 - D1).normalized;
                    if (stableDirection.sqrMagnitude < 0.001f) stableDirection = context.OwnerTransform.forward;
                }
                else
                {
                    stableDirection = context.OwnerTransform.forward;
                }

                if (clip.displacementType == DisplacementType.Instant)
                {
                    Vector3 targetPos = CalculateTargetPosition();
                    transformHandler.SetPosition(targetPos);
                }
            }
            
            lastTargetPos = startPosition;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (transformHandler == null || clip.displacementType == DisplacementType.Instant) return;

            float duration = clip.Duration;
            if (duration <= 0) return;

            float elapsed = currentTime - clip.StartTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = EvaluateCurve(t, clip.movementCurve);

            // 计算当前帧角色应该在的位置（逻辑位置）
            Vector3 finalTargetPos = (clip.referenceDestination == ReferenceDestination.Fixed) 
                ? fixedTargetPos 
                : CalculateTargetPosition();

            Vector3 desiredPos = Vector3.Lerp(startPosition, finalTargetPos, curveT);
            
            // 计算位移增量
            Vector3 currentPos = transformHandler.GetPosition();
            Vector3 delta = desiredPos - currentPos;

            if (delta.sqrMagnitude > 0.0001f)
            {
                transformHandler.Move(delta);
            }
        }

        public override void OnExit()
        {
            RestoreLayer();
        }

        public override void OnDisable()
        {
            RestoreLayer();
        }

        public override void Reset()
        {
            base.Reset();
            transformHandler = null;
            hasSetLayer = false;
        }

        private void RestoreLayer()
        {
            if (hasSetLayer && transformHandler != null)
            {
                transformHandler.SetExcludeLayers(originalExcludeLayers);
                hasSetLayer = false;
            }
        }

        private Vector3 CalculateTargetPosition()
        {
            Transform target = transformHandler.GetTarget();
            if (target == null) return fixedTargetPos; // 降级

            Vector3 D1 = transformHandler.GetPosition();
            Vector3 D2 = target.position;
            D2.y = D1.y; // 保持水平计算

            float R1 = transformHandler.GetTargetRadius();
            float R2 = transformHandler.GetRadius();
            float dist = R1 + R2;

            if (clip.targetPositionEnum == TargetPositionType.EnemyFront)
            {
                return D2 - stableDirection * dist;
            }
            else // EnemyBack
            {
                return D2 + stableDirection * dist;
            }
        }

        private float EvaluateCurve(float t, MovementCurve curve)
        {
            switch (curve)
            {
                case MovementCurve.EaseIn: return t * t;
                case MovementCurve.EaseOut: return 1 - (1 - t) * (1 - t);
                case MovementCurve.EaseInOut: return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                default: return t;
            }
        }
    }
}
