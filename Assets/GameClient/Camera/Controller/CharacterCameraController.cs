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
        }

        public void EnableInput(bool enable)
        {
            if (_virtualCamera == null)
            {
                return;
            }

            CinemachineInputProvider inputProvider = _virtualCamera.GetComponent<CinemachineInputProvider>();
            if (inputProvider != null)
            {
                inputProvider.enabled = enable;
            }
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
                return;
            }

            ReleaseOwnedVirtualCamera();
            _virtualCamera = virtualCamera;
            _impluseSource = ResolveImpulseSource(virtualCamera);
            RefreshVirtualCameraTargets();
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
    }
}
