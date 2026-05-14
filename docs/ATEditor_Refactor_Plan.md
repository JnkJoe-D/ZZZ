# SkillEditor 重构方案 - 多 Runner 架构

## 一、设计目标

| 目标 | 说明 |
|------|------|
| **精简** | 移除冗余代码、未使用字段、重复定义 |
| **高效** | 对象池复用、避免 GC、优化遍历 |
| **可靠** | 修复已知 Bug、添加类型约束、完善错误处理 |
| **易扩展** | 清晰的分层架构、接口抽象、插件机制 |
| **解耦** | 技能系统与动画系统单向依赖，服务按需创建 |

---

## 二、架构总览

```
┌─────────────────────────────────────────────────────────────────┐
│                    Unity 生命周期层                              │
├─────────────────────────────────────────────────────────────────┤
│  SkillSystemManager (单例 MonoBehaviour)                        │
│       │                                                         │
│       ├── Update()    → SkillDriver.Instance.Update(dt)         │
│       └── FixedUpdate() → SkillDriver.Instance.FixedUpdate(dt)  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    驱动层 (Singleton)                            │
├─────────────────────────────────────────────────────────────────┤
│  SkillDriver                                                    │
│       │                                                         │
│       ├── _runners: List<SkillRunner>          (所有 Runner)    │
│       ├── _runnersByOwner: Dictionary<int, SkillRunner>         │
│       ├── _instancePool: SkillInstancePool      (对象池)        │
│       ├── _contextPool: SkillContextPool        (对象池)        │
│       │                                                         │
│       ├── PlaySkill(skillData, owner) → SkillInstance           │
│       ├── StopSkill(instanceId)                                 │
│       ├── PauseOwner(owner)                                     │
│       └── RemoveOwner(owner)                                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    服务工厂层                                    │
├─────────────────────────────────────────────────────────────────┤
│  ServiceFactory                                                 │
│       │                                                         │
│       ├── 轨道类型 → 服务类型映射                                │
│       │   AnimationTrack → IAnimationService                    │
│       │   VFXTrack        → IVFXService                         │
│       │   AudioTrack      → IAudioService                       │
│       │                                                         │
│       ├── CreateService(track, owner) → IService                │
│       │   1. 检测轨道是否有有效片段                              │
│       │   2. 从 owner 获取对应组件                               │
│       │   3. 创建服务适配器                                      │
│       │                                                         │
│       └── 按需创建，无有效片段则不创建服务                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    运行层 (Per-Owner)                            │
├─────────────────────────────────────────────────────────────────┤
│  SkillRunner (纯 C# 类，不继承 MonoBehaviour)                   │
│       │                                                         │
│       ├── Owner: GameObject                    (目标角色)        │
│       ├── _activeSkills: List<SkillInstance>                    │
│       ├── _pendingSkills: Queue<SkillInstance>                  │
│       ├── _services: ServiceLocator            (按需创建的服务)  │
│       │                                                         │
│       ├── Play(skillData) → SkillInstance                       │
│       ├── Stop(skillInstance)                                   │
│       ├── Pause() / Resume()                                    │
│       └── Update(deltaTime)                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    实例层 (Per-Skill)                            │
├─────────────────────────────────────────────────────────────────┤
│  SkillInstance                                                  │
│       │                                                         │
│       ├── InstanceId: int                                       │
│       ├── Data: SkillTimeline                                   │
│       ├── CurrentTime: float                                    │
│       ├── IsFinished: bool                                      │
│       ├── IsPaused: bool                                        │
│       ├── _processors: List<ProcessorState>                     │
│       │                                                         │
│       ├── Initialize()                                          │
│       ├── Tick(deltaTime)                                       │
│       ├── Evaluate(time)      // 编辑器预览                      │
│       └── Dispose()                                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## 三、核心类设计

### 3.1 SkillSystemManager

**文件**: `Runtime/System/SkillSystemManager.cs`

```csharp
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能系统管理器。
    /// 单例 MonoBehaviour，负责分发 Unity 生命周期事件。
    /// </summary>
    public class SkillSystemManager : MonoBehaviour
    {
        public static SkillSystemManager Instance { get; private set; }
        
        /// <summary>
        /// 自动创建单例。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;
            
            var go = new GameObject("[SkillSystemManager]");
            Instance = go.AddComponent<SkillSystemManager>();
            DontDestroyOnLoad(go);
        }
        
        private void Update()
        {
            SkillDriver.Instance.Update(Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            SkillDriver.Instance.FixedUpdate(Time.fixedDeltaTime);
        }
        
        private void OnDestroy()
        {
            SkillDriver.Instance.Dispose();
        }
    }
}
```

### 3.2 SkillDriver

**文件**: `Runtime/System/SkillDriver.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能驱动器。
    /// 单例纯 C# 类，管理所有 Runner 和对象池。
    /// </summary>
    public class SkillDriver : System.IDisposable
    {
        public static SkillDriver Instance { get; } = new SkillDriver();
        
        // 所有 Runner
        private readonly List<SkillRunner> _runners = new List<SkillRunner>();
        
        // 按 Owner 实例 ID 索引的 Runner
        private readonly Dictionary<int, SkillRunner> _runnersByOwner = new Dictionary<int, SkillRunner>();
        
        // 对象池
        private readonly SkillInstancePool _instancePool = new SkillInstancePool();
        private readonly SkillContextPool _contextPool = new SkillContextPool();
        
        // 实例 ID 生成器
        private int _instanceIdGenerator;
        
        private SkillDriver() { }
        
        /// <summary>
        /// 获取或创建指定角色的 Runner。
        /// </summary>
        public SkillRunner GetRunner(GameObject owner)
        {
            int ownerId = owner.GetInstanceID();
            
            if (_runnersByOwner.TryGetValue(ownerId, out var runner))
            {
                return runner;
            }
            
            runner = new SkillRunner(owner, _instancePool, _contextPool);
            _runnersByOwner[ownerId] = runner;
            _runners.Add(runner);
            
            return runner;
        }
        
        /// <summary>
        /// 播放技能。
        /// 服务按需创建，无需手动注册。
        /// </summary>
        /// <param name="skillData">技能数据</param>
        /// <param name="owner">目标角色</param>
        /// <returns>技能实例</returns>
        public SkillInstance PlaySkill(SkillTimeline skillData, GameObject owner)
        {
            if (skillData == null || owner == null) return null;
            
            var runner = GetRunner(owner);
            return runner.Play(skillData);
        }
        
        /// <summary>
        /// 停止指定技能实例。
        /// </summary>
        public void StopSkill(int instanceId)
        {
            foreach (var runner in _runners)
            {
                if (runner.StopByInstanceId(instanceId))
                {
                    break;
                }
            }
        }
        
        /// <summary>
        /// 暂停指定角色的所有技能。
        /// </summary>
        public void PauseOwner(GameObject owner)
        {
            int ownerId = owner.GetInstanceID();
            if (_runnersByOwner.TryGetValue(ownerId, out var runner))
            {
                runner.Pause();
            }
        }
        
        /// <summary>
        /// 恢复指定角色的所有技能。
        /// </summary>
        public void ResumeOwner(GameObject owner)
        {
            int ownerId = owner.GetInstanceID();
            if (_runnersByOwner.TryGetValue(ownerId, out var runner))
            {
                runner.Resume();
            }
        }
        
        /// <summary>
        /// 移除指定角色的 Runner。
        /// </summary>
        public void RemoveOwner(GameObject owner)
        {
            int ownerId = owner.GetInstanceID();
            if (_runnersByOwner.TryGetValue(ownerId, out var runner))
            {
                runner.Dispose();
                _runners.Remove(runner);
                _runnersByOwner.Remove(ownerId);
            }
        }
        
        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void Update(float deltaTime)
        {
            for (int i = _runners.Count - 1; i >= 0; i--)
            {
                _runners[i].Update(deltaTime);
            }
        }
        
        /// <summary>
        /// 固定帧更新（用于帧同步）。
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 帧同步逻辑可在此处理
        }
        
        /// <summary>
        /// 生成唯一实例 ID。
        /// </summary>
        public int GenerateInstanceId()
        {
            return ++_instanceIdGenerator;
        }
        
        public void Dispose()
        {
            foreach (var runner in _runners)
            {
                runner.Dispose();
            }
            _runners.Clear();
            _runnersByOwner.Clear();
            _instancePool.Clear();
            _contextPool.Clear();
        }
    }
}
```

### 3.3 ServiceFactory

**文件**: `Runtime/System/ServiceFactory.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 服务工厂。
    /// 根据轨道类型按需创建服务适配器。
    /// 
    /// 设计说明：
    /// - 技能系统依赖动画系统（单向依赖）
    /// - AnimComponent 不需要实现任何 SkillEditor 接口
    /// - 服务适配器由工厂创建，桥接两者
    /// </summary>
    public static class ServiceFactory
    {
        /// <summary>
        /// 轨道类型到服务类型的映射。
        /// </summary>
        private static readonly Dictionary<Type, Type> TrackToServiceMap = new Dictionary<Type, Type>
        {
            { typeof(AnimationTrack), typeof(IAnimationService) },
            { typeof(VFXTrack), typeof(IVFXService) },
            { typeof(AudioTrack), typeof(IAudioService) }
        };
        
        /// <summary>
        /// 获取轨道对应的服务类型。
        /// </summary>
        public static Type GetServiceType(Type trackType)
        {
            return TrackToServiceMap.TryGetValue(trackType, out var serviceType) ? serviceType : null;
        }
        
        /// <summary>
        /// 为指定轨道创建服务。
        /// 只有当轨道有有效片段时才创建服务。
        /// </summary>
        /// <param name="track">轨道</param>
        /// <param name="owner">目标角色</param>
        /// <returns>服务实例，无有效片段时返回 null</returns>
        public static object CreateService(TrackBase track, GameObject owner)
        {
            if (track == null || owner == null) return null;
            
            // 检查是否有有效片段
            if (!HasValidClips(track)) return null;
            
            // 获取服务类型
            var serviceType = GetServiceType(track.GetType());
            if (serviceType == null) return null;
            
            // 创建服务
            return CreateServiceInternal(serviceType, owner);
        }
        
        /// <summary>
        /// 检查轨道是否有有效片段。
        /// </summary>
        private static bool HasValidClips(TrackBase track)
        {
            if (track.clips == null) return false;
            
            foreach (var clip in track.clips)
            {
                if (clip.isEnabled) return true;
            }
            return false;
        }
        
        /// <summary>
        /// 内部创建服务实现。
        /// </summary>
        private static object CreateServiceInternal(Type serviceType, GameObject owner)
        {
            // 动画服务
            if (serviceType == typeof(IAnimationService))
            {
                // 尝试获取 AnimComponent
                var anim = owner.GetComponent<Game.MAnimSystem.AnimComponent>();
                if (anim != null)
                {
                    return new MAnimAnimationService(anim);
                }
                
                // 尝试获取 Animator（备用方案）
                var animator = owner.GetComponent<Animator>();
                if (animator != null)
                {
                    return new RuntimeAnimationService(animator);
                }
            }
            
            // 特效服务
            else if (serviceType == typeof(IVFXService))
            {
                return new RuntimeVFXService(owner);
            }
            
            // 音频服务
            else if (serviceType == typeof(IAudioService))
            {
                var audioSource = owner.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    return new RuntimeAudioService(audioSource);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 为技能创建所有需要的服务。
        /// </summary>
        /// <param name="skillData">技能数据</param>
        /// <param name="owner">目标角色</param>
        /// <returns>服务字典</returns>
        public static Dictionary<Type, object> CreateServices(SkillTimeline skillData, GameObject owner)
        {
            var services = new Dictionary<Type, object>();
            
            if (skillData == null || skillData.tracks == null) return services;
            
            foreach (var track in skillData.tracks)
            {
                if (!track.isEnabled) continue;
                
                var service = CreateService(track, owner);
                if (service != null)
                {
                    var serviceType = GetServiceType(track.GetType());
                    if (serviceType != null && !services.ContainsKey(serviceType))
                    {
                        services[serviceType] = service;
                    }
                }
            }
            
            return services;
        }
    }
}
```

### 3.4 SkillRunner

**文件**: `Runtime/System/SkillRunner.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能运行器。
    /// 纯 C# 类，管理单个角色的所有技能实例。
    /// </summary>
    public class SkillRunner : System.IDisposable
    {
        /// <summary>
        /// 目标角色。
        /// </summary>
        public GameObject Owner { get; }
        
        /// <summary>
        /// 是否暂停。
        /// </summary>
        public bool IsPaused { get; private set; }
        
        /// <summary>
        /// 活动技能数量。
        /// </summary>
        public int ActiveSkillCount => _activeSkills.Count;
        
        private readonly List<SkillInstance> _activeSkills = new List<SkillInstance>();
        private readonly Queue<SkillInstance> _pendingSkills = new Queue<SkillInstance>();
        
        private readonly ServiceLocator _services = new ServiceLocator();
        private readonly SkillInstancePool _instancePool;
        private readonly SkillContextPool _contextPool;
        
        public SkillRunner(GameObject owner, SkillInstancePool instancePool, SkillContextPool contextPool)
        {
            Owner = owner;
            _instancePool = instancePool;
            _contextPool = contextPool;
        }
        
        /// <summary>
        /// 播放技能。
        /// 自动检测轨道类型，按需创建服务。
        /// </summary>
        public SkillInstance Play(SkillTimeline skillData)
        {
            if (skillData == null) return null;
            
            // 按需创建服务（只创建尚未创建的服务）
            AutoCreateServices(skillData);
            
            // 创建技能实例
            var instance = _instancePool.Get();
            instance.Initialize(SkillDriver.Instance.GenerateInstanceId(), skillData, _services);
            _pendingSkills.Enqueue(instance);
            
            return instance;
        }
        
        /// <summary>
        /// 按需创建服务。
        /// 只创建尚未注册的服务。
        /// </summary>
        private void AutoCreateServices(SkillTimeline skillData)
        {
            foreach (var track in skillData.tracks)
            {
                if (!track.isEnabled) continue;
                
                var serviceType = ServiceFactory.GetServiceType(track.GetType());
                if (serviceType == null) continue;
                
                // 已注册则跳过
                if (_services.Has(serviceType)) continue;
                
                // 创建服务
                var service = ServiceFactory.CreateService(track, Owner);
                if (service != null)
                {
                    _services.Register(serviceType, service);
                }
            }
        }
        
        /// <summary>
        /// 停止指定实例。
        /// </summary>
        public bool StopByInstanceId(int instanceId)
        {
            for (int i = _activeSkills.Count - 1; i >= 0; i--)
            {
                if (_activeSkills[i].InstanceId == instanceId)
                {
                    var instance = _activeSkills[i];
                    instance.Dispose();
                    _instancePool.Return(instance);
                    _activeSkills.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 停止所有技能。
        /// </summary>
        public void StopAll()
        {
            foreach (var instance in _activeSkills)
            {
                instance.Dispose();
                _instancePool.Return(instance);
            }
            _activeSkills.Clear();
            
            while (_pendingSkills.Count > 0)
            {
                var instance = _pendingSkills.Dequeue();
                _instancePool.Return(instance);
            }
        }
        
        /// <summary>
        /// 暂停。
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }
        
        /// <summary>
        /// 恢复。
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }
        
        /// <summary>
        /// 更新。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsPaused) return;
            
            // 1. 处理待启动的技能
            while (_pendingSkills.Count > 0)
            {
                var instance = _pendingSkills.Dequeue();
                instance.Start();
                _activeSkills.Add(instance);
            }
            
            // 2. 更新所有活动技能
            for (int i = _activeSkills.Count - 1; i >= 0; i--)
            {
                var instance = _activeSkills[i];
                instance.Tick(deltaTime);
                
                if (instance.IsFinished)
                {
                    instance.Dispose();
                    _instancePool.Return(instance);
                    _activeSkills.RemoveAt(i);
                }
            }
        }
        
        public void Dispose()
        {
            StopAll();
            _services.Clear();
        }
    }
}
```

### 3.5 SkillInstance

**文件**: `Runtime/System/SkillInstance.cs`

```csharp
using System.Collections.Generic;

