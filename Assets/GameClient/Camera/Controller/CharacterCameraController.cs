using Cinemachine;
using Game.Logic;
using UnityEngine;

namespace Game.Camera
{
    public class CharacterCameraController : MonoBehaviour, ICameraController
    {
        [SerializeField]
        private GameObject _virtualCameraPrefab;

        [SerializeField]
        private CinemachineVirtualCameraBase _virtualCamera;

        [SerializeField]
        private string virtualCamName = "主相机";

        [SerializeField]
        private Transform follow;

        [SerializeField]
        private Transform lookAt;

        private Transform _mainCamTransform;
        private CharacterEntity _entity;
        private CameraPointBinder _pointBinder;
        private bool _ownsVirtualCameraInstance;
        private CinemachineImpulseSource _impluseSource;

        private void Awake()
        {
            if (UnityEngine.Camera.main != null)
            {
                _mainCamTransform = UnityEngine.Camera.main.transform;
            }
        }

        public void Init(CharacterEntity entity)
        {
            if (_virtualCamera == null && _virtualCameraPrefab != null)
            {
                GameObject obj = UnityEngine.Object.Instantiate(_virtualCameraPrefab);
                _ownsVirtualCameraInstance = true;
                _virtualCamera = obj.GetComponent<CinemachineVirtualCameraBase>();
                _impluseSource = obj.GetComponent<CinemachineImpulseSource>();
            }

            SetBindingSource(entity);
            _isInputDisabled = false;
            _isYawLocked = false;
            _isPitchLocked = false;
            ApplyAxisInputState();
        }

        public void EnableInput(bool enable)
        {
            // 如果绑定了角色实体，且该实体并不是当前主控角色（例如待机隐藏的队友），则不应该篡改共享相机的输入状态
            if (_entity != null && !_entity.IsControlActive && !enable)
            {
                return;
            }

            _isInputDisabled = !enable;
            ApplyAxisInputState();
        }

        public void SetCameraActive(bool active)
        {
            if (_virtualCamera == null)
            {
                return;
            }

            if (active)
            {
                RefreshVirtualCameraTargets();
            }

            _virtualCamera.gameObject.SetActive(active);
        }

        public void SnapToPose(Vector3 position, Quaternion rotation)
        {
            if (_virtualCamera == null)
            {
                return;
            }

            _virtualCamera.PreviousStateIsValid = false;
            _virtualCamera.ForceCameraPosition(position, rotation);
        }

        public void AssignVirtualCamera(CinemachineVirtualCameraBase virtualCamera)
        {
            if (_virtualCamera == virtualCamera)
            {
                RefreshVirtualCameraTargets();
                _isInputDisabled = false;
                _isYawLocked = false;
                _isPitchLocked = false;
                ApplyAxisInputState();
                return;
            }

            ReleaseOwnedVirtualCamera();
            _virtualCamera = virtualCamera;
            _impluseSource = ResolveImpulseSource(virtualCamera);
            RefreshVirtualCameraTargets();
            _isInputDisabled = false;
            _isYawLocked = false;
            _isPitchLocked = false;
            ApplyAxisInputState();
        }

        public void SetBindingSource(CharacterEntity entity)
        {
            _entity = entity;
            _pointBinder = entity != null ? entity.GetComponent<CameraPointBinder>() : null;
            RefreshVirtualCameraTargets();
        }

        public Vector3 GetForward()
        {
            if (_mainCamTransform != null)
            {
                Vector3 forward = _mainCamTransform.forward;
                forward.y = 0f;
                return forward.normalized;
            }

            Transform bindingRoot = GetBindingRoot();
            return bindingRoot != null ? bindingRoot.forward : transform.forward;
        }

        public Vector3 GetRight()
        {
            if (_mainCamTransform != null)
            {
                Vector3 right = _mainCamTransform.right;
                right.y = 0f;
                return right.normalized;
            }

            Transform bindingRoot = GetBindingRoot();
            return bindingRoot != null ? bindingRoot.right : transform.right;
        }

        public void GenerateImpulse()
        {
            _impluseSource?.GenerateImpulse();
        }

