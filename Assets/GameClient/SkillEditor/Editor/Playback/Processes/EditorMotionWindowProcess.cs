using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(MotionWindowClip), PlayMode.EditorPreview)]
    public class EditorMotionWindowProcess : ProcessBase<MotionWindowClip>
    {
        private string _lateTickActionKey;
        private MotionWindowRuntimeData _previewRuntimeData;
        private bool _isWindowActive;
        private Vector3 _previousRootPosition;

        public override void OnEnable()
        {
            if (context?.Owner == null)
            {
                return;
            }

            _lateTickActionKey = $"EditorPreview.MotionWindow.LateTick.{context.Owner.GetInstanceID()}.{clip?.clipId}";
            context.RegisterLateTickAction(_lateTickActionKey, OnLateTick);
        }

        public override void OnEnter()
        {
            if (context?.OwnerTransform == null || clip == null)
            {
                return;
            }

            Vector3 ownerForward = context.OwnerTransform.forward;
            ownerForward.y = 0f;
            if (ownerForward.sqrMagnitude <= 0.0001f)
            {
                ownerForward = Vector3.forward;
            }

            _previewRuntimeData = new MotionWindowRuntimeData
            {
                Clip = clip,
                StartPosition = context.OwnerTransform.position,
                StartRotation = Quaternion.LookRotation(ownerForward.normalized, Vector3.up),
                ReferenceForward = ownerForward.normalized,
                EnterCapsuleCenter = MotionConstraintBoxUtility.ResolveCapsuleCenter(context.Owner, context.OwnerTransform.position)
            };
            _previousRootPosition = context.OwnerTransform.position;
            _isWindowActive = true;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }

        public override void OnExit()
        {
            _isWindowActive = false;
            _previewRuntimeData = null;
            _previousRootPosition = Vector3.zero;
        }

        public override void OnDisable()
        {
            if (context != null && !string.IsNullOrEmpty(_lateTickActionKey))
            {
                context.UnregisterLateTickAction(_lateTickActionKey);
            }

            _isWindowActive = false;
            _previewRuntimeData = null;
            _previousRootPosition = Vector3.zero;
        }

        public override void Reset()
        {
            _lateTickActionKey = null;
            _previewRuntimeData = null;
            _isWindowActive = false;
            _previousRootPosition = Vector3.zero;
            base.Reset();
        }

        private void OnLateTick(float currentTime, float deltaTime)
        {
            if (!_isWindowActive || clip == null || !clip.enableConstraintBox || context?.OwnerTransform == null || _previewRuntimeData == null)
            {
                return;
            }

            MotionConstraintBoxData boxData = MotionConstraintBoxUtility.BuildPreviewConstraintBox(
                clip,
                _previewRuntimeData.StartPosition,
                _previewRuntimeData.StartRotation);

            Vector3 currentRootPosition = context.OwnerTransform.position;
            Vector3 centerOffsetWorld = GetPreviewCapsuleCenterOffset();
            float horizontalRadius = GetPreviewCapsuleRadius();
            if (clip.constraintBoxMode == MotionConstraintBoxMode.Block)
            {
                if (MotionConstraintBoxUtility.TryRestrictRootPositionToBox(
                    boxData,
                    _previousRootPosition,
                    currentRootPosition,
                    centerOffsetWorld,
                    horizontalRadius,
                    out Vector3 restrictedRootPosition))
                {
                    context.OwnerTransform.position = new Vector3(
                        restrictedRootPosition.x,
                        currentRootPosition.y,
                        restrictedRootPosition.z);
                    _previousRootPosition = context.OwnerTransform.position;
                    return;
                }

                if (!MotionConstraintBoxUtility.IsRootPositionInsideBox(
                        boxData,
                        _previousRootPosition,
                        centerOffsetWorld,
                        horizontalRadius))
                {
                    _previousRootPosition = currentRootPosition;
                    return;
                }
            }

            Vector3 clampedRootPosition = MotionConstraintBoxUtility.ClampRootPositionToBox(
                boxData,
                currentRootPosition,
                centerOffsetWorld,
                horizontalRadius);

            if ((clampedRootPosition - currentRootPosition).sqrMagnitude > 0.0000001f)
            {
                context.OwnerTransform.position = new Vector3(
                    clampedRootPosition.x,
                    currentRootPosition.y,
                    clampedRootPosition.z);
            }

            _previousRootPosition = context.OwnerTransform.position;
        }

        private Vector3 GetPreviewCapsuleCenterOffset()
        {
            CharacterController controller = context?.Owner != null ? context.Owner.GetComponent<CharacterController>() : null;
            if (controller == null || context?.OwnerTransform == null)
            {
                return Vector3.zero;
            }

            return context.OwnerTransform.TransformVector(controller.center);
        }

        private float GetPreviewCapsuleRadius()
        {
            CharacterController controller = context?.Owner != null ? context.Owner.GetComponent<CharacterController>() : null;
            return controller != null ? Mathf.Max(controller.radius, 0.01f) : 0.3f;
        }
    }
}
