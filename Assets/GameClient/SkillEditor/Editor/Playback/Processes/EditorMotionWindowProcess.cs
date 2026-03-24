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
        private bool _hasAppliedPreviewSnap;

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
            _hasAppliedPreviewSnap = false;
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
            _hasAppliedPreviewSnap = false;
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
            _hasAppliedPreviewSnap = false;
        }

        public override void Reset()
        {
            _lateTickActionKey = null;
            _previewRuntimeData = null;
            _isWindowActive = false;
            _previousRootPosition = Vector3.zero;
            _hasAppliedPreviewSnap = false;
            base.Reset();
        }

        private void OnLateTick(float currentTime, float deltaTime)
        {
            if (!_isWindowActive || clip == null || context?.OwnerTransform == null || _previewRuntimeData == null)
            {
                return;
            }

            Vector3 currentRootPosition = context.OwnerTransform.position;
            Quaternion referenceRotation = ResolvePreviewReferenceRotation();
            Vector3 filteredRootPosition = ApplyPreviewAxisFilter(currentRootPosition, referenceRotation);

            if (clip.UsesConstraintBox())
            {
                MotionConstraintBoxData boxData = MotionConstraintBoxUtility.BuildPreviewConstraintBox(
                    clip,
                    _previewRuntimeData.StartPosition,
                    referenceRotation);

                Vector3 centerOffsetWorld = GetPreviewCapsuleCenterOffset();
                float horizontalRadius = GetPreviewCapsuleRadius();
                if (clip.constraintBoxMode == MotionConstraintBoxMode.Block)
                {
                    if (MotionConstraintBoxUtility.TryRestrictRootPositionToBox(
                        boxData,
                        _previousRootPosition,
                        filteredRootPosition,
                        centerOffsetWorld,
                        horizontalRadius,
                        out Vector3 restrictedRootPosition))
                    {
                        filteredRootPosition = restrictedRootPosition;
                    }
                }
                else if (!_hasAppliedPreviewSnap)
                {
                    filteredRootPosition = MotionConstraintBoxUtility.SnapRootPositionToBoxBoundary(
                        boxData,
                        _previousRootPosition,
                        filteredRootPosition,
                        centerOffsetWorld,
                        horizontalRadius);
                    _hasAppliedPreviewSnap = true;
                }

                filteredRootPosition = MotionConstraintBoxUtility.ClampRootPositionToBox(
                    boxData,
                    filteredRootPosition,
                    centerOffsetWorld,
                    horizontalRadius);
            }

            if ((filteredRootPosition - currentRootPosition).sqrMagnitude > 0.0000001f)
            {
                context.OwnerTransform.position = new Vector3(
                    filteredRootPosition.x,
                    currentRootPosition.y,
                    filteredRootPosition.z);
            }

            _previousRootPosition = context.OwnerTransform.position;
        }

        private Quaternion ResolvePreviewReferenceRotation()
        {
            Vector3 forward = _previewRuntimeData.ReferenceForward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = _previewRuntimeData.StartRotation * Vector3.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private Vector3 ApplyPreviewAxisFilter(Vector3 currentRootPosition, Quaternion referenceRotation)
        {
            if (clip.localDeltaFilterMode == MotionWindowLocalDeltaFilterMode.None)
            {
                return currentRootPosition;
            }

            Vector3 localOffset = Quaternion.Inverse(referenceRotation) * (currentRootPosition - _previewRuntimeData.StartPosition);
            switch (clip.localDeltaFilterMode)
            {
                case MotionWindowLocalDeltaFilterMode.ZeroLocalX:
                    localOffset.x = 0f;
                    break;
                case MotionWindowLocalDeltaFilterMode.ZeroLocalZ:
                    localOffset.z = 0f;
                    break;
                case MotionWindowLocalDeltaFilterMode.ZeroLocalXZ:
                    localOffset.x = 0f;
                    localOffset.z = 0f;
                    break;
            }

            return _previewRuntimeData.StartPosition + referenceRotation * localOffset;
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
