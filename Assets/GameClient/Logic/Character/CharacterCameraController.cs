using UnityEngine;
using Cinemachine;
using System;

namespace Game.Logic.Character
{
    /// <summary>
    /// 基于 Cinemachine 的实体专属控制桥接器
    /// 实现脱水接口，将自身逻辑需求委托给真实的相机系统运算
    /// </summary>
    public class CharacterCameraController : MonoBehaviour, ICameraController
    {
        [Tooltip("拖拽您预设在玩家组里的跟拍或全向虚拟相机")]
        [SerializeField]
        private CinemachineVirtualCameraBase _virtualCamera;
        [SerializeField]
        private string virtualCamName = "第三人称自由相机";
        [SerializeField]
        private Transform follow;
        [SerializeField]
        private Transform lookAt;
        private Transform _mainCamTransform;

        private void Awake()
        {
            if (UnityEngine.Camera.main != null)
            {
                _mainCamTransform = UnityEngine.Camera.main.transform;
                if (_mainCamTransform.GetComponent<CinemachineBrain>() == null)
                {
                    Debug.LogError("[CharacterCameraController] 严重配置错误：当前主相机(Tag为MainCamera)上未找到 CinemachineBrain 组件！\nCinemachine 的虚拟相机必须依靠主相机上的 Brain 引擎来驱动，否则画面永远不会动。");
                }
            }
            _virtualCamera = GameObject.Find(virtualCamName)?.GetComponent<CinemachineVirtualCameraBase>();
        }

        private void Start()
        {
            // 自动绑定跟拍
            if (_virtualCamera != null)
            {
                _virtualCamera.Follow = follow??this.transform; 
                _virtualCamera.LookAt = lookAt??this.transform;
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
    }
}
