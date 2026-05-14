# MAnimSystem 接入 SkillEditor 分析报告

## 一、背景

MAnimSystem 是项目自研的动画系统，基于 Unity Playable API 实现。SkillEditor 是技能编辑器，需要通过 `IAnimationService` 接口调用动画服务。本文档分析 MAnimSystem 接入 SkillEditor 的可行性和所需扩展。

---

## 二、SkillEditor 动画服务接口分析

### 2.1 接口定义

```csharp
public interface IAnimationService
{
    void Play(UnityEngine.AnimationClip clip, float transitionDuration);  // 播放动画
    void Evaluate(float time);                                              // 采样到指定时间
    void ManualUpdate(float deltaTime);                                     // 手动更新
}
```

### 2.2 接口用途

| 方法 | 用途 | 场景 |
|------|------|------|
| `Play(clip, duration)` | 播放动画片段 | 运行时技能执行 |
| `Evaluate(time)` | 采样到指定时间 | 编辑器预览、时间轴拖拽 |
| `ManualUpdate(dt)` | 手动驱动更新 | 帧同步、时间缩放 |

### 2.3 调用流程

```
SkillRunner (技能运行器)
      │
      ▼
AnimationClipProcessor.OnEnter()
      │
      ▼
IAnimationService.Play(clip, duration)
      │
      ▼
动画系统执行播放
```

---

## 三、MAnimSystem 架构概述

### 3.1 类层次结构

```
AnimState (抽象基类)
    ├── ClipState        - 单个动画片段
    ├── MixerState       - 混合器基类
    │     ├── LinearMixerState   - 1D 线性混合
    │     └── BlendTreeState2D   - 2D 混合
    └── (可扩展)

AnimLayer (动画层管理)
    - 状态过渡、缓存、清理
    - 支持多层混合

AnimComponent (组件入口)
    - PlayableGraph 生命周期管理
    - 多层管理
```

### 3.2 数据流

```
AnimComponent.Update()
      │
      ▼
AnimLayer.OnUpdate(deltaTime)
      │
      ▼
状态过渡更新 → 权重归一化 → 事件触发
```

---

## 四、能力对比分析

### 4.1 功能对照表

| 功能 | SkillEditor 需求 | MAnimSystem 现状 | 差距 |
|------|------------------|------------------|------|
| 播放动画 | ✅ 需要 | ✅ 支持 `Play(clip, fadeDuration)` | 无 |
| 过渡时间 | ✅ 需要 | ✅ 支持 | 无 |
| 手动采样 | ✅ 需要 | ❌ 不支持 | **缺少** |
| 手动更新 | ✅ 需要 | ❌ 依赖 Unity Update | **缺少** |
| 时间控制 | ✅ 需要 | ⚠️ 部分支持 | 需扩展 |
| 动画速度 | ✅ 需要 | ✅ 支持 `Speed` 属性 | 无 |
| 多层混合 | ⚠️ 可选 | ✅ 支持 | 无 |
| 动画事件 | ⚠️ 可选 | ✅ 支持 `OnEnd` | 无 |
| 暂停/恢复 | ✅ 需要 | ✅ 支持 `Pause/Resume` | 无 |
| 播放状态查询 | ✅ 需要 | ⚠️ 部分支持 | 需扩展 |

### 4.2 缺少的核心功能

#### 4.2.1 手动更新模式

**问题**：MAnimSystem 依赖 `MonoBehaviour.Update()` 自动驱动，无法手动控制更新时机。

**SkillEditor 需求**：
```csharp
animComponent.SetUpdateMode(UpdateMode.Manual);
animComponent.ManualUpdate(deltaTime);
```

#### 4.2.2 时间采样功能

**问题**：MAnimSystem 没有提供"跳转到指定时间并采样"的功能。

**SkillEditor 需求**：
```csharp
animComponent.Evaluate(time);  // 采样到指定时间
```

#### 4.2.3 播放状态查询

**问题**：缺少查询当前播放状态的能力。

**SkillEditor 需求**：
```csharp
var currentClip = animComponent.GetCurrentClip();
var isPlaying = animComponent.IsPlaying(clip);
```

---

## 五、集成方案设计

### 5.1 架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                         SkillEditor                              │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              IAnimationService (接口)                      │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            │                                     │
│                            ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              MAnimAnimationService (适配器)                │  │
│  │  - AnimComponent _animComponent                            │  │
│  │  + Play(clip, duration)                                    │  │
│  │  + Evaluate(time)                                          │  │
│  │  + ManualUpdate(deltaTime)                                 │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            │                                     │
│                            ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              AnimComponent (扩展)                          │  │
│  │  + UpdateMode updateMode                                   │  │
│  │  + ManualUpdate(deltaTime)                                 │  │
│  │  + Evaluate(time)                                          │  │
│  │  + GetCurrentClip()                                        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 需要扩展的功能清单

| 优先级 | 功能 | 文件 | 工作量 |
|--------|------|------|--------|
| **P0** | UpdateMode 枚举 + ManualUpdate | AnimComponent.cs | 小 |
| **P0** | GetCurrentState() | AnimLayer.cs | 小 |
| **P1** | Evaluate(time) | AnimComponent.cs | 中 |
| **P1** | MAnimAnimationService 适配器 | 新建文件 | 小 |
| **P2** | GetCurrentClip() / IsPlaying() | AnimLayer.cs | 小 |

---

## 六、风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| PlayableGraph.Evaluate() 可能影响性能 | 中 | 仅在编辑器预览模式使用 |
| 手动模式与自动模式切换可能导致状态不一致 | 低 | 添加模式切换时的状态重置 |
| 多层混合时的采样逻辑复杂 | 中 | 仅采样 Layer 0 的状态 |

---

## 七、结论

**MAnimSystem 可以接入 SkillEditor**，需要扩展以下功能：

1. **必须实现 (P0)**：
   - 手动更新模式 (`UpdateMode.Manual`)
   - 状态查询 (`GetCurrentState()`)

2. **建议实现 (P1)**：
   - 时间采样 (`Evaluate(time)`)
   - 适配器 `MAnimAnimationService`

3. **工作量评估**：
   - 核心功能扩展：约 100-150 行代码
   - 适配器实现：约 50 行代码
   - 总工作量：约 2-3 小时

---

**文档日期**: 2026-02-13
