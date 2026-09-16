using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 全局状态机管理器
    /// 统一驱动所有的 FSMSystem 的 Update，避免大量的 MonoBehaviour.Update 带来的性能损耗
    /// 也负责分配和回收 FSM
    /// </summary>
    public class FSMManager : Game.Framework.MonoSingleton<FSMManager>
    {
        private readonly List<IFSMSystemRunner> _fsms = new List<IFSMSystemRunner>();
        private readonly List<IFSMSystemRunner> _pendingRemoveFsms = new List<IFSMSystemRunner>();
        private bool _isUpdating = false;

        public void Initialize()
        {
            GLog.Info(LogTags.FSM, "初始化完成");
        }

        public void Shutdown()
        {
            _fsms.Clear();
            _pendingRemoveFsms.Clear();
            GLog.Info(LogTags.FSM, "已关闭");
        }

        /// <summary>
        /// 为指定的 Owner 创建并分配一台状态机
        /// 状态机的轮询已自动接入全局驱动
        /// </summary>
        public FSMSystem<T> CreateFSM<T>(T owner)
        {
            var fsm = new FSMSystem<T>(owner);
            _fsms.Add(fsm);
            return fsm;
        }

        /// <summary>
        /// 销毁并回收状态机
        /// </summary>
        public void DestroyFSM<T>(FSMSystem<T> fsm)
        {
            if (fsm == null) return;
            fsm.Destroy();

            if (_isUpdating)
            {
                _pendingRemoveFsms.Add(fsm);
            }
            else
            {
                _fsms.Remove(fsm);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _isUpdating = true;
            try
            {
                for (int i = 0; i < _fsms.Count; i++)
                {
                    _fsms[i]?.Update(dt);
                }
            }
            finally
            {
                _isUpdating = false;
                if (_pendingRemoveFsms.Count > 0)
                {
                    for (int i = 0; i < _pendingRemoveFsms.Count; i++)
                    {
                        _fsms.Remove(_pendingRemoveFsms[i]);
                    }
                    _pendingRemoveFsms.Clear();
                }
            }
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            for (int i = 0; i < _fsms.Count; i++)
            {
                _fsms[i]?.FixedUpdate(fdt);
            }
        }
    }
}
