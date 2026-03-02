using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    public class LayerMaskState
    {
        public AvatarMask OriginalMask;
        public List<AvatarMask> ActiveOverrides = new List<AvatarMask>();
    }
    /// <summary>
    /// 播放上下文，为 Process 提供依赖注入：
    /// - 目标角色的 GameObject / Transform
    /// - 组件惰性缓存
    /// - 系统级清理注册（同 key 去重）
    /// </summary>
    public class ProcessContext
    {
        /// <summary>
        /// 目标角色
        /// </summary>
        public GameObject Owner { get; private set; }

        /// <summary>
        /// 目标角色的 Transform
        /// </summary>
        public Transform OwnerTransform { get; private set; }

        /// <summary>
        /// 当前播放模式
        /// </summary>
        public PlayMode PlayMode { get; private set; }

        /// <summary>
        /// 可选扩展数据（外部注入业务相关对象）
        /// </summary>
        public object UserData { get; set; }
        public float GlobalPlaySpeed { get; set; } = 1f; // 全局播放速度控制
        
        /// <summary>
        /// 标识当前的执行上下文是否正处于被打断清理状态
        /// 供 Process 在 OnExit 时判断是自然结束还是被强制终止
        /// </summary>
        public bool IsInterrupted { get; set; } = false;

        /// <summary>
        /// 当前播放的技能 ID（由 SkillRunner.Play 注入）
        /// </summary>
        public int SkillId { get; private set; }

        public void SetSkillId(int id) { SkillId = id; }
        
        // 单层字典，Key 为服务接口类型
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private IServiceFactory _serviceFactory; // 服务工厂（懒加载）
        // 组件缓存
        private Dictionary<Type, Component> _componentCache = new Dictionary<Type, Component>();
        // Mask托管栈
        private Dictionary<int, LayerMaskState> _layerMaskStates = new Dictionary<int, LayerMaskState>();

        // 系统级清理注册（同 key 去重）
        private Dictionary<string, Action> _cleanupActions = new Dictionary<string, Action>();

        public ProcessContext(GameObject owner, PlayMode playMode, IServiceFactory factory = null)
        {
            Owner = owner;
            OwnerTransform = owner != null ? owner.transform : null;
            PlayMode = playMode;
            _serviceFactory = factory;
        }

        /// <summary>
        /// 获取组件（惰性查找 + 缓存）
        /// </summary>
        public T GetComponent<T>() where T : Component
        {
            var type = typeof(T);
            if (!_componentCache.TryGetValue(type, out var comp))
            {
                if (Owner != null)
                {
                    comp = Owner.GetComponentInChildren<T>();
                    if (comp != null)
                    {
                        _componentCache[type] = comp;
                    }
                }
            }
            return (T)comp;
        }
        public void PushLayerMask(int layerIndex, AvatarMask overrideMask)
        {
            if (overrideMask == null) return;

            var animHandler = GetService<ISkillAnimationHandler>(); // 懒加载
            if (animHandler == null) return;

            if (!_layerMaskStates.TryGetValue(layerIndex, out var state))
            {
                // 第一次有 Clip 进入该层，记录原始 Mask
                state = new LayerMaskState();
                state.OriginalMask = animHandler.GetLayerMask(layerIndex);
                _layerMaskStates[layerIndex] = state;
            }

            // 入栈
            state.ActiveOverrides.Add(overrideMask);

            // 应用栈顶 Mask
            animHandler.SetLayerMask(layerIndex, overrideMask);
        }
        public void PopLayerMask(int layerIndex, AvatarMask overrideMask)
        {
            if (overrideMask == null) return;
            var animHandler = GetService<ISkillAnimationHandler>();
            if (animHandler == null) return;

            if (_layerMaskStates.TryGetValue(layerIndex, out var state))
            {
                // 移除该 Mask（处理中间退出的情况）
                if (state.ActiveOverrides.Remove(overrideMask))
                {
                    // 重新计算应生效的 Mask
                    if (state.ActiveOverrides.Count > 0)
                    {
                        // 还有其他 Override，应用栈顶（List 最后一个）
                        var topMask = state.ActiveOverrides[state.ActiveOverrides.Count - 1];
                        animHandler.SetLayerMask(layerIndex, topMask);
                    }
                    else
                    {
                        // 栈空，恢复原始 Mask
                        animHandler.SetLayerMask(layerIndex, state.OriginalMask);

                        // 可选：清理 State，节省内存（下次进入重新获取 Original）
                        _layerMaskStates.Remove(layerIndex);
                    }
                }
            }
        }

        public void AddService<T>(T service)
        {
            if (service == null) return;
            _services[typeof(T)] = service;
        }

        public T GetService<T>() where T : class
        {
            var type = typeof(T);
            
            // 1. 尝试从缓存获取
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            // 2. 尝试懒加载
            if (_serviceFactory != null)
            {
                var newService = _serviceFactory.ProvideService(type);
                if (newService != null && newService is T typedService)
                {
                    // 注册到缓存，下次直接获取
                    AddService<T>(typedService);
                    return typedService;
                }
            }
            return null;
        }

        public void RemoveService<T>()
        {
            _services.Remove(typeof(T));
        }
        /// <summary>
        /// 注册系统级清理操作（同 key 去重，后注册覆盖前注册）
        /// Process 在 OnEnable 中调用，Runner 结束时统一执行
        /// </summary>
        /// <param name="key">清理标识（如 "AnimComponent"），同类 Process 注册相同 key</param>
        /// <param name="cleanup">清理回调</param>
        public void RegisterCleanup(string key, Action cleanup)
        {
            _cleanupActions[key] = cleanup;
        }

        /// <summary>
        /// 执行所有注册的系统级清理（Runner 结束时调用）
        /// </summary>
        internal void ExecuteCleanups()
        {
            foreach (var action in _cleanupActions.Values)
            {
                action?.Invoke();
            }
            _cleanupActions.Clear();
        }

        /// <summary>
        /// 清空组件缓存和清理注册
        /// </summary>
        internal void Clear()
        {
            ExecuteCleanups();
            _services.Clear();
            _componentCache.Clear();
            _layerMaskStates.Clear();
        }
    }
}
