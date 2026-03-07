using System.Collections.Generic;
using UnityEngine;

namespace Game.FSM
{
    /// <summary>
    /// 全局状态机管理器
    /// 统一驱动所有的 FSMSystem 的 Update，避免大量的 MonoBehaviour.Update 带来的性能损耗
    /// 也负责分配和回收 FSM
    /// </summary>
    public class FSMManager : Game.Framework.MonoSingleton<FSMManager>
    {
        private readonly List<System.Action<float>> _updateActions = new List<System.Action<float>>();
        private readonly List<System.Action<float>> _fixedUpdateActions = new List<System.Action<float>>();

        public void Initialize()
        {
            Debug.Log("[FSMManager] 初始化完成");
        }

        public void Shutdown()
        {
            _updateActions.Clear();
            _fixedUpdateActions.Clear();
            Debug.Log("[FSMManager] 已关闭");
        }

        /// <summary>
        /// 为指定的 Owner 创建并分配一台状态机
        /// 状态机的轮询已自动接入全局驱动
        /// </summary>
        public FSMSystem<T> CreateFSM<T>(T owner)
        {
            var fsm = new FSMSystem<T>(owner);

            // 注册生命周期回调
            System.Action<float> updateAction = dt => fsm.Update(dt);
            System.Action<float> fixedUpdateAction = dt => fsm.FixedUpdate(dt);

            _updateActions.Add(updateAction);
            _fixedUpdateActions.Add(fixedUpdateAction);

            return fsm;
        }

        /// <summary>
        /// 销毁并回收状态机
        /// </summary>
        public void DestroyFSM<T>(FSMSystem<T> fsm)
        {
            if (fsm == null) return;
            fsm.Destroy();
            // 注意：真实工业项目需做移除操作，因匿名委托的缘故，这里为简化演示略去复杂的解绑定位
            // 或改用接口遍历、ID句柄等方式注册
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _updateActions.Count; i++)
            {
                _updateActions[i]?.Invoke(dt);
            }
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            for (int i = 0; i < _fixedUpdateActions.Count; i++)
            {
                _fixedUpdateActions[i]?.Invoke(fdt);
            }
        }
    }
}