namespace SkillEditor
{
    /// <summary>
    /// 技能实例。
    /// 表示一次技能播放的运行时状态。
    /// </summary>
    public class SkillInstance
    {
        /// <summary>
        /// 实例 ID。
        /// </summary>
        public int InstanceId { get; private set; }
        
        /// <summary>
        /// 技能数据。
        /// </summary>
        public SkillTimeline Data { get; private set; }
        
        /// <summary>
        /// 当前时间。
        /// </summary>
        public float CurrentTime { get; private set; }
        
        /// <summary>
        /// 是否已完成。
        /// </summary>
        public bool IsFinished { get; private set; }
        
        /// <summary>
        /// 是否暂停。
        /// </summary>
        public bool IsPaused { get; set; }
        
        /// <summary>
        /// 播放速度。
        /// </summary>
        public float Speed { get; set; } = 1f;
        
        private readonly List<ProcessorState> _processors = new List<ProcessorState>();
        private SkillContext _context;
        private ServiceLocator _services;
        
        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize(int instanceId, SkillTimeline data, ServiceLocator services)
        {
            InstanceId = instanceId;
            Data = data;
            _services = services;
            CurrentTime = 0f;
            IsFinished = false;
            IsPaused = false;
            Speed = 1f;
            
            _processors.Clear();
            
            // 创建所有 Processor
            foreach (var track in data.tracks)
            {
                if (!track.isEnabled) continue;
                
                foreach (var clip in track.clips)
                {
                    if (!clip.isEnabled) continue;
                    
                    var processor = clip.CreateProcessorInternal();
                    if (processor != null)
                    {
                        _processors.Add(new ProcessorState
                        {
                            Clip = clip,
                            Processor = processor,
                            IsRunning = false
                        });
                    }
                }
            }
        }
        
