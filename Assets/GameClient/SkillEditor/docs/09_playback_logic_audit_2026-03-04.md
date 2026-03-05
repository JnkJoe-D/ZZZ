# SkillEditor 播放逻辑全链路审计报告（2026-03-04）

## 1. 目标与范围

本报告针对当前 `SkillEditor` 的三条播放链路进行审计：

- 编辑器正常预览播放（Play/Pause/Stop）
- 编辑器单帧预览与 Seek（标尺拖动、前后帧、首尾帧）
- 运行时播放（`SkillRunner.Tick` 驱动）

审计重点：

- 逻辑链路错误（调用顺序、状态机边界）
- 设计缺陷（职责边界、状态一致性）
- 字段使用不当（字段“有定义无语义”或“语义与实现不一致”）
- 逻辑漏洞（未覆盖场景、边界不闭环）

---

## 2. 当前播放链路梳理

## 2.1 编辑器正常预览播放

入口主要在：

- `ToolbarView.OnTogglePlay()` -> `SkillEditorWindow.TogglePlay()`
- `SkillEditorWindow.StartPreview()` 创建 `ProcessContext` 并 `previewRunner.Play(...)`
- `SkillEditorWindow.Update()` -> `UpdatePreview()`

关键时序（简化）：

1. `StartPreview()`
2. 捕获预览初始位姿（origin）
3. 应用主动画轨道基准位姿（`AnimationTrack.offsetPos/offsetRot`）
4. 构建 `ProcessContext`
5. `SkillRunner.Play()`: `BuildProcesses` -> 所有 `Process.OnEnable` -> `ExecuteStartActionsOnce`
6. 每帧 `UpdatePreview()` 调 `previewRunner.Tick(dt)`

---

## 2.2 编辑器单帧预览 / Seek

入口主要在：

- `StepForward/StepBackward/JumpToStart/JumpToEnd/SeekPreview`

关键时序（简化）：

1. 若正在播放，先 `TogglePlay()` 转暂停
2. `EnsureRunnerActive()`（Idle 时会 `StartPreview` + `PausePreview`）
3. `SeekWithPreviewTrackBase(targetTime, deltaTime)`
4. 先重置到轨道基准位姿，再 `previewRunner.Seek(...)`
5. 更新 `state.timeIndicator`

---

## 2.3 运行时播放

当前代码中，运行时主要由外部脚本直接驱动：

- `new SkillRunner(PlayMode.Runtime)`
- `runner.Play(timeline, context)`
- 外部 `Update` 循环内手动 `runner.Tick(step)`（测试脚本普遍是 30fps 固定步）

`SkillLifecycleManager` 存在，但当前仓库实际调用点非常弱（基本未形成统一接入）。

---

## 3. 问题总览（按严重度）

| 编号 | 严重度 | 类型 | 核心结论 |
|---|---|---|---|
| P0-01 | 严重 | 链路错误 | 编辑器存在“双时钟驱动”，`state.timeIndicator` 被两套逻辑同时推进。 |
| P0-02 | 严重 | 状态机边界 | `Tick` 与 `Seek` 的区间判定不一致：一个 `(start, end]`，一个 `[start, end)`。 |
| P0-03 | 严重 | 字段/参数滥用 | `Seek` 的 `deltaTime` 复用 `SnapInterval`，变量步长模式下会传 `-1`。 |
| P0-04 | 严重 | 逻辑漏洞 | 同轨片段不按 `startTime` 排序执行，重叠与同帧进入顺序依赖历史编辑顺序。 |
| P0-05 | 严重 | 设计缺陷 | 动画状态按 `AnimationClip` 资源识别，导致“同资源多片段”共享状态。 |
| P1-01 | 高 | 设计缺陷 | `isMasterTrack` 约束只在 Inspector 绘制时修正，不是数据层强约束。 |
| P1-02 | 高 | 字段使用不当 | `Group.isEnabled`、`TrackBase.isMuted` 未进入 `BuildProcesses` 播放过滤。 |
| P1-03 | 高 | 设计缺陷 | Pause 仅冻结 Runner，不对运行中音频/运行时动画形成统一暂停闭环。 |
| P1-04 | 高 | 功能闭环缺失 | 反向播放未闭环：结束条件、循环重置、子系统速度语义均不完整。 |
| P2-01 | 中 | 行为不一致 | 预览速度倍率在 `Variable` 模式未生效。 |
| P2-02 | 中 | 字段失效 | `SkillAnimationClip.positionOffset/rotationOffset/useMatchOffset` 写入但播放链路不消费。 |
| P2-03 | 中 | 代码卫生 | `previewSeekDirty` 只写不读；`prevoewTarget` 拼写错误暴露到外部调用。 |
| P2-04 | 中 | 接入缺口 | 运行时缺少统一 Runner 托管范式（`SkillLifecycleManager` 与手动 Tick 并存）。 |

---

## 4. 重点问题与错误链路

## P0-01 双时钟驱动（编辑器）

定位：