        public void GenerateImpulseWithVelocity(Vector3 velocity, float force, float duration)
        {
            if (_impluseSource == null)
            {
                return;
            }

            var envelope = _impluseSource.m_ImpulseDefinition.m_TimeEnvelope;
            float attack = envelope.m_AttackTime;
            float decay = envelope.m_DecayTime;
            envelope.m_SustainTime = Mathf.Max(0f, duration - attack - decay);
            _impluseSource.m_ImpulseDefinition.m_TimeEnvelope = envelope;
            _impluseSource.GenerateImpulseWithVelocity(velocity * force);
        }

        public GameObject CreateCamera(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            Transform parent = GetBindingRoot();
            return parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab, transform);
        }

        public void DestroyCamera(GameObject cameraInstance)
        {
            if (cameraInstance != null)
            {
                UnityEngine.Object.Destroy(cameraInstance);
            }
        }

        private void OnDestroy()
        {
            ReleaseOwnedVirtualCamera();
        }

        public void PlayCameraTimeline(GameObject cameraInstance, ATEditor.CameraControlParams paramsObj)
        {
            if (cameraInstance == null || paramsObj == null || paramsObj.timelineAsset == null)
            {
                return;
            }

            UnityEngine.Playables.PlayableDirector director = cameraInstance.GetComponent<UnityEngine.Playables.PlayableDirector>();
            if (director == null)
            {
                director = cameraInstance.AddComponent<UnityEngine.Playables.PlayableDirector>();
            }

            director.playableAsset = paramsObj.timelineAsset;

            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            CameraClearFlags originalClearFlags = 0;
            Color originalBgColor = Color.black;
            int originalMask = -1;
            bool didOverride = false;

            if (paramsObj.overrideSettings && mainCam != null)
            {
                originalClearFlags = mainCam.clearFlags;
                originalBgColor = mainCam.backgroundColor;
                originalMask = mainCam.cullingMask;

                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = paramsObj.backgroundColor;
                mainCam.cullingMask = paramsObj.cullingMask;
                didOverride = true;
            }

            CinemachineVirtualCameraBase virtualCam = cameraInstance.GetComponentInChildren<CinemachineVirtualCameraBase>();
            Animator animator = cameraInstance.GetComponentInChildren<Animator>();

            if (virtualCam != null)
            {
                if (!string.IsNullOrEmpty(paramsObj.followBoneName))
                {
                    virtualCam.Follow = FindBindingBone(paramsObj.followBoneName);
                }

                if (!string.IsNullOrEmpty(paramsObj.lookAtBoneName))
                {
                    virtualCam.LookAt = FindBindingBone(paramsObj.lookAtBoneName);
                }
            }

            if (paramsObj.timelineAsset is UnityEngine.Timeline.TimelineAsset timeline)
            {
                foreach (var output in timeline.outputs)
                {
                    if (output.sourceObject is UnityEngine.Timeline.AnimationTrack && animator != null)
                    {
                        director.SetGenericBinding(output.sourceObject, animator);
                    }
                    else if (output.sourceObject is UnityEngine.Timeline.ControlTrack && virtualCam != null)
                    {
                        director.SetGenericBinding(output.sourceObject, virtualCam.gameObject);
                    }
                }
            }

            director.stopped += _ =>
            {
                if (didOverride && mainCam != null)
                {
                    mainCam.clearFlags = originalClearFlags;
                    mainCam.backgroundColor = originalBgColor;
                    mainCam.cullingMask = originalMask;
                }

                if (cameraInstance != null)
                {
                    DestroyCamera(cameraInstance);
                }
            };

            director.Play();
        }