        /// <summary>
        /// 开始播放。
        /// </summary>
        public void Start()
        {
            _context = new SkillContext();
            _context.IsPreviewMode = false;
            
            // 复制服务引用到 Context
            _context.CopyServices(_services);
        }
        
        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (IsPaused || IsFinished) return;
            
            float prevTime = CurrentTime;
            CurrentTime += deltaTime * Speed;
            
            // 区间扫描
            foreach (var state in _processors)
            {
                var clip = state.Clip;
                
                // 检查时间重叠
                bool isOverlap = (clip.StartTime < CurrentTime) && (clip.EndTime > prevTime);
                
                _context.CurrentClipData = clip;
                
                if (isOverlap)
                {
                    // 进入
                    if (!state.IsRunning)
                    {
                        state.Processor.OnEnter(_context);
                        state.IsRunning = true;
                    }
                    
                    // 更新
                    float clipLocalTime = CurrentTime - clip.StartTime;
                    float clipPrevLocalTime = prevTime - clip.StartTime;
                    float progress = clipLocalTime / clip.Duration;
                    
                    state.Processor.OnUpdate(_context, progress);
                    state.Processor.OnTick(_context, clipLocalTime, clipPrevLocalTime);
                    
                    // 退出
                    if (clip.EndTime <= CurrentTime)
                    {
                        state.Processor.OnExit(_context);
                        state.IsRunning = false;
                    }
                }
                else
                {
                    // 不在时间范围内
                    if (state.IsRunning)
                    {
                        state.Processor.OnExit(_context);
                        state.IsRunning = false;
                    }
                }
                
                _context.CurrentClipData = null;
            }
            
