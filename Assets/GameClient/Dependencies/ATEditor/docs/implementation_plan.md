# SkillEditor 架构重构计划

## 目标
将 `SkillEditor` 从当前的单体耦合架构重构为分层解耦架构（数据-编辑器-运行时分离）。目标是提高代码的可读性、可维护性和扩展性，使其能够支持更复杂的技能逻辑，并实现纯数据驱动的运行时。

## 涉及文件
- `D:\Unity\Server_Game\Assets\SkillEditor\**`

## 用户确认事项
> [!WARNING]
> 本次重构将对现有的 `TrackBase` 和 `ClipBase` 进行重大修改，可能会破坏现有的序列化数据。

## 实施方案

### 1. 架构理念 (Architecture Philosophy)
采用 **Data-Driver-View** 分离架构。

*   **Data (Runtime Assembly)**: 纯数据类 (PCO)，无任何逻辑引用，仅包含 `[SerializeField]`。
*   **Driver/Logic (Runtime Assembly)**: 逻辑驱动层。定义 `Processor` 接口，负责具体的业务逻辑执行。
*   **View/Drawer (Editor Assembly)**: 编辑器绘制层。负责 Inspector 和 Timeline 上的可视化。

### 2. 详细设计 (Detailed Design)

#### 2.1 数据层 (Data) - `Runtime/Data`
```csharp
[Serializable]
public abstract class SkillClip<TProcessor> : ISkillClipData where TProcessor : BaseClipProcessor {
    public float startTime;
    public float duration;
}
```

#### 2.2 核心驱动层 (Core) - `Runtime/System`
`SkillRunner` 充当时间权威，支持手动更新 (ManualUpdate) 以适应帧同步。

```csharp
public class SkillRunner : MonoBehaviour {
    public void ManualUpdate(float deterministicDeltaTime) { ... }
}
```

#### 2.3 逻辑层 (Logic) - `Runtime/Logic`
`Processor` 通过 `ISkillContext` 获取服务，实现逻辑与环境解耦。

```csharp
public abstract class BaseClipProcessor {
    public abstract void OnEnter(ISkillContext context);
    public abstract void OnUpdate(ISkillContext context, float progress);
    public abstract void OnExit(ISkillContext context);
}
```

#### 2.4 服务层 (Services) - `Runtime/Services`
通过注入不同的 Service 实现来处理 Runtime/Editor 以及 Client/Server 的差异。
- `IAnimationService`: 处理动画播放（Runtime直接播放，Editor手动采样）。
- `IVFXService`: 处理特效（支持混合更新策略）。

### 3. 可扩展性验证 (Extensibility)
以 "震屏轨道" (ShakeTrack) 为例：
1.  **Data**: `ShakeClip` 定义强度。
2.  **Logic**: `ShakeProcessor` 调用 `ICameraService`。
3.  **Impl**: `RuntimeCameraService` (DOTween) vs `EditorCameraService` (Log)。

### 4. 网络同步
- **Client**: `SkillRunner.ManualUpdate` + 预测逻辑 + 表现层插值。
- **Server**: 仅运行 Logic Tracks，使用 `NullViewService` 屏蔽表现。

## 实施步骤
1.  **Phase 1: Core Framework**: 搭建目录，定义核心接口 (`SkillRunner`, `ISkillContext`, `BaseClipProcessor`).
2.  **Phase 2: Data Migration**: 拆分 `OtherTracks.cs`，净化数据类.
3.  **Phase 3: Logic Implementation**: 实现具体的 Processor 和 Services.
4.  **Phase 4: Editor Refactoring**: 实现 Drawer 和 PreviewPlayer.
