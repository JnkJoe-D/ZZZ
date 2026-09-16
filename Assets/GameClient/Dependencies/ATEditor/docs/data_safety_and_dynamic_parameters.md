# 技能数据安全与动态运行参数架构设计

> 记录于 2026-02-21：关于 SkillTimeline 内存共享与运行时参数动态修改的架构探讨。

## 1. 背景与核心矛盾

在《SkillEditor》框架的设计中，目前及未来必然面临一个核心需求：**技能的表现受限于动态战斗环境**。
例如，法师的“火球术”默认生成 1 颗火球，但当法师获得特定天赋（如“多重施法”）或 Buff 后，该技能需要动态生成 3 颗火球。 

在这个需求下，我们遇到了**编辑器序列化（Editor Serialization）**与**运行时数据流转（Runtime Data Flow）**的架构冲突：
- 为了能在 Inspector 面板自由拖拽与配置，`DamageClip`、`SpawnClip` 等组件的字段（如 `spawnCount`、`radius`）必须暴露可访问性（通常为 `public` 或 `[SerializeField]`）。
- 但是，在 `RuntimeSpawnProcess` 等执行阶段，如果直接依据业务逻辑修改 `clip.spawnCount = 3`，会引发极其严重的跨实体数据污染问题。

## 2. 内存共享模型（享元模式）与脏数据污染

导致“修改 Clip=系统级灾难”的根本原因，在于 Unity 以 ScriptableObject 为主体的资源加载机制：

当游戏运行并初始化多个角色（例如：多个地精都在释放同一个“重击”技能）时，为了节省内存开销，这些角色并不各自实例化一份 `SkillTimeline.asset`。
- `SkillTimeline` 和它内部包含的所有 `Track` 和 `Clip` 在内存中是**单例共享（Shared Instance）**的。
- 而不同的释放者在释放技能时，通过 `SkillRunner.Play()` 和 `ProcessFactory.Create()`，构建出了属于自己的**独立的 `Process` 执行域**。
- 但这些不同的 `Process` 所持有的 `Clip` 数据引用，均指向堆内存当中的**同一个资产物理地址**。

### 危险场景追溯
如果 `Process` 贪图方便，在 `OnEnter()` 内直接改写了 `clip.value = 动态新值`：
1. **跨角色污染 (Cross-Entity Pollution)**：法师A因天赋将 `clip.spawnCount` 改为了 3，此后毫无天赋的法师B释放火球术时，也会读取到被污染的 3，导致逻辑越权。
2. **生命周期泄露 (Lifecycle Leak)**：技能如果在释放中途被眩晕打断，原定于在 `OnExit()` 中写回默认值（=1）的重置逻辑未能执行，此后该技能将永久残留被放大的状态。

## 3. 标准化架构解法：混合器与参数下发 (Payload Delivery)

为了兼顾“可配性”与“并发安全”，技能系统应当坚守以下数据隔离原则：
**Clip 数据即为模板基准值（Base Configuration），在运行时应视为绝对只读。**

### 解决方案：抽象参数结构 (Parameter Payload) 与外部接管

我们采用将 `Process` 降维成**“时序分发器”**，并将真正生成与判定的控制权移交给系统外部（Handler）。

在 `RuntimeSpawnProcess` 中，我们不再直接把整个 `SpawnClip` 交出去，而是只向下传递原始的、纯净的配置参数。Handler 在接到参数的瞬间，自己去拉取战斗系统的动态数据进行叠加换算。

例如在生成阶段的设计规范：
```csharp
// 【绝对禁止的做法】
// Process 中：
clip.count += buffSys.GetBonus("Fireball_Count"); 
handler.Spawn(clip); // 传递被污染的 clip

// 【规范推荐的做法】
// Process 中：
// 仅作为触发扳机，只传递 Base Value （如通过接口参数下发）
spawnedProjectile = spawnHandler.SpawnObject(
    clip.prefab, 
    pos, 
    rot, 
    clip.eventTag, 
    clip.detach,
    clip.detach ? null : parent
);

// 外部 Handler 或 Projectile 在 Initialize/SpawnObject 内部：
// 自行拉取动态数值，与传过来的 Base Value 结合，决定实际威力或弹片数量。
```

## 4. 总结

1. **Clip 字段可访问性**：保持 `public` 是合理且必要的，主要为了迎合 Editor Inspector 面板反射绘制序列化及未来与其他美术工具流的对接。
2. **读写权限隔离**：在开发准则上，必须约束所有 `IProcess` 的实现类：**严禁在 Runtime 下对自身持有的 `Clip` 属性赋值**。
3. **动态结合模式**：通过 `Context.GetService<T>()` 获取外部系统，将 Clip 的基础值（Base Value）推(Push)给对应的 Handler 进行混合运算，或者 Handler 反向抓取(Pull)环境 Buff 数据进行动态修正，最终保证数据的绝对无状态化和高并发访问安全。