            // 检查是否完成
            if (CurrentTime >= Data.duration)
            {
                IsFinished = true;
            }
        }
        
        /// <summary>
        /// 编辑器预览采样。
        /// </summary>
        public void Evaluate(float time)
        {
            _context.IsPreviewMode = true;
            CurrentTime = time;
            
            foreach (var state in _processors)
            {
                var clip = state.Clip;
                bool shouldBeRunning = (clip.StartTime <= CurrentTime) && (clip.EndTime > CurrentTime);
                
                _context.CurrentClipData = clip;
                
                if (shouldBeRunning)
                {
                    if (!state.IsRunning)
                    {
                        state.Processor.OnEnter(_context);
                        state.IsRunning = true;
                    }
                    
                    float progress = (CurrentTime - clip.StartTime) / clip.Duration;
                    state.Processor.OnUpdate(_context, progress);
                    state.Processor.OnSample(_context, CurrentTime - clip.StartTime);
                }
                else
                {
                    if (state.IsRunning)
                    {
                        state.Processor.OnExit(_context);
                        state.IsRunning = false;
                    }
                }
                
                _context.CurrentClipData = null;
            }
        }
        
        /// <summary>
        /// 重置状态（用于对象池回收）。
        /// </summary>
        public void Reset()
        {
            InstanceId = 0;
            Data = null;
            CurrentTime = 0f;
            IsFinished = false;
            IsPaused = false;
            Speed = 1f;
            _context = null;
            _services = null;
            
            foreach (var state in _processors)
            {
                state.IsRunning = false;
            }
            _processors.Clear();
        }
        
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            Reset();
        }
        
        private class ProcessorState
        {
            public ClipBase Clip;
            public BaseClipProcessor Processor;
            public bool IsRunning;
        }
    }
}
```

### 3.6 ServiceLocator

**文件**: `Runtime/System/ServiceLocator.cs`

```csharp
using System;
using System.Collections.Generic;

