using Game.Resource;
using Cinemachine;
using Game.Camera;
using SkillEditor;
using UnityEditor;
using UnityEngine;

namespace Game.Adapters
{
    /// <summary>
    /// 技能相机 Handler 适配器
    /// 
    /// 采用 SetCamera 缓存模式：
    /// 1. 外部在技能释放前，通过 RegisterCamera 预注册 (cameraIndex → VirtualCamera) 的映射
    /// 2. Process 在 OnEnter 时调用 SetCamera(index)，Handler 缓存目标相机的 TrackedDolly 引用
    /// 3. Process 在 OnUpdate 时调用 SetPathPosition(float)，直接驱动已缓存组件
    /// 4. Process 在 OnExit 时调用 ReleaseCamera()，清除缓存
    /// </summary>
    public class SkillCameraHandler : ISkillCameraHandler, System.IDisposable
    {
        // cameraIndex → 预注册的虚拟相机
        private readonly System.Collections.Generic.Dictionary<int, CinemachineVirtualCamera> _cameraRegistry
            = new System.Collections.Generic.Dictionary<int, CinemachineVirtualCamera>();

        // 当前正在操作的 Dolly 组件（缓存）
        private CinemachineTrackedDolly _activeDolly;
        
        // 当前正在操作的虚拟相机
        private CinemachineVirtualCamera _activeCamera;

        public void Dispose()
        {
            foreach (var kvp in _cameraRegistry)
            {
                if (kvp.Value != null)
                {
                    // 保证摧毁由 Instantiate 产生并在子节点挂接组件的整个 Root 根对象
                    Object.DestroyImmediate(kvp.Value.transform.root.gameObject);
                }
            }
            ClearRegistry();
        }
        public SkillCameraHandler()
        {
            CameraConfig config;
#if UNITY_EDITOR
            config = AssetDatabase.LoadAssetAtPath<ScriptableObject>
            ($"Assets/Resources/Serializations/ScriptableObjects/10001.asset") as CameraConfig;
#else
            config = ResourceManager.Instance.LoadAsset<CameraConfig>
            ($"Assets/Resources/Serializations/ScriptableObjects/10001.asset");
#endif
            if (config!=null)
            {
                if(config.prefab!=null)
                {
                    var prefab = Object.Instantiate(config.prefab);
                    RegisterCamera(10001,prefab.GetComponentInChildren<CinemachineVirtualCamera>());
                }
            }
        }
        // ─── 外部预注册 API ───

        /// <summary>
        /// 注册一个虚拟相机到指定索引位
        /// 应在技能释放前由战斗系统/关卡管理器调用
        /// </summary>
        public void RegisterCamera(int cameraIndex, CinemachineVirtualCamera vcam)
        {
            if (vcam == null) return;
            _cameraRegistry[cameraIndex] = vcam;
        }

        /// <summary>
        /// 清空所有注册（技能完全结束后应调用）
        /// </summary>
        public void ClearRegistry()
        {
            _cameraRegistry.Clear();
            _activeDolly = null;
            _activeCamera = null;
        }

        // ─── ISkillCameraHandler 实现 ───

        public void SetCamera(int cameraIndex, GameObject target)
        {
            _activeDolly = null;
            _activeCamera = null;

            if (!_cameraRegistry.TryGetValue(cameraIndex, out var vcam))
            {
                Debug.LogWarning($"[SkillCameraHandler] 未找到 cameraIndex={cameraIndex} 对应的虚拟相机。请确认已调用 RegisterCamera。");
                return;
            }

            _activeCamera = vcam;

            // 绑定跟随目标
            if (target != null)
            {
                vcam.Follow = target.transform;
                var aim = target.transform.Find("Aim");
                vcam.LookAt = aim ?? target.transform;
            }

            // 获取 Body 上的 TrackedDolly 扩展
            // Cinemachine 2.x: GetCinemachineComponent<CinemachineTrackedDolly>()
            _activeDolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (_activeDolly == null)
            {
                Debug.LogWarning($"[SkillCameraHandler] cameraIndex={cameraIndex} 的虚拟相机上未找到 CinemachineTrackedDolly 组件。" +
                                 "请确保该虚拟相机的 Body 设置为 Tracked Dolly。");
                return;
            }

            // 激活虚拟相机
            vcam.gameObject.SetActive(true);
        }

        public void SetPathPosition(float position)
        {
            if (_activeDolly != null)
            {
                _activeDolly.m_PathPosition = position;
                // Debug.Log($"POS:<color=#00FF00>{_activeDolly.m_PathPosition}</color>");
            }
            if (_activeCamera != null)
            {
                // 在编辑器非运行模式下，手动滑动 Timeline 时 Cinemachine Brain 通常不执行 FixedUpdate 或 LateUpdate
                // 这里我们调用 InternalUpdateCameraState 强制其在此刻依据新 PathPosition 计算矩阵
                _activeCamera.InternalUpdateCameraState(Vector3.up, -1f);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // 若无此步，Scene 视图画面可能仍然卡在上一帧直到下次主动重绘
                    SceneView.RepaintAll();
                }
#endif
            }
        }

        public void ReleaseCamera()
        {
            if (_activeCamera != null)
            {
                // 关闭虚拟相机，Cinemachine Brain 会自动切回优先级更高的默认相机
                _activeCamera.gameObject.SetActive(false);
            }

            _activeDolly = null;
            _activeCamera = null;
        }
    }
}