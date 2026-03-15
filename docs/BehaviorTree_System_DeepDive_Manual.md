# 行为树系统全栈实战技术手册 (精华详尽版)

> **前言**：本手册是针对本项目行为树（Behavior Tree, BT）架构的深度复盘与总结。它不仅涵盖了从零开始的基础建模，还深入探讨了运行时内核、业务绑定逻辑以及底层架构的设计哲学。

---

## 第一部分：核心数据模型 (Core Data Models) —— AI 的记忆与骨骼

行为树的本质是一棵**带有状态的决策树**。在本项目中，数据被严谨地划分为静态定义与动态容器。

### 1.1 节点模型 (Nodes)
所有节点均派生自 `BehaviorTreeNodeModelBase`。
-   **组合节点 (Composite)**: 逻辑的“流向控制中心”。
    -   *Sequence (顺序)*: 依次执行子节点，遇 False 则断。
    -   *Selector (选择)*: 依次尝试子节点，遇 True 则停。
-   **装饰/条件节点 (Condition)**: 逻辑的“过滤器”。基于黑板数据或常量进行数值比较。
-   **服务节点 (Service)**: 逻辑的“观察员”。不直接参与决策流切换，但会定时刷新黑板数据。
-   **动作节点 (Action)**: 逻辑的“执行者”。

### 1.2 黑板机制 (Blackboard)
黑板是 AI 的**短时记忆执行器**。
-   **解耦作用**: 节点之间互不通信，只读取和写入黑板。这使得节点可以被高度复用。
-   **类型安全**: `BehaviorTreeValueData` 确保了数据在编辑器与运行时之间拥有统一的类型约束（如 Vector3, Float, Bool 等）。

### 1.3 静态定义 (Definition)
编译后的 `BehaviorTreeDefinition` 是一份**只读的蓝图**。它采用扁平化的列表存储节点，消除了递归引用带来的内存开销，是运行时高效 Tick 的基础。

---

## 第二部分：编辑器与编译流程 (Editor & Compilation) —— 从视觉到逻辑

### 2.1 图资产 (GraphAsset)
`BehaviorTreeGraphAsset` 是编辑器的核心。它存储了节点的位置、连线关系以及黑板项的初始定义。

### 2.2 编译器 (Compiler)
编译器是一个“翻译官”，它的流程如下：
1.  **拓扑解析**: 遍历 Graph，理清父子关系。
2.  **数据映射**: 将视觉节点转换为运行态的 `DefinitionNode`。
3.  **类型同步**: 确保黑板键在不同节点间的索引一致性。

### 2.3 校验器 (Validator)
在编译前，`Validator` 会进行深度体检，防止以下错误：
-   孤立节点或多个 Root 情况。
-   动作节点（叶子）带了子节点。
-   引用了不存在的黑板键。

---

## 第三部分：运行时执行内核 (Runtime Execution) —— 每一帧的心跳

### 3.1 Tick 驱动机制
每个 AI 身上挂载的 `BehaviorTreeCharacterAgent` 会在 `Update` 里调用 `Tick()`。
-   **状态反馈机制**: 每一帧，Root 都会询问子节点当前状态（`Success`, `Failure`, `Running`）。
-   **Running 的魔力**: 如果动作返回 `Running`，下一帧 Tick 会直接“降落”到该节点继续执行，直到它给出明确结果。

### 3.2 深度拆解：抢占模式 (AbortMode) —— 极速响应的关键
这是本架构最精妙的地方。它允许高优先级分支**由于外部环境变化**强行打断低优先级任务。
-   **自中止 (Self)**: 当本分支的条件不再满足，节点主动退出。
-   **低优先级中止 (LowerPriority)**: 当高优先级条件（左侧）被满足，立即掐断右侧正在运行的动作。
-   **Both**: 以上两者结合。
-   **执行流程**: 每一帧，Selector 都会先重新审视左侧的条件节点配置，若发生变化则触发 `ResolveSelectorStartIndex` 逻辑进行跳转。

### 3.3 架构辨析：轮询 (Polling) vs 事件驱动 (Event-Driven)
-   **轮询式 (本项目)**: 
    -   *优点*: 极端稳定，AI 每一帧都基于最新世界信息决策，适合位置瞬变的动作游戏。
    -   *缺点*: 复杂度极高时消耗 CPU 较多。
-   **事件驱动式 (如 Unreal)**: 
    -   *优点*: 只有数据变时才触发，极致省电/省 CPU。
    -   *缺点*: 实现极其复杂，容易因为丢失事件导致 AI 表现“断片”。

---

## 第四部分：业务绑定与输入模拟 (Business Binding) —— 肌肉的收缩

### 4.1 傀儡模型：BT -> Input -> FSM
我们没有让 BT 直接去“播动画”，因为那会破坏角色规则。
1.  **BT (大脑)**: 产生意图（我想攻击）。
2.  **Facade (传递)**: 调动接口将意图传给信号层。
3.  **InputProvider (信号)**: 像玩家按键一样触发事件。
4.  **FSM (规则)**: 状态机通过 `InputHandler` 拦截非法操作（如被击飞时忽略攻击按键）。

### 4.2 角色代理 (Agent) 与 门面 (Facade)
-   **Agent**: 管理生命周期，负责初始化和注销元数据。
-   **Facade**: 行为树 Handler 操作角色的唯一接口，隐藏了底层的物理细节。

---

## 第五部分：实战演练案例 —— “追击+攻击”逻辑全链路

### 蓝图结构
-   `Root` -> `Service: SyncTarget`
    -   `Selector` (Abort: Both)
        -   `Sequence: 攻击` (Condition: Dist < 2) -> `Action: BasicAttack`
        -   `Sequence: 追击` (Condition: HasTarget) -> `Action: ChaseTarget`
        -   `Action: Idle`

### 执行过程（当玩家靠近时）：
1.  **检测**: `SyncTarget` 发现玩家，写入黑板位置。
2.  **判定**: `Selector` 发现“攻击”分支的条件满足了（距离 < 2）。
3.  **动作**: 执行 `TriggerCharacterCommandAction`。
4.  **翻译**: Facade 调用了 `AIInputProvider.TriggerBasicAttack()`。
5.  **拦截**: 状态机发现自己处于 `Idle` 态，允许攻击，切入 `SkillState`。
6.  **结果**: 玩家看到 AI 挥剑砍了过来！

---

## 第六部分：FAQ 与 最佳实践

-   **如何让 AI 反应更快？**
    -   减小 Service 的 `TickInterval` 或使用抢占模式。
-   **什么时候用黑板？**
    -   只要是跨节点共享的数据（目标、剩余血量、位置）都该进黑板。
-   **Handler 里的状态存储？**
    -   Handler 是全局唯一的单例！**千万不要**在 Handler 类里定义 `float timer` 之类的字段。请务必使用 `context.GetOrCreateNodeMemory<T>` 来确保存储在每个实例专属的内存块中。

---

## 结语
行为树是逻辑的载体，而状态机是物理的堡垒。通过 **BT -> Input -> FSM** 的链路，我们构建出了既有智慧又遵守规则的强大 AI 系统。

*版本: v1.1 (2026-03-15 精华总结版)*