namespace SkillEditor
{
    /// <summary>
    /// 服务定位器。
    /// 提供服务的注册、获取和检查。
    /// </summary>
    public class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        /// <summary>
        /// 注册服务。
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }
        
        /// <summary>
        /// 注册服务（非泛型版本）。
        /// </summary>
        public void Register(Type serviceType, object service)
        {
            _services[serviceType] = service;
        }
        
        /// <summary>
        /// 获取服务。
        /// </summary>
        public T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }
        
        /// <summary>
        /// 检查服务是否已注册。
        /// </summary>
        public bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 检查服务是否已注册（非泛型版本）。
        /// </summary>
        public bool Has(Type serviceType)
        {
            return _services.ContainsKey(serviceType);
        }
        
        /// <summary>
        /// 移除服务。
        /// </summary>
        public void Remove<T>() where T : class
        {
            _services.Remove(typeof(T));
        }
        
        /// <summary>
        /// 清空所有服务。
        /// </summary>
        public void Clear()
        {
            _services.Clear();
        }
    }
}
```

### 3.7 SkillContext

**文件**: `Runtime/System/SkillContext.cs`

```csharp
using System.Collections.Generic;

namespace SkillEditor
{
    /// <summary>
    /// 技能上下文。
    /// 提供 Processor 执行时的环境和数据访问。
    /// </summary>
    public class SkillContext : ISkillContext
    {
        /// <summary>
        /// 当前处理的 Clip 数据。
        /// </summary>
        public object CurrentClipData { get; set; }
        