- `Editor/SkillEditorWindow.cs:225-253`（手动推进 `state.timeIndicator`）
- `Editor/SkillEditorWindow.cs:259`（随后调用 `UpdatePreview()`）
- `Editor/Playback/SkillEditorWindow.Preview.cs:375`（再次用 `previewRunner.CurrentTime` 回写）

问题链路：

1. `Update()` 先按 `lastFrameTime` 增量推进 `state.timeIndicator`
2. 同一帧 `UpdatePreview()` 再按 Runner 结果回写
3. 两套结束判断并存（`Update()` 与 `UpdatePreview()` 各有结束逻辑）

影响：

- 时间指示器与真实 Runner 时间可能短暂分叉
- 在变速或固定步模式下，存在提前/延后触发停止的风险
- 后续定位“过渡时长异常”时会被状态噪声干扰

---

## P0-02 Tick/Seek 区间边界不一致

定位：

- `Runtime/Playback/Core/SkillRunner.cs:186-187`（Seek：`target >= start && target < end`）
- `Runtime/Playback/Core/SkillRunner.cs:245-246`（Tick：`time > start && time <= end`）

问题链路：

- Tick 正放进入片段时会“晚一帧”触发 `OnEnter`（因为 `>`）
- Seek 到同一时间点却会“立即激活”片段（因为 `>=`）
- 两者在边界帧行为不一致，导致“同一时刻，播放与单帧预览表现不一致”

影响：

- 过渡区有效时长缩短（常见 1 帧偏差，30fps 下约 33ms）
- 进入/退出事件在 Tick 与 Seek 间无法对齐

---

## P0-03 Seek 的 deltaTime 语义错误

定位：

- `Editor/Core/SkillEditorState.cs:125-134`（`SnapInterval` 变量模式返回 `-1`）
- `Editor/Playback/SkillEditorWindow.Preview.cs` 多处 `SeekWithPreviewTrackBase(..., state.SnapInterval)`（如 `266/285/298/312/328`）
- `Runtime/Playback/Core/SkillRunner.cs:219,223`（`deltaTime` 传入 `OnUpdate` 与 TickAction）
- `Editor/Playback/Processes/EditorAnimationProcess.cs:28-31`（TickAction 用 `deltaTime` 做 `ManualUpdate`）

问题链路：

1. 变量步长模式 Seek 传入 `-1`
2. `EditorAnimationProcess` 将其作为 `ManualUpdate(-1)` 使用
3. 底层混合进度按负增量更新，权重/过渡出现反向或突变

影响：

- 单帧预览、Seek 进入过渡区时的权重异常
- 回退帧行为不可逆（“动作回退但权重不回退/反向错乱”）

---

## P0-04 同轨片段执行顺序不稳定（未按时间排序）

定位：

- `SkillRunner.BuildProcesses()` 直接遍历 `track.clips`：`Runtime/Playback/Core/SkillRunner.cs:333`
- 拖拽只改 `clip.startTime` 不重排列表：`Editor/Views/TimelineClipInteraction.cs:439`

问题链路：

- 当同轨多个片段重叠或同帧进入时，`OnEnter/OnUpdate` 顺序取决于列表顺序
- 列表顺序又取决于历史编辑动作，而非时间顺序

影响：

- 同一数据在不同编辑过程下可能产生不同播放结果
- 过渡目标“最后一次 Play 覆盖前一次 Play”，行为不确定

---

## P0-05 同资源多片段共享同一 AnimState

定位：

- `AnimLayer.GetState(AnimationClip)`：`MAnimSystem/AnimLayer.cs:351-371`
- `AnimLayer.Play(...)` 对同一目标状态直接返回：`MAnimSystem/AnimLayer.cs:251-257`
- `AnimComponent.Evaluate(...)` 也通过 `GetState(clip)`：`MAnimSystem/AnimComponent.cs:343`

问题链路：

- 片段身份是 `SkillAnimationClip`（时间段），但动画状态身份却是 `AnimationClip` 资源
- 同一资源在同层多个片段会争用一个状态：
  - 后片段可能无法触发独立重播/独立过渡
  - 不同片段 OnUpdate 会覆盖同一状态时间

影响：

- 出现“看起来像高频自切换/重入”的异常体感
- 同资源切段编排失真（尤其是重叠和循环）

---

## P1-01 `isMasterTrack` 不是数据层强约束

定位：

- `AnimationTrack.CanPlay => isMasterTrack`：`Runtime/Data/Tracks/AnimationTrack.cs:24`
- `BuildProcesses` 依赖 `track.CanPlay`：`Runtime/Playback/Core/SkillRunner.cs:331`
- 约束逻辑仅在 `AnimationTrackDrawer.DrawInspector` 内调用：`Editor/Drawers/Impl/AnimationTrackDrawer.cs:21-23,108+`

问题：

- 若 JSON 导入后未打开该轨道 Inspector，非法状态（全 false/多 true）不会自动修正

影响：

- 可能出现“有动画轨道但完全不播”或“主轨不确定”的问题

---

## P1-02 `Group.isEnabled` / `Track.isMuted` 与播放脱节

定位：

