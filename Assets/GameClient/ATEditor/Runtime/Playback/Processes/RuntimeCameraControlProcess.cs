using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 常规相机控制运行时处理器（锁定旋转、视角回正、目标注视、FOV变焦）
    /// </summary>
    [ProcessBinding(typeof(CameraControlClip), PlayMode.Runtime)]
    public class RuntimeCameraControlProcess : ProcessBase<CameraControlClip>
    {
        private ICameraHandler _handler;

        public override void OnEnable()
        {
            _handler = context.GetService<ICameraHandler>();
        }

        public override void OnEnter()
        {
            if (_handler == null) return;

            switch (clip.controlMode)
            {
                case CameraControlMode.LockRotation:
                    _handler.LockCameraRotation(clip.lockYaw, clip.lockPitch);
                    break;

                case CameraControlMode.Recenter:
                    _handler.StartRecenter(clip.recenterTarget, clip.smoothTime, clip.targetPitch, clip.disableInputDuringRecenter, clip.framingBiasAngle, clip.deadzoneAngle, clip.allowSoftInputInterrupt);
                    break;

                case CameraControlMode.LookAtTarget:
                    _handler.StartLookAtTarget(clip.lookAtOffset, clip.trackSmoothSpeed, clip.fallbackToCharacter);
                    break;

                case CameraControlMode.FovTransition:
                    _handler.SetCameraFOVAndDistance(clip.targetFOV, clip.targetDistance, clip.blendInSpeed);
                    break;
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (_handler == null) return;

            switch (clip.controlMode)
            {
                case CameraControlMode.Recenter:
                    _handler.UpdateRecenter(deltaTime);
                    break;

                case CameraControlMode.LookAtTarget:
                    _handler.UpdateLookAtTarget(deltaTime);
                    break;
            }
        }

        public override void OnExit()
        {
            if (_handler == null) return;

            switch (clip.controlMode)
            {
                case CameraControlMode.LockRotation:
                    if (clip.unlockOnExit)
                    {
                        _handler.UnlockCameraRotation();
                    }
                    break;

                case CameraControlMode.Recenter:
                    _handler.StopRecenter(clip.unlockInputOnExit);
                    break;

                case CameraControlMode.LookAtTarget:
                    if (clip.restoreLookAtOnExit)
                    {
                        _handler.StopLookAtTarget(true);
                    }
                    break;

                case CameraControlMode.FovTransition:
                    if (clip.restoreOnExit)
                    {
                        _handler.ResetCameraFOVAndDistance(clip.blendOutSpeed);
                    }
                    break;
            }
        }

        public override void OnDisable()
        {
            if (_handler == null) return;

            if (clip.controlMode == CameraControlMode.LockRotation && clip.unlockOnExit)
            {
                _handler.UnlockCameraRotation();
            }
            else if (clip.controlMode == CameraControlMode.Recenter)
            {
                _handler.StopRecenter(true);
            }
            else if (clip.controlMode == CameraControlMode.LookAtTarget && clip.restoreLookAtOnExit)
            {
                _handler.StopLookAtTarget(true);
            }
            else if (clip.controlMode == CameraControlMode.FovTransition && clip.restoreOnExit)
            {
                _handler.ResetCameraFOVAndDistance(clip.blendOutSpeed);
            }
        }
    }
}