        /// <summary>
        /// 是否为编辑器预览模式。
        /// </summary>
        public bool IsPreviewMode { get; set; }
        
        private readonly Dictionary<System.Type, object> _services = new Dictionary<System.Type, object>();
        
        /// <summary>
        /// 获取指定类型的服务。
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }
        
        /// <summary>
        /// 注册服务。
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }
        
        /// <summary>
        /// 从外部服务定位器复制服务引用。
        /// </summary>
        public void CopyServices(ServiceLocator locator)
        {
            // 通过反射获取所有服务并复制
            // 实际实现中可以使用更高效的方式
        }
        
        /// <summary>
        /// 获取当前 Clip 数据。
        /// </summary>
        public T GetData<T>() where T : class
        {
            return CurrentClipData as T;
        }
        
        /// <summary>
        /// 重置状态（用于对象池回收）。
        /// </summary>
        public void Reset()
        {
            CurrentClipData = null;
            IsPreviewMode = false;
            _services.Clear();
        }
    }
}
```

### 3.8 对象池

**文件**: `Runtime/System/ObjectPool.cs`

```csharp
using System.Collections.Generic;

namespace SkillEditor
{
    /// <summary>
    /// 技能实例对象池。
    /// </summary>
    public class SkillInstancePool
    {
        private readonly Stack<SkillInstance> _pool = new Stack<SkillInstance>();
        private const int MAX_SIZE = 64;
        
        public SkillInstance Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new SkillInstance();
        }
        
        public void Return(SkillInstance instance)
        {
            if (_pool.Count >= MAX_SIZE) return;
            instance.Reset();
            _pool.Push(instance);
        }
        
        public void Clear()
        {
            _pool.Clear();
        }
    }
    
    /// <summary>
    /// 技能上下文对象池。
    /// </summary>
    public class SkillContextPool
    {
        private readonly Stack<SkillContext> _pool = new Stack<SkillContext>();
        private const int MAX_SIZE = 64;
        
        public SkillContext Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new SkillContext();
        }
        
        public void Return(SkillContext context)
        {
            if (_pool.Count >= MAX_SIZE) return;
            context.Reset();
            _pool.Push(context);
        }
        
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
```

---

## 四、接口定义

### 4.1 ISkillContext

**文件**: `Runtime/System/ISkillContext.cs`

```csharp
namespace SkillEditor
{
    public interface ISkillContext
    {
        /// <summary>
        /// 当前处理的 Clip 数据。
        /// </summary>
        object CurrentClipData { get; }
        