- 字段定义：`Runtime/Data/Group.cs:24`，`Runtime/Data/TrackBase.cs:27`
- 播放过滤仅检查 `track.isEnabled && track.CanPlay`：`Runtime/Playback/Core/SkillRunner.cs:331`

问题：

- UI 上可操作的开关并未进入播放判定

影响：

- 编辑器看到“禁用分组/静音轨道”，实际仍可播放（取决于 track.isEnabled）

---

## P1-03 Pause 语义不完整

定位：

- `SkillRunner.Pause` 仅改状态：`Runtime/Playback/Core/SkillRunner.cs:148-153`
- 运行时动画仍由 `AnimComponent.Update` 驱动：`MAnimSystem/AnimComponent.cs:76-84,99-112`
- 预览音频暂停依赖 `GlobalPlaySpeed==0`：`Editor/Playback/Processes/EditorAudioProcess.cs:87-90`

问题：

- Pause 没有统一下发到“持续自行更新”的子系统（音频、运行时动画）

影响：

- 时间轴暂停后，局部子系统可能继续推进

---

## P1-04 反向播放闭环缺失

定位：

- `CurrentTime += deltaTime * speed`：`SkillRunner.cs:239`
- 结束判定仅 `>= Duration`：`SkillRunner.cs:285`
- loop 重置固定回 `0`：`SkillRunner.cs:290`
- `RuntimeVFXProcess.SyncSpeed` 负速直接 return：`Runtime/Playback/Processes/RuntimeVFXProcess.cs:112`

问题：

- 反向播放没有“到 0 的终止/循环”语义
- 子系统对负速处理不统一

影响：

- 反向播时行为不可预测（无法保证 Enter/Exit/Loop 完整闭环）

---

## P2-01 预览速度倍率在 Variable 模式失效

定位：

- 固定步分支使用 `previewSpeedMultiplier`：`SkillEditorWindow.Preview.cs:351`
- 变量步分支未使用：`SkillEditorWindow.Preview.cs:368`

影响：

- 同一个速度设置在不同步进模式下行为不一致

---

## P2-02 动画 Offset 匹配字段未进入播放链路

定位：

- 字段定义：`SkillAnimationClip.positionOffset/rotationOffset/useMatchOffset`（`Runtime/Data/Clips/SkillAnimationClip.cs:24-30`）
- 匹配工具写入这些字段：`Editor/Utils/AnimationMatchUtility.cs:37-43`
- 动画 Process 播放/采样不消费上述字段：`EditorAnimationProcess.cs`、`RuntimeAnimationProcess.cs`

影响：

- “匹配上一个片段 offset”数据层有值，但播放层无效果
- 功能表现与用户认知不一致

---

## P2-03 代码卫生问题：死字段与拼写错误

定位：

- `previewSeekDirty` 仅写入未读取：`Editor/Core/SkillEditorState.cs:146` + 多处写入
- `prevoewTarget` 拼写错误并被外部使用：`Editor/Playback/SkillEditorWindow.Preview.cs:14`、`Editor/Drawers/Impl/AnimationClipDrawer.cs:61,70`

影响：

- 增加维护成本与误用风险

---

## P2-04 运行时 Runner 托管方式未统一

现状：

- 既有 `SkillLifecycleManager`（统一 Update Tick），也有外部手动 `runner.Tick(step)` 方式
- 当前仓库主使用场景以手动 Tick 测试脚本为主

影响：

- 接入层易出现“谁负责驱动、谁负责反注册”的职责不清

---

## 5. 字段使用审计结论

### 5.1 使用不得当或语义未落地

- `previewSeekDirty`：只写不读，建议移除或补齐消费链路。
- `SkillAnimationClip.useMatchOffset`：仅作为标记存储，无运行时/预览语义。
- `SkillAnimationClip.positionOffset/rotationOffset`：动画播放链路未使用。
- `Group.isEnabled`、`TrackBase.isMuted`：与 `BuildProcesses` 过滤条件脱节。

### 5.2 语义弱约束字段

- `AnimationTrack.isMasterTrack`：依赖 Inspector 绘制时矫正，不是数据模型层约束。

---

## 6. 风险优先级建议

建议按以下顺序处理：

1. 先修 P0-01 / P0-02 / P0-03 / P0-04 / P0-05（直接影响“过渡时长、权重、稳定性”）
2. 再修 P1-01 / P1-02 / P1-03 / P1-04（提升状态机闭环和一致性）
3. 最后清理 P2（字段/接口卫生、可维护性）

---

## 7. 结论

当前播放系统“主干可跑”，但在边界一致性、状态机闭环、字段语义落地上存在系统性风险。最关键的根因不是单点 bug，而是以下三类叠加：

- 时间与状态判定存在多源（双时钟 + 边界不一致）
- 片段身份与动画状态身份不一致（同资源片段共享状态）
- 编辑器预览参数语义复用不当（`SnapInterval` 兼作 `deltaTime`）

若不先统一这些基础语义，后续在“过渡时长准确性、Seek 可逆性、反向播放、偏移匹配”上的问题会反复出现。

