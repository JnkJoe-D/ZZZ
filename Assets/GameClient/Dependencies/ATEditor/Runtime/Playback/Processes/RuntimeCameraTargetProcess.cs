using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 相机目标跟随运行时处理器
    /// </summary>
    [ProcessBinding(typeof(CameraTargetClip), PlayMode.Runtime)]
    public class RuntimeCameraTargetProcess : ProcessBase<CameraTargetClip>
    {
        private ICameraHandler _cameraHandler;
        private IBoneGetter _boneGetter;
        private Transform _targetBone;

        public override void OnEnable()
        {
            _cameraHandler = context.GetService<ICameraHandler>();
            _boneGetter = context.GetService<IBoneGetter>();
        }

        public override void OnEnter()
        {
            if (_boneGetter != null && clip != null)
            {
                _targetBone = _boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (_cameraHandler == null || clip == null) return;

            if (_targetBone == null && _boneGetter != null)
            {
                _targetBone = _boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }

            Vector3 targetWorldPos = _targetBone != null 
                ? _targetBone.position 
                : (context.OwnerTransform != null ? context.OwnerTransform.position : Vector3.zero);

            float weight = CalculateBlendWeight(currentTime);

            _cameraHandler.SetCameraTargetAnchor(
                clip.targetScope,
                targetWorldPos,
                weight,
                clip.syncX,
                clip.syncY,
                clip.syncZ,
                clip.positionOffset,
                clip.referenceSpace,
                clip.enableDamping,
                clip.smoothTime,
                deltaTime
            );
        }

        public override void OnExit()
        {
            if (_cameraHandler != null && clip != null && clip.restoreOnExit)
            {
                _cameraHandler.RestoreCameraTargetAnchor(clip.targetScope, clip.BlendOutDuration);
            }
        }

        public override void OnStop()
        {
            if (_cameraHandler != null && clip != null && clip.restoreOnStop)
            {
                _cameraHandler.RestoreCameraTargetAnchor(clip.targetScope, clip.interruptRestoreDuration);
            }
        }

        public override void Reset()
        {
            _cameraHandler = null;
            _boneGetter = null;
            _targetBone = null;
            base.Reset();
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