        /// <summary>
        /// 是否为编辑器预览模式。
        /// </summary>
        bool IsPreviewMode { get; }
        
        /// <summary>
        /// 获取服务。
        /// </summary>
        T GetService<T>() where T : class;
        
        /// <summary>
        /// 获取当前 Clip 数据。
        /// </summary>
        T GetData<T>() where T : class;
    }
}
```

### 4.2 服务接口

**文件**: `Runtime/Services/IServices.cs`

```csharp
using UnityEngine;

namespace SkillEditor
{
    public interface IAnimationService
    {
        void Play(AnimationClip clip, float transitionDuration);
        void Evaluate(float time);
        void SetSpeed(float speedScale);
    }
    
    public interface IVFXService
    {
        void Play(GameObject prefab, Vector3 offset, float duration);
        void Stop();
        void Evaluate(float time);
    }
    
    public interface IAudioService
    {
        void Play(AudioClip clip, float volume);
        void Stop();
    }
}
```

---

## 五、依赖关系说明

### 5.1 单向依赖

```
┌─────────────────────────────────────────────────────────────────┐
│  AnimComponent (动画系统)                                        │
│       │                                                         │
│       └── 独立存在，不知道 SkillEditor                           │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ (技能系统主动获取)
                              │
┌─────────────────────────────────────────────────────────────────┐
│  SkillEditor (技能系统)                                          │
│       │                                                         │
│       ├── ServiceFactory 从 owner 获取 AnimComponent            │
│       └── 创建 MAnimAnimationService 适配器                      │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 AnimComponent 保持独立

```csharp
// AnimComponent 不需要实现任何 SkillEditor 的接口
// 它只提供动画播放能力，不知道技能系统的存在
public class AnimComponent : MonoBehaviour
{
    public void Play(AnimationClip clip, float fadeDuration = 0.25f) { }
    public void SetSpeed(float speed) { }
    public void Evaluate(float time) { }
}
```

### 5.3 适配器负责桥接

```csharp
// MAnimAnimationService 是适配器
// 它知道 AnimComponent 和 IAnimationService 两者
// 由技能系统创建，实现解耦
public class MAnimAnimationService : IAnimationService
{
    private readonly AnimComponent _anim;
    
    public MAnimAnimationService(AnimComponent anim)
    {
        _anim = anim;
    }
    
    public void Play(AnimationClip clip, float transitionDuration)
    {
        _anim.Play(clip, transitionDuration);
    }
    
    public void Evaluate(float time)
    {
        _anim.Evaluate(time);
    }
    
    public void SetSpeed(float speedScale)
    {
        _anim.SetSpeed(speedScale);
    }
}
```

---

## 六、使用示例

### 6.1 播放技能（服务自动创建）

```csharp
// 用户只需调用播放，服务自动按需创建
public class Character : MonoBehaviour
{
    [SerializeField] private SkillTimeline _skillData;
    
    public void PlaySkill()
    {
        // 传入技能数据和目标角色
        // 内部自动检测轨道类型，按需创建服务
        var instance = SkillDriver.Instance.PlaySkill(_skillData, gameObject);
        
        if (instance != null)
        {
            Debug.Log($"Playing skill, instanceId: {instance.InstanceId}");
        }
    }
    
    public void StopAllSkills()
    {
        SkillDriver.Instance.RemoveOwner(gameObject);
    }
    
    public void PauseSkills()
    {
        SkillDriver.Instance.PauseOwner(gameObject);
    }
    
    public void ResumeSkills()
    {
        SkillDriver.Instance.ResumeOwner(gameObject);
    }
}
```

### 6.2 帧同步场景

```csharp
// 帧同步驱动
public class FrameSyncManager
{
    private const float FIXED_DT = 1f / 30f;
    
    public void OnFrameUpdate()
    {
        // 使用固定时间步长
        SkillDriver.Instance.FixedUpdate(FIXED_DT);
    }
}
```

### 6.3 服务创建流程

