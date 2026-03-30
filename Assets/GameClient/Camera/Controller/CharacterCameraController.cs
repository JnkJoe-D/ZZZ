using UnityEngine;
using Cinemachine;
using System;
using Game.Logic.Character;

namespace Game.Camera
{
    /// <summary>
    /// 基于 Cinemachine 的实体专属控制桥接器
    /// 实现脱水接口，将自身逻辑需求委托给真实的相机系统运算
    /// </summary>
    public class CharacterCameraController : MonoBehaviour, ICameraController
    {
        [SerializeField]
        private GameObject _virtualCameraPrefab;
        [Tooltip("拖拽您预设在玩家组里的跟拍或全向虚拟相机")]
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

        private CinemachineImpulseSource _impluseSource;
        private void Awake()
        {
            if (UnityEngine.Camera.main != null)
            {
                _mainCamTransform = UnityEngine.Camera.main.transform;
                if (_mainCamTransform.GetComponent<CinemachineBrain>() == null)
                {
                    // Debug.LogError("[CharacterCameraController] 严重配置错误：当前主相机(Tag为MainCamera)上未找到 CinemachineBrain 组件！\nCinemachine 的虚拟相机必须依靠主相机上的 Brain 引擎来驱动，否则画面永远不会动。");
                }
            }
        }

        public void Init(CharacterEntity entity)
        {
            _entity=entity;
            if (_virtualCamera == null && _virtualCameraPrefab != null)
            {
                var obj = UnityEngine.Object.Instantiate(_virtualCameraPrefab);
                // obj.name = $"{_entity.name}_{virtualCamName}";
                _virtualCamera = obj.GetComponent<CinemachineVirtualCameraBase>();
                _impluseSource = obj.GetComponent<CinemachineImpulseSource>();
            }
            // 自动绑定跟拍
            if (_virtualCamera != null)
            {
                _virtualCamera.Follow = follow ?? this.transform;
                _virtualCamera.LookAt = lookAt ?? this.transform;
            }
        }
        /// <summary>
        /// 控制玩家能否通过鼠标转动视角
        /// </summary>
        public void EnableInput(bool enable)
        {
            if (_virtualCamera != null)
            {
                // 在接入了 New Input System 的 Cinemachine 2.x 版本中
                // 关闭其 Provider 组件即切断了鼠标的输入通道
                var inputProvider = _virtualCamera.GetComponent<CinemachineInputProvider>();
                if (inputProvider != null)
                {
                    inputProvider.enabled = enable;
                }
            }
        }

        /// <summary>
        /// 提供给移动状态机当前相机的正前方（降维到水平面）
        /// </summary>
        public Vector3 GetForward()
        {
            if (_mainCamTransform != null)
            {
                Vector3 forward = _mainCamTransform.forward;
                forward.y = 0;
                return forward.normalized;
            }
            // 兜底：如果连主相机也没有，直接沿着主角自身正前方向
            return transform.forward;
        }

        /// <summary>
        /// 提供给移动状态机的右向映射
        /// </summary>
        public Vector3 GetRight()
        {
            if (_mainCamTransform != null)
            {
                Vector3 right = _mainCamTransform.right;
                right.y = 0;
                return right.normalized;
            }
            return transform.right;
        }
        public void GenerateImpulse()
        {
            _impluseSource?.GenerateImpulse();
        }

        public void GenerateImpulseWithVelocity(Vector3 velocity, float force,float duration)
        {
            if (_impluseSource == null) return;
            
            var envelope = _impluseSource.m_ImpulseDefinition.m_TimeEnvelope;
            // 确保总时长匹配：Sustain = Total - Attack - Decay
            float attack = envelope.m_AttackTime;
            float decay = envelope.m_DecayTime;
            envelope.m_SustainTime = Mathf.Max(0, duration - attack - decay);
            
            // 如果 EnvelopeDefinition 是结构体，需要写回
            _impluseSource.m_ImpulseDefinition.m_TimeEnvelope = envelope;

            // 触发带速度偏移的脉冲
            _impluseSource.GenerateImpulseWithVelocity(velocity* force);
        }

        public GameObject CreateCamera(GameObject prefab)
        {
            if (prefab == null) return null;
            var instance = UnityEngine.Object.Instantiate(prefab, this.transform);

            return instance;
        }

        public void DestroyCamera(GameObject cameraInstance)
        {
            if (cameraInstance != null)
            {
                UnityEngine.Object.Destroy(cameraInstance);
            }
        }

        public void PlayCameraTimeline(GameObject cameraInstance, SkillEditor.CameraControlParams paramsObj)
        {
            if (cameraInstance == null || paramsObj == null || paramsObj.timelineAsset == null) return;

            var director = cameraInstance.GetComponent<UnityEngine.Playables.PlayableDirector>();
            if (director == null) director = cameraInstance.AddComponent<UnityEngine.Playables.PlayableDirector>();

            director.playableAsset = paramsObj.timelineAsset;

            // 处理相机设置覆盖
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

            // 获取虚拟相机以便动态绑定 Follow/LookAt
            var virtualCam = cameraInstance.GetComponentInChildren<CinemachineVirtualCameraBase>();
            var animator = cameraInstance.GetComponentInChildren<Animator>();
            
            if (virtualCam != null)
            {
                if (!string.IsNullOrEmpty(paramsObj.followBoneName))
                {
                    virtualCam.Follow = FindChildRecursive(this.transform, paramsObj.followBoneName);
                }
                if (!string.IsNullOrEmpty(paramsObj.lookAtBoneName))
                {
                    virtualCam.LookAt = FindChildRecursive(this.transform, paramsObj.lookAtBoneName);
                }
            }

            // 自动绑定
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

            // 订阅播放结束回调，自动销毁实例并重置相机设置
            director.stopped += (d) =>
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

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
