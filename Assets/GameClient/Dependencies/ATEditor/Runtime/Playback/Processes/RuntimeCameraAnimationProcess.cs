using UnityEngine;
using UnityEngine.Playables;

namespace ATEditor
{
    [ProcessBinding(typeof(CameraAnimationClip), PlayMode.Runtime)]
    public class RuntimeCameraAnimationProcess : ProcessBase<CameraAnimationClip>
    {
        private ICameraHandler _handler;
        private GameObject _cameraInstance;

        public override void OnEnable()
        {
            _handler = context.GetService<ICameraHandler>();
        }

        public override void OnEnter()
        {
            if (_handler == null || clip.cameraPrefab == null || clip.timelineAsset == null)
            {
                return;
            }

            // 生成相机实例
            _cameraInstance = _handler.CreateCamera(clip.cameraPrefab);
            if (_cameraInstance == null) return;

            // 播放 Timeline
            var paramsObj = new CameraControlParams
            {
                timelineAsset = clip.timelineAsset,
                followBoneName = clip.followBoneName,
                lookAtBoneName = clip.lookAtBoneName,
                overrideSettings = clip.overrideSettings,
                backgroundColor = clip.backgroundColor,
                cullingMask = clip.cullingMask
            };
            _handler.PlayCameraTimeline(_cameraInstance, paramsObj);
        }

        public override void OnExit()
        {
        }

        public override void OnDisable()
        {
            if (_cameraInstance != null && _handler != null)
            {
                _handler.DestroyCamera(_cameraInstance);
            }

            _cameraInstance = null;
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
        }
    }
}