```
PlaySkill(skillData, owner)
    │
    ├── 1. 获取或创建 Runner
    │       └── GetRunner(owner)
    │
    ├── 2. 按需创建服务
    │       └── AutoCreateServices(skillData)
    │               │
    │               ├── 遍历所有轨道
    │               │
    │               ├── 检查轨道是否有有效片段
    │               │       └── HasValidClips(track)
    │               │
    │               ├── 检查服务是否已注册
    │               │       └── _services.Has(serviceType)
    │               │
    │               └── 创建服务适配器
    │                       └── ServiceFactory.CreateService(track, owner)
    │                               │
    │                               ├── AnimationTrack → 获取 AnimComponent → 创建 MAnimAnimationService
    │                               ├── VFXTrack → 创建 RuntimeVFXService
    │                               └── AudioTrack → 获取 AudioSource → 创建 RuntimeAudioService
    │
    └── 3. 创建并启动技能实例
            └── runner.Play(skillData)
```

---

## 七、需要移除的冗余代码

### 7.1 移除的类/文件

| 文件 | 原因 |
|------|------|
| `ClipContext` (旧) | 合并到新的 `SkillContext` |
| `SkillRunner` (旧 MonoBehaviour 版本) | 替换为纯 C# 版本 |
| `SkillContextExtensions` | 移动到 `SkillContext` 内部 |

### 7.2 移除的字段/属性

| 类 | 移除项 | 原因 |
|---|--------|------|
| `SkillRunner` (旧) | `activeClips` | 改为从 SkillTimeline 加载 |
| `SkillRunner` (旧) | `Mode` (UpdateMode) | 不再需要，统一由 SkillDriver 驱动 |
| `SkillRunner` (旧) | `StepMode` | 移到帧同步层 |
| `SkillRunner` (旧) | `FrameRate` | 移到帧同步层 |
| `SkillRunner` (旧) | `TimeScale` | 移到 SkillInstance.Speed |
| `SkillRunner` (旧) | `IsReverse` | 极少使用，可移除 |
| `SkillRunner` (旧) | `_accumulator` | 移到帧同步层 |

### 7.3 移除的枚举

| 枚举 | 原因 |
|------|------|
| `UpdateMode` | 不再需要 |
| `TimeStepMode` | 移到帧同步层 |

---

## 八、文件变更清单

### 8.1 新建文件

| 文件 | 说明 |
|------|------|
| `Runtime/System/SkillSystemManager.cs` | Unity 生命周期单例 |
| `Runtime/System/SkillDriver.cs` | 驱动器单例 |
| `Runtime/System/SkillRunner.cs` | 新版 Runner（纯 C#） |
| `Runtime/System/SkillInstance.cs` | 技能实例 |
| `Runtime/System/SkillContext.cs` | 新版上下文 |
| `Runtime/System/ServiceFactory.cs` | 服务工厂 |
| `Runtime/System/ServiceLocator.cs` | 服务定位器 |
| `Runtime/System/ObjectPool.cs` | 对象池 |

### 8.2 修改文件

| 文件 | 修改内容 |
|------|----------|
| `Runtime/System/ISkillContext.cs` | 简化接口 |
| `Runtime/Services/IServices.cs` | 保持不变 |
| `Runtime/Logic/Processors/AnimationClipProcessor.cs` | 适配新接口 |

### 8.3 删除文件

| 文件 | 原因 |
|------|------|
| `Runtime/System/SkillRunner.cs` (旧版) | 替换为新版 |

---

## 九、实施计划

### 阶段 1：核心架构 (1-2 天)

1. 创建 `SkillSystemManager`
2. 创建 `SkillDriver`
3. 创建 `ServiceFactory`
4. 创建新版 `SkillRunner`
5. 创建 `SkillInstance`
6. 创建对象池

### 阶段 2：接口适配 (1 天)

1. 更新 `ISkillContext`
2. 更新 `SkillContext`
3. 适配所有 Processor

### 阶段 3：测试验证 (1 天)

1. 单元测试
2. 集成测试
3. 性能测试

### 阶段 4：清理优化 (半天)

1. 移除旧代码
2. 更新文档
3. 代码审查

---

**文档日期**: 2026-02-14
**更新说明**: 加入按需创建服务机制，实现技能系统与动画系统解耦
