using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(MovementClip), PlayMode.Runtime)]
    public class RuntimeMovementProcess : ProcessBase<MovementClip>
    {
        private ITransformHandler transformHandler;
        private Vector3 startPosition;
        private Vector3 lastTargetPos;
        private LayerMask originalExcludeLayers;
        private bool hasSetLayer;
        private Vector3 fixedTargetPos; // 用于 Fixed 模式下的目标点缓存
        private Vector3 stableDirection; // 用于 Target 模式下的稳定参考方向

        public override void OnEnable()
        {
            transformHandler = context.GetService<ITransformHandler>();
        }

        private Vector3 resolvedTargetPos; // 解算并校验后的最终安全目标位置
        private bool isTargetFound = true;

        public override void OnEnter()
        {
            if (transformHandler == null) return;

            startPosition = transformHandler.GetPosition();
            Transform owner = context.OwnerTransform;

            // 初始化稳定方向
            Transform target = transformHandler.GetTarget();
            if (target != null)
            {
                Vector3 D1 = startPosition;
                Vector3 D2 = target.position;
                D2.y = D1.y;
                stableDirection = (D2 - D1).normalized;
                if (stableDirection.sqrMagnitude < 0.001f) stableDirection = owner != null ? owner.forward : Vector3.forward;
            }
            else
            {
                stableDirection = owner != null ? owner.forward : Vector3.forward;
            }

            // 计算固定目标/回滚目标点
            if (clip.referenceCoordinate == CoordinateSystem.World)
            {
                fixedTargetPos = clip.targetPosition;
            }
            else
            {
                fixedTargetPos = (owner != null ? owner.position : startPosition) + (owner != null ? owner.rotation : Quaternion.identity) * clip.targetPosition;
            }

            // 智能解算并校验最终目标位置
            resolvedTargetPos = MovementPositionSolver.ResolveTargetPosition(
                clip,
                transformHandler,
                owner,
                startPosition,
                stableDirection,
                out isTargetFound);

            // 若开启了位置校验且未能找到任何合法安全目标，则取消位移
            if (clip.enablePositionValidation && !isTargetFound)
            {
                return;
            }

            // 碰撞层级处理
            if (clip.displacementType == DisplacementType.Continuous && clip.ignoreLayerMask != 0)
            {
                originalExcludeLayers = transformHandler.GetExcludeLayers();
                transformHandler.SetExcludeLayers(clip.ignoreLayerMask);
                hasSetLayer = true;
                string cleanupKey = "Movement_Layer_" + (clip != null ? clip.clipId : GetHashCode().ToString());
                context?.RegisterCleanup(cleanupKey, RestoreLayer);
            }

            // 执行瞬时位移
            if (clip.displacementType == DisplacementType.Instant)
            {
                transformHandler.SetPosition(resolvedTargetPos);

                if (clip.faceTargetOnArrival && target != null)
                {
                    transformHandler.FaceToTargetImmediately(target);
                }
            }
            
            lastTargetPos = startPosition;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (transformHandler == null || clip.displacementType == DisplacementType.Instant) return;
            if (clip.enablePositionValidation && !isTargetFound) return;

            float duration = clip.Duration;
            if (duration <= 0) return;

            float elapsed = currentTime - clip.StartTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = EvaluateCurve(t, clip.movementCurve);

            // 计算当前帧角色应该在的位置（逻辑位置）
            Vector3 desiredPos = Vector3.Lerp(startPosition, resolvedTargetPos, curveT);
            
            // 计算位移增量
            Vector3 currentPos = transformHandler.GetPosition();
            Vector3 delta = desiredPos - currentPos;

            if (delta.sqrMagnitude > 0.0001f)
            {
                transformHandler.Move(delta);
            }

            if (clip.faceTargetOnArrival && t >= 0.99f)
            {
                Transform target = transformHandler.GetTarget();
                if (target != null)
                {
                    transformHandler.FaceToTarget(target, 15f);
                }
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
