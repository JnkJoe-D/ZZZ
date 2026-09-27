using Game.GamePlay;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// 相机目标跟随编辑器预览处理器
    /// 支持在时间轴编辑器窗口中实时预览相机目标基准点同步移动与回弹复位
    /// </summary>
    [ProcessBinding(typeof(CameraTargetClip), PlayMode.EditorPreview)]
    public class EditorCameraTargetProcess : ProcessBase<CameraTargetClip>
    {
        private ATBoneGetter _boneGetter;
        private Transform _targetBone;
        private CameraPointBinder _pointBinder;
        private Vector3 _defaultFollowLocalPos;
        private Vector3 _defaultLookAtLocalPos;
        private bool _hasRecordedDefaultPos;
        private Vector3 _followDampingVelocity;
        private Vector3 _lookAtDampingVelocity;

        public override void OnEnable()
        {
            if (context?.Owner == null) return;

            _boneGetter = new ATBoneGetter(context.Owner);
            _pointBinder = context.Owner.GetComponent<CameraPointBinder>();

            if (_pointBinder != null && !_hasRecordedDefaultPos)
            {
                if (_pointBinder.FollowPoint != null)
                {
                    _defaultFollowLocalPos = _pointBinder.FollowPoint.localPosition;
                }
                if (_pointBinder.LookAtPoint != null)
                {
                    _defaultLookAtLocalPos = _pointBinder.LookAtPoint.localPosition;
                }
                _hasRecordedDefaultPos = true;
            }

            // 注册系统级清理兜底，窗口关闭或强制停止时必须还原位置
            string cleanupKey = $"CameraTargetPreview.Cleanup.{context.Owner.GetInstanceID()}";
            var binder = _pointBinder;
            var defFollow = _defaultFollowLocalPos;
            var defLookAt = _defaultLookAtLocalPos;
            bool hasRecorded = _hasRecordedDefaultPos;

            context.RegisterCleanup(cleanupKey, () =>
            {
                if (binder != null && hasRecorded)
                {
                    if (binder.FollowPoint != null) binder.FollowPoint.localPosition = defFollow;
                    if (binder.LookAtPoint != null) binder.LookAtPoint.localPosition = defLookAt;
                }
            });
        }

        public override void OnEnter()
        {
            if (_boneGetter != null && clip != null)
            {
                _targetBone = _boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }
            _followDampingVelocity = Vector3.zero;
            _lookAtDampingVelocity = Vector3.zero;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip == null || context?.Owner == null) return;

            if (_targetBone == null && _boneGetter != null)
            {
                _targetBone = _boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }

            Vector3 targetWorldPos = _targetBone != null ? _targetBone.position : context.OwnerTransform.position;
            float weight = CalculateBlendWeight(currentTime);

            ApplyTargetPosition(targetWorldPos, weight, deltaTime);
        }

        public override void OnExit()
        {
            if (clip != null && clip.restoreOnExit)
            {
                RestoreDefaultPositions();
            }
        }

        public override void OnStop()
        {
            if (clip != null && clip.restoreOnStop)
            {
                RestoreDefaultPositions();
            }
        }

        public override void Reset()
        {
            RestoreDefaultPositions();
            _boneGetter = null;
            _targetBone = null;
            _pointBinder = null;
            _hasRecordedDefaultPos = false;
            base.Reset();
        }

        private void ApplyTargetPosition(Vector3 targetWorldPos, float weight, float deltaTime)
        {
            if (_pointBinder == null) return;

            Transform root = _pointBinder.RootTransform != null ? _pointBinder.RootTransform : context.OwnerTransform;
            Transform followPoint = _pointBinder.FollowPoint;
            Transform lookAtPoint = _pointBinder.LookAtPoint;

            Vector3 targetLocalPos;
            if (clip.referenceSpace == CameraTargetSpace.CharacterLocal && root != null)
            {
                targetLocalPos = root.InverseTransformPoint(targetWorldPos) + clip.positionOffset;
            }
            else
            {
                Vector3 rootWorldPos = root != null ? root.position : Vector3.zero;
                targetLocalPos = (targetWorldPos - rootWorldPos) + clip.positionOffset;
            }

            float clampedWeight = Mathf.Clamp01(weight);

            if ((clip.targetScope == CameraTargetScope.Both || clip.targetScope == CameraTargetScope.FollowOnly) && followPoint != null)
            {
                Vector3 basePos = _defaultFollowLocalPos;
                Vector3 filteredPos = new Vector3(
                    clip.syncX ? targetLocalPos.x : basePos.x,
                    clip.syncY ? targetLocalPos.y : basePos.y,
                    clip.syncZ ? targetLocalPos.z : basePos.z
                );
                Vector3 blendedPos = Vector3.Lerp(basePos, filteredPos, clampedWeight);

                if (clip.enableDamping && deltaTime > 0f)
                {
                    followPoint.localPosition = Vector3.SmoothDamp(followPoint.localPosition, blendedPos, ref _followDampingVelocity, Mathf.Max(0.001f, clip.smoothTime), Mathf.Infinity, deltaTime);
                }
                else
                {
                    followPoint.localPosition = blendedPos;
                }
            }

            if ((clip.targetScope == CameraTargetScope.Both || clip.targetScope == CameraTargetScope.LookAtOnly) && lookAtPoint != null)
            {
                if (lookAtPoint == followPoint && clip.targetScope == CameraTargetScope.Both)
                {
                    return;
                }

                Vector3 basePos = _defaultLookAtLocalPos;
                Vector3 filteredPos = new Vector3(
                    clip.syncX ? targetLocalPos.x : basePos.x,
                    clip.syncY ? targetLocalPos.y : basePos.y,
                    clip.syncZ ? targetLocalPos.z : basePos.z
                );
                Vector3 blendedPos = Vector3.Lerp(basePos, filteredPos, clampedWeight);

                if (clip.enableDamping && deltaTime > 0f)
                {
                    lookAtPoint.localPosition = Vector3.SmoothDamp(lookAtPoint.localPosition, blendedPos, ref _lookAtDampingVelocity, Mathf.Max(0.001f, clip.smoothTime), Mathf.Infinity, deltaTime);
                }
                else
                {
                    lookAtPoint.localPosition = blendedPos;
                }
            }
        }

        private void RestoreDefaultPositions()
        {
            if (_pointBinder != null && _hasRecordedDefaultPos)
            {
                if (_pointBinder.FollowPoint != null) _pointBinder.FollowPoint.localPosition = _defaultFollowLocalPos;
                if (_pointBinder.LookAtPoint != null) _pointBinder.LookAtPoint.localPosition = _defaultLookAtLocalPos;
            }
            _followDampingVelocity = Vector3.zero;
            _lookAtDampingVelocity = Vector3.zero;
        }

        private float CalculateBlendWeight(float currentTime)
        {
            if (clip.Duration <= 0.0001f) return 1f;

            float elapsed = Mathf.Max(0f, currentTime - clip.StartTime);
            float remaining = Mathf.Max(0f, clip.EndTime - currentTime);

            float inWeight = 1f;
            if (clip.BlendInDuration > 0.001f)
            {
                float inProgress = Mathf.Clamp01(elapsed / clip.BlendInDuration);
                inWeight = clip.blendInCurve != null ? clip.blendInCurve.Evaluate(inProgress) : inProgress;
            }

            float outWeight = 1f;
            if (clip.BlendOutDuration > 0.001f)
            {
                float outProgress = Mathf.Clamp01(remaining / clip.BlendOutDuration);
                outWeight = clip.blendOutCurve != null ? clip.blendOutCurve.Evaluate(1f - outProgress) : outProgress;
            }

            return Mathf.Clamp01(Mathf.Min(inWeight, outWeight));
        }
    }
}