        private Transform FindBindingBone(string name)
        {
            Transform bindingRoot = GetBindingRoot();
            if (bindingRoot == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            return FindChildRecursive(bindingRoot, name);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void RefreshVirtualCameraTargets()
        {
            if (_virtualCamera == null)
            {
                return;
            }

            _virtualCamera.Follow = follow ?? GetFollowTarget();
            _virtualCamera.LookAt = lookAt ?? GetLookAtTarget();
            _impluseSource = ResolveImpulseSource(_virtualCamera);
        }

        private Transform GetBindingRoot()
        {
            if (_pointBinder != null)
            {
                return _pointBinder.RootTransform;
            }

            return _entity != null ? _entity.transform : transform;
        }

        private Transform GetFollowTarget()
        {
            if (_pointBinder != null)
            {
                return _pointBinder.FollowPoint;
            }

            return _entity != null ? _entity.transform : transform;
        }

        private Transform GetLookAtTarget()
        {
            if (_pointBinder != null)
            {
                return _pointBinder.LookAtPoint;
            }

            return _entity != null ? _entity.transform : transform;
        }

        private void ReleaseOwnedVirtualCamera()
        {
            if (!_ownsVirtualCameraInstance || _virtualCamera == null)
            {
                _ownsVirtualCameraInstance = false;
                return;
            }

            UnityEngine.Object.Destroy(_virtualCamera.gameObject);
            _virtualCamera = null;
            _impluseSource = null;
            _ownsVirtualCameraInstance = false;
        }

        private static CinemachineImpulseSource ResolveImpulseSource(CinemachineVirtualCameraBase virtualCamera)
        {
            if (virtualCamera == null)
            {
                return null;
            }

            return virtualCamera.GetComponent<CinemachineImpulseSource>()
                ?? virtualCamera.GetComponentInParent<CinemachineImpulseSource>();
        }

        #region Regular Camera Control Implementation
        private bool _isRecenterActive;
        private float _recenterSmoothTime = 0.25f;
        private float _targetPitch = 12f;
        private ATEditor.CameraRecenterTarget _recenterTarget;
        private bool _disableInputDuringRecenter;
        private float _framingBiasAngle = -8.0f;
        private float _deadzoneAngle = 1.5f;
        private bool _allowSoftInputInterrupt = true;
        private float _recenterYawVelocity;
        private float _recenterPitchVelocity;
        private float _softInputBlendWeight = 1.0f;

        private bool _isLookAtActive;
        private Vector3 _lookAtOffset = new Vector3(0, 1.2f, 0);
        private float _lookAtSmoothSpeed = 8f;
        private bool _lookAtFallbackToCharacter = true;
        private Transform _cachedLookAtTransform;

        private float _originalFov = -1f;
        private float _originalDistance = -1f;
        private float _targetFov = -1f;
        private float _targetDistance = -1f;
        private float _fovBlendSpeed = 5f;
        private bool _isFovTransitionActive;

        private bool _isInputDisabled;
        private bool _isYawLocked;
        private bool _isPitchLocked;
        private string _cachedHorizontalAxisName = "Mouse X";
        private string _cachedVerticalAxisName = "Mouse Y";
        private float _lockedYawValue;
        private float _lockedPitchValue;

        private void ApplyAxisInputState()
        {
            if (_virtualCamera == null) return;

            bool shouldLockYaw = _isInputDisabled || _isYawLocked;
            bool shouldLockPitch = _isInputDisabled || _isPitchLocked;

            if (_virtualCamera is CinemachineVirtualCamera vcam)
            {
                var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                if (pov != null)
                {
                    if (!string.IsNullOrEmpty(pov.m_HorizontalAxis.m_InputAxisName))
                    {
                        _cachedHorizontalAxisName = pov.m_HorizontalAxis.m_InputAxisName;
                    }
                    if (!string.IsNullOrEmpty(pov.m_VerticalAxis.m_InputAxisName))
                    {
                        _cachedVerticalAxisName = pov.m_VerticalAxis.m_InputAxisName;
                    }

                    if (shouldLockYaw)
                    {
                        pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
                        pov.m_HorizontalAxis.m_InputAxisValue = 0f;
                    }
                    else
                    {
                        pov.m_HorizontalAxis.m_InputAxisName = !string.IsNullOrEmpty(_cachedHorizontalAxisName) ? _cachedHorizontalAxisName : "Mouse X";
                    }

                    if (shouldLockPitch)
                    {
                        pov.m_VerticalAxis.m_InputAxisName = string.Empty;
                        pov.m_VerticalAxis.m_InputAxisValue = 0f;
                    }
                    else
                    {
                        pov.m_VerticalAxis.m_InputAxisName = !string.IsNullOrEmpty(_cachedVerticalAxisName) ? _cachedVerticalAxisName : "Mouse Y";
                    }
                }
            }
            else if (_virtualCamera is CinemachineFreeLook freeLook)
            {
                if (!string.IsNullOrEmpty(freeLook.m_XAxis.m_InputAxisName))
                {
                    _cachedHorizontalAxisName = freeLook.m_XAxis.m_InputAxisName;
                }
                if (!string.IsNullOrEmpty(freeLook.m_YAxis.m_InputAxisName))
                {
                    _cachedVerticalAxisName = freeLook.m_YAxis.m_InputAxisName;
                }

                if (shouldLockYaw)
                {
                    freeLook.m_XAxis.m_InputAxisName = string.Empty;
                    freeLook.m_XAxis.m_InputAxisValue = 0f;
                }
                else
                {
                    freeLook.m_XAxis.m_InputAxisName = !string.IsNullOrEmpty(_cachedHorizontalAxisName) ? _cachedHorizontalAxisName : "Mouse X";
                }

                if (shouldLockPitch)
                {
                    freeLook.m_YAxis.m_InputAxisName = string.Empty;
                    freeLook.m_YAxis.m_InputAxisValue = 0f;
                }
                else
                {
                    freeLook.m_YAxis.m_InputAxisName = !string.IsNullOrEmpty(_cachedVerticalAxisName) ? _cachedVerticalAxisName : "Mouse Y";
                }
            }

            var inputProvider = _virtualCamera.GetComponent<CinemachineInputProvider>();
            if (inputProvider != null)
            {
                inputProvider.enabled = !shouldLockYaw && !shouldLockPitch;
            }
        }

        public void LockRotation(bool lockYaw, bool lockPitch)
        {
            _isYawLocked = lockYaw;
            _isPitchLocked = lockPitch;

            // 记录当前的锁定值，防止轴向漂移
            if (_virtualCamera is CinemachineVirtualCamera vcam)
            {
                var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                if (pov != null)
                {
                    _lockedYawValue = pov.m_HorizontalAxis.Value;
                    _lockedPitchValue = pov.m_VerticalAxis.Value;
                }
            }
            else if (_virtualCamera is CinemachineFreeLook freeLook)
            {
                _lockedYawValue = freeLook.m_XAxis.Value;
                _lockedPitchValue = freeLook.m_YAxis.Value;
            }

            ApplyAxisInputState();
        }

        public void UnlockRotation()
        {
            _isYawLocked = false;
            _isPitchLocked = false;
            ApplyAxisInputState();
        }

        public void StartRecenter(
            ATEditor.CameraRecenterTarget target,
            float smoothTime,
            float targetPitch,
            bool disableInput,
            float framingBiasAngle = -8.0f,
            float deadzoneAngle = 1.5f,
            bool allowSoftInput = true)
        {
            _recenterTarget = target;
            _recenterSmoothTime = Mathf.Max(0.05f, smoothTime);
            _targetPitch = targetPitch;
            _disableInputDuringRecenter = disableInput;
            _framingBiasAngle = framingBiasAngle;
            _deadzoneAngle = Mathf.Max(0.01f, deadzoneAngle);
            _allowSoftInputInterrupt = allowSoftInput;
            _recenterYawVelocity = 0f;
            _recenterPitchVelocity = 0f;
            _softInputBlendWeight = 1.0f;
            _isRecenterActive = true;

            if (_disableInputDuringRecenter)
            {
                EnableInput(false);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        public void UpdateRecenter(float deltaTime)
        {
            if (!_isRecenterActive || _virtualCamera == null || deltaTime <= 0f) return;

            Transform root = GetBindingRoot();
            if (root == null) return;

            // 4. 软输入融合与打断检测 (Soft Input Blending / Interruption)
            if (!_disableInputDuringRecenter && _allowSoftInputInterrupt)
            {
                float playerInputX = Mathf.Abs(UnityEngine.Input.GetAxis("Mouse X"));
                float playerInputY = Mathf.Abs(UnityEngine.Input.GetAxis("Mouse Y"));
                if (playerInputX > 0.05f || playerInputY > 0.05f)
                {
                    // 玩家主动控制视角，平滑衰减回正权重并优雅让权
                    _softInputBlendWeight -= deltaTime * 5.0f;
                    if (_softInputBlendWeight <= 0.05f)
                    {
                        StopRecenter(true);
                        return;
                    }
                }
                else
                {
                    _softInputBlendWeight = Mathf.MoveTowards(_softInputBlendWeight, 1.0f, deltaTime * 2.0f);
                }
            }

            // 1. 智能构图逻辑：计算目标水平偏航角 (Yaw)
            float targetYaw = 0f;

            switch (_recenterTarget)
            {
                case ATEditor.CameraRecenterTarget.CombatFraming:
                    // 智能战斗对峙构图：以角色与怪物的空间连线为中心，施加黄金分割侧向偏角
                    Transform framingCombatTarget = _entity != null && _entity.TargetFinder != null ? _entity.TargetFinder.GetEnemy() : null;
                    if (framingCombatTarget != null)
                    {
                        Vector3 toEnemy = framingCombatTarget.position - root.position;
                        toEnemy.y = 0;
                        if (toEnemy.sqrMagnitude > 0.01f)
                        {
                            float baseAngle = Quaternion.LookRotation(toEnemy.normalized).eulerAngles.y;
                            targetYaw = NormalizeAngle(baseAngle + _framingBiasAngle);
                        }
                        else
                        {
                            targetYaw = NormalizeAngle(root.eulerAngles.y + _framingBiasAngle);
                        }
                    }
                    else
                    {
                        targetYaw = NormalizeAngle(root.eulerAngles.y);
                    }
                    break;

                case ATEditor.CameraRecenterTarget.CharacterBack:
                    // 角色背后主视角：相机朝向与角色朝向一致，即从角色背后向前看
                    targetYaw = NormalizeAngle(root.eulerAngles.y);
                    break;

                case ATEditor.CameraRecenterTarget.MovementDirection:
                    // 移动突进方向：以移动输入或位移方向为对齐基准
                    Vector3 moveWorldDir = Vector3.zero;
                    if (_entity != null && _entity.MovementController != null && _entity.InputProvider != null)
                    {
                        Vector2 moveInput = _entity.InputProvider.GetMovementDirection();
                        if (moveInput.sqrMagnitude > 0.01f)
                        {
                            moveWorldDir = _entity.MovementController.CalculateWorldDirection(moveInput);
                        }
                    }

                    if (moveWorldDir.sqrMagnitude > 0.01f)
                    {
                        targetYaw = NormalizeAngle(Quaternion.LookRotation(moveWorldDir.normalized).eulerAngles.y);
                    }
                    else
                    {
                        targetYaw = NormalizeAngle(root.eulerAngles.y);
                    }
                    break;

                case ATEditor.CameraRecenterTarget.TargetDirection:
                    // 直接正对目标：无侧向偏移的死锁正面视角
                    Transform directCombatTarget = _entity != null && _entity.TargetFinder != null ? _entity.TargetFinder.GetEnemy() : null;
                    if (directCombatTarget != null)
                    {
                        Vector3 dir = directCombatTarget.position - root.position;
                        dir.y = 0;
                        if (dir.sqrMagnitude > 0.01f)
                        {
                            targetYaw = NormalizeAngle(Quaternion.LookRotation(dir.normalized).eulerAngles.y);
                        }
                        else
                        {
                            targetYaw = NormalizeAngle(root.eulerAngles.y);
                        }
                    }
                    else
                    {
                        targetYaw = NormalizeAngle(root.eulerAngles.y);
                    }
                    break;
            }

            // 2 & 3. 动力学模型与三轴协同（二阶临界阻尼 SmoothDampAngle / SmoothDamp）
            if (_virtualCamera is CinemachineVirtualCamera vcam)
            {
                var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                if (pov != null)
                {
                    float currentYaw = pov.m_HorizontalAxis.Value;
                    float diffYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

                    // 死区控制
                    if (Mathf.Abs(diffYaw) > _deadzoneAngle)
                    {
                        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _recenterYawVelocity, _recenterSmoothTime, 720f, deltaTime);
                        pov.m_HorizontalAxis.Value = newYaw;
                    }
                    else
                    {
                        _recenterYawVelocity = Mathf.MoveTowards(_recenterYawVelocity, 0f, 360f * deltaTime);
                    }

                    // 垂直俯仰角协同
                    if (Mathf.Abs(_targetPitch - (-999f)) > 0.1f)
                    {
                        float currentPitch = pov.m_VerticalAxis.Value;
                        float diffPitch = _targetPitch - currentPitch;
                        if (Mathf.Abs(diffPitch) > (_deadzoneAngle * 0.7f))
                        {
                            float newPitch = Mathf.SmoothDamp(currentPitch, _targetPitch, ref _recenterPitchVelocity, _recenterSmoothTime, 360f, deltaTime);
                            pov.m_VerticalAxis.Value = newPitch;
                        }
                        else
                        {
                            _recenterPitchVelocity = Mathf.MoveTowards(_recenterPitchVelocity, 0f, 180f * deltaTime);
                        }
                    }
                }
            }
            else if (_virtualCamera is CinemachineFreeLook freeLook)
            {
                float currentYaw = freeLook.m_XAxis.Value;
                float diffYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

                if (Mathf.Abs(diffYaw) > _deadzoneAngle)
                {
                    float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _recenterYawVelocity, _recenterSmoothTime, 720f, deltaTime);
                    freeLook.m_XAxis.Value = newYaw;
                }
                else
                {
                    _recenterYawVelocity = Mathf.MoveTowards(_recenterYawVelocity, 0f, 360f * deltaTime);
                }

                if (Mathf.Abs(_targetPitch - (-999f)) > 0.1f)
                {
                    float normY = Mathf.Clamp01((_targetPitch + 40f) / 80f);
                    float currentY = freeLook.m_YAxis.Value;
                    float diffY = normY - currentY;
                    if (Mathf.Abs(diffY) > 0.005f)
                    {
                        float newY = Mathf.SmoothDamp(currentY, normY, ref _recenterPitchVelocity, _recenterSmoothTime, 5f, deltaTime);
                        freeLook.m_YAxis.Value = newY;
                    }
                    else
                    {
                        _recenterPitchVelocity = Mathf.MoveTowards(_recenterPitchVelocity, 0f, 2f * deltaTime);
                    }
                }
            }
        }

        public void StopRecenter(bool restoreInput)
        {
            _isRecenterActive = false;
            _recenterYawVelocity = 0f;
            _recenterPitchVelocity = 0f;
            _softInputBlendWeight = 1.0f;

            if (restoreInput || _disableInputDuringRecenter)
            {
                EnableInput(true);
            }
        }

        public void StartLookAtTarget(Vector3 offset, float smoothSpeed, bool fallbackToCharacter)
        {
            _lookAtOffset = offset;
            _lookAtSmoothSpeed = Mathf.Max(0.1f, smoothSpeed);
            _lookAtFallbackToCharacter = fallbackToCharacter;
            _isLookAtActive = true;
            _cachedLookAtTransform = _virtualCamera != null ? _virtualCamera.LookAt : null;
        }

        public void UpdateLookAtTarget(float deltaTime)
        {
            if (!_isLookAtActive || _virtualCamera == null) return;

            Transform target = _entity != null && _entity.TargetFinder != null ? _entity.TargetFinder.GetEnemy() : null;
            if (target != null)
            {
                Vector3 targetLookPos = target.position + _lookAtOffset;
                Transform root = GetBindingRoot();
                if (root != null)
                {
                    Vector3 dir = targetLookPos - _virtualCamera.transform.position;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                        float targetYaw = NormalizeAngle(targetRot.eulerAngles.y);
                        float targetPitch = NormalizeAngle(targetRot.eulerAngles.x);

                        if (_virtualCamera is CinemachineVirtualCamera vcam)
                        {
                            var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                            if (pov != null)
                            {
                                float curYaw = pov.m_HorizontalAxis.Value;
                                float diffYaw = Mathf.DeltaAngle(curYaw, targetYaw);
                                float stepYaw = Mathf.Max(Mathf.Abs(diffYaw) * (1f - Mathf.Exp(-_lookAtSmoothSpeed * deltaTime)), _lookAtSmoothSpeed * 30f * deltaTime);
                                pov.m_HorizontalAxis.Value = Mathf.MoveTowardsAngle(curYaw, targetYaw, stepYaw);

                                float curPitch = pov.m_VerticalAxis.Value;
                                float diffPitch = targetPitch - curPitch;
                                float stepPitch = Mathf.Max(Mathf.Abs(diffPitch) * (1f - Mathf.Exp(-_lookAtSmoothSpeed * deltaTime)), _lookAtSmoothSpeed * 20f * deltaTime);
                                pov.m_VerticalAxis.Value = Mathf.MoveTowards(curPitch, targetPitch, stepPitch);
                            }
                        }
                    }
                }
            }
            else if (_lookAtFallbackToCharacter)
            {
                UpdateRecenter(deltaTime);
            }
        }

        public void StopLookAtTarget(bool restore)
        {
            _isLookAtActive = false;
            if (restore && _virtualCamera != null && _cachedLookAtTransform != null)
            {
                _virtualCamera.LookAt = _cachedLookAtTransform;
            }
        }

        public void SetCameraFOVAndDistance(float targetFOV, float targetDistance, float speed)
        {
            if (_virtualCamera == null) return;

            if (_originalFov < 0f)
            {
                if (_virtualCamera is CinemachineVirtualCamera vcam)
                {
                    _originalFov = vcam.m_Lens.FieldOfView;
                    var transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
                    if (transposer != null) _originalDistance = transposer.m_CameraDistance;
                }
                else if (_virtualCamera is CinemachineFreeLook freeLook)
                {
                    _originalFov = freeLook.m_Lens.FieldOfView;
                }
            }

            _targetFov = targetFOV;
            _targetDistance = targetDistance;
            _fovBlendSpeed = Mathf.Max(0.1f, speed);
            _isFovTransitionActive = true;
        }

        public void ResetCameraFOVAndDistance(float speed)
        {
            if (_originalFov > 0f)
            {
                _targetFov = _originalFov;
                _targetDistance = _originalDistance;
                _fovBlendSpeed = Mathf.Max(0.1f, speed);
            }
            else
            {
                _isFovTransitionActive = false;
            }
        }

        private void LateUpdate()
        {
            if (_virtualCamera == null || !Application.isPlaying) return;

            // 1. 如果处于轴向锁定状态且未被回正或注视驱动，则强制维持锁定角度
            if (!_isRecenterActive && !_isLookAtActive)
            {
                if (_virtualCamera is CinemachineVirtualCamera vcam)
                {
                    var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                    if (pov != null)
                    {
                        if (_isYawLocked) pov.m_HorizontalAxis.Value = _lockedYawValue;
                        if (_isPitchLocked) pov.m_VerticalAxis.Value = _lockedPitchValue;
                    }
                }
                else if (_virtualCamera is CinemachineFreeLook freeLook)
                {
                    if (_isYawLocked) freeLook.m_XAxis.Value = _lockedYawValue;
                    if (_isPitchLocked) freeLook.m_YAxis.Value = _lockedPitchValue;
                }
            }

            // 2. 处理 FOV 与 距离过渡
            if (_isFovTransitionActive)
            {
                float dt = Time.deltaTime;
                if (_virtualCamera is CinemachineVirtualCamera vcam)
                {
                    if (_targetFov > 0f)
                    {
                        vcam.m_Lens.FieldOfView = Mathf.Lerp(vcam.m_Lens.FieldOfView, _targetFov, dt * _fovBlendSpeed);
                    }
                    if (_targetDistance > 0f)
                    {
                        var transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
                        if (transposer != null)
                        {
                            transposer.m_CameraDistance = Mathf.Lerp(transposer.m_CameraDistance, _targetDistance, dt * _fovBlendSpeed);
                        }
                    }
                }
                else if (_virtualCamera is CinemachineFreeLook freeLook)
                {
                    if (_targetFov > 0f)
                    {
                        freeLook.m_Lens.FieldOfView = Mathf.Lerp(freeLook.m_Lens.FieldOfView, _targetFov, dt * _fovBlendSpeed);
                    }
                }
            }
        }
        #endregion
    }
}
