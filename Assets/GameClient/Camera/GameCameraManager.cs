using System;
using System.Collections.Generic;
using System.Resources;
using System.Threading.Tasks;
using Cinemachine;
using UnityEngine;

namespace Game.Camera
{
    /// <summary>
    /// 全局相机管理器
    /// 统一接口中心，防止多个业务脚本争夺主相机的 Transform 与视角
    /// 未来建议直接在此接入 Unity.Cinemachine 做平滑越肩封装
    /// </summary>
    public class GameCameraManager : Game.Framework.Singleton<GameCameraManager>
    {
        public UnityEngine.Camera MainCamera { get; private set; }
        public Transform MainCameraTransform => MainCamera != null ? MainCamera.transform : null;
        public Transform CurrentTarget { get; private set; }

        // public Cinemachine.CinemachineBrain Brain { get; private set; }

        public void Initialize()
        {
            ResolveMainCamera();

            if (MainCamera == null)
            {
                Debug.LogWarning("[GameCameraManager] 场景中未找到 Tag 为 MainCamera 的相机对象。");
            }
            Debug.Log("[GameCameraManager] 初始化完成");
        }

        public void Update(float deltaTime)
        {
            if (MainCamera == null)
            {
                ResolveMainCamera();
            }
        }

        public void Shutdown()
        {
            CurrentTarget = null;
            MainCamera = null;
            // Brain = null;
            Debug.Log("[GameCameraManager] 已关闭");
        }

        /// <summary>
        /// 设定相机的跟随和看点目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            CurrentTarget = target;

            if (MainCamera == null)
            {
                ResolveMainCamera();
            }

            if (target != null)
            {
                // TODO: 寻获当前的虚拟相机并在 Cinemachine 修改 Follow = target
                Debug.Log($"[GameCameraManager] 相机系统已绑定跟随目标：{target.name}");
            }
        }

        /// <summary>
        /// 检查相机当前是否在由一个位置平滑移动（Blending）到另一个位置
        /// </summary>
        public bool IsBlending()
        {
            // if (Brain != null) return Brain.IsBlending;
            return false;
        }

        /// <summary>
        /// 发起全局震屏事件 (接入 Cinemachine Impulse)
        /// 此类事件常由命中技能或 Boss 砸地产生
        /// </summary>
        /// <param name="impulseVelocity">震动三轴方向与力度</param>
        public void DoShake(Vector3 impulseVelocity)
        {
            // CinemachineImpulseSource.GenerateImpulse(impulseVelocity);
            Debug.Log($"[GameCameraManager] 触发震动打击: {impulseVelocity}");
        }

        /// <summary>
        /// 发起全局震屏事件
        /// </summary>
        /// <param name="intensity">震动强度</param>
        /// <param name="time">震动时长</param>
        public void DoShake(float intensity, float time)
        {
            // TODO: 调用 Cinemachine Impulse Source
        }

        private void ResolveMainCamera()
        {
            MainCamera = UnityEngine.Camera.main;
            // if (MainCamera != null) Brain = MainCamera.GetComponent<Cinemachine.CinemachineBrain>();
        }


        public async Task<GameObject> LoadCameraAsync(string path)
        {
            if(String.IsNullOrEmpty(path))return null;
            GameObject cam = await Game.Resource.ResourceManager.Instance.LoadAssetAsync<GameObject>(path);
            if(cam!=null)
            {
                
            }
            return cam;
        }
    }
}
