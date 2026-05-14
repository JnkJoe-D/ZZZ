# AGENT.md

## 目标
本文件用于统一 AI/工程协作规范，默认用于 `d:/Unity/Server_Game` 全项目。

## 项目现状快照（2026-02-26）
- 引擎版本：Unity `2022.3.44f1c1`
- 核心业务目录：`Assets/GameClient`
- 已具备主链路：`GameRoot` 启动、`ResourceManager`(YooAsset) 热更、`NetworkManager`(TCP/UDP+重连+心跳)、`UIManager`(MVC)、`ConfigManager`(Luban)、`SceneManager`
- 技能系统：`Assets/GameClient/SkillEditor`（Runtime + Editor 分层，Process/Track/Clip 可扩展）
- 当前重点开发区：输入指令、Locomotion 状态机（Ground/Air）、角色状态链路、全局相机管理

## 目录边界与职责
- `Assets/GameClient/Framework`：全局生命周期与事件总线
- `Assets/GameClient/Input`：输入抽象层与本地输入提供器
- `Assets/GameClient/FSM`：通用状态机框架
- `Assets/GameClient/Logic/Player`：玩家实体、状态与控制器
- `Assets/GameClient/Camera`：全局相机管理
- `Assets/GameClient/UI`：MVC UI 框架与业务模块
- `Assets/GameClient/Network`：协议、通道、分发、重连、心跳
- `Assets/GameClient/SkillEditor`：技能时间轴数据、运行时与编辑器预览
- `Assets/Editor`：通用编辑器工具

## 当前架构共识（必须遵守）
1. **输入与战斗控制采用混合分层**：
   - 底层：Locomotion FSM（Ground/Air）
   - 上层：Ability/Skill 路由（必要时暂停 FSM，技能结束后恢复）
2. **FSM 不承载技能状态爆炸**：技能调度走能力路由与 SkillEditor 运行时。
3. **输入层依赖 `IInputProvider` 抽象**，避免业务直接绑定具体输入实现。
4. **移动/动画/技能解耦**：`IMovementController`、`IAnimController`、Skill Process 接口分层协作。

## 代码风格与语言规范
1. 注释使用中文；类/方法/变量命名使用英文并符合 C# 规范。
2. C# 大括号采用 Allman 风格，缩进 4 空格。
3. MonoBehaviour 方法顺序：`Awake -> OnEnable -> Start -> Update -> FixedUpdate -> LateUpdate -> OnDisable -> OnDestroy`。
4. 优先私有字段 + `[SerializeField]`，避免直接暴露公有字段。
5. 避免在 `Update` 中频繁 `GameObject.Find`、循环内频繁 `GetComponent`、高频字符串拼接。

## 开发流程约定
1. 小改动（<10行）：可直接修改并说明。
2. 中改动（10-50行）：先给修改计划，再实施。
3. 大改动（>50行）：先给实现计划（建议落文档），获确认后实施。
4. 默认流程：定位 -> 修改 -> 最小验证 -> 汇报结果（改动点/原因/风险）。

## Karpathy 协作准则（已接入）
1. 本项目已添加本地 skill：`.agents/skills/karpathy-guidelines/SKILL.md`。
2. 处理中大型改动、重构、排查和评审时，默认同时遵循四条原则：先澄清假设、简单优先、手术式修改、目标驱动验证。
3. 这套准则用于约束 AI 协作行为，不替代本文件里的项目架构边界、代码风格和文件操作限制。

## Character Config Codex Skill（必须优先考虑）
1. 本项目已添加本地 skill：`.agents/skills/character-config-codex/SKILL.md`。
2. 当任务涉及以下任一内容时，优先使用该 skill，而不是临时全量检索所有配置文件：
   - `CharacterConfigAsset`
   - `ActionConfigAsset`
   - `HitReactionConfig`
   - 角色 Action 模板
   - 角色/动作 ID 分配
   - 注册缓存重建、读取、更新
3. 默认流程是：先读 `ProjectSettings/Codex/character_action_registry.json`，只有在缓存缺失、用户要求重建、或缓存明显失真时，才允许全量扫描 `Assets/Resources/Serializations/ScriptableObjects/Action/`。
4. 新建角色或新建动作配置时，模板中不得手填具体 ID，应由 skill 根据注册缓存按规则自动分配。
5. 删除或重命名操作默认保持缓存“单调递增”语义，不主动回收或下调最大 ID；只有用户明确要求精确重算时，才执行全量重建。

## 文件操作策略
- 允许优先修改：
  - `Assets/GameClient/**/*.cs`
  - `Assets/Editor/**/*.cs`
  - `*.md`
- 谨慎修改：
  - `Assets/Resources/**/*`
  - `ProjectSettings/**/*`
  - `Packages/manifest.json`
- 禁止修改（除非用户明确要求）：
  - 第三方插件代码目录（如 `Assets/XLua/**/*`、外部包源码）
  - `Library/**/*`
  - `*.csproj` / `*.sln`

## 当前已识别风险（接手优先处理）
1. `UI/Modules/Register/RegisterModule.cs`：邮箱刷新写入 `tag`，疑似应为 `text`。
2. `Adapters/SkillServiceFactory.cs`：`ISkillAudioHandler` 可能重复 `AddComponent`。
3. `Framework/GameRoot.cs`：初始化步骤编号/注释与实际顺序有偏差。
4. `Camera/GameCameraManager.cs`：`SetTarget`、`DoShake` 尚为 TODO，需补全实现。

## 质量门槛与检查清单
- [ ] 代码注释为中文
- [ ] 命名规范符合 C# 约定
- [ ] 无明显性能热点（Find/GetComponent/GC）
- [ ] 生命周期使用合理
- [ ] 网络相关逻辑考虑断线重连
- [ ] 新增逻辑与既有架构（Input/FSM/Ability）一致

## 文档与输出规范
1. 所有说明文档优先中文。
2. 需要可追踪时，按需维护：
   - `implementation_plan.md`（实现计划）
   - `task.md`（任务清单）
   - `walkthrough.md`（阶段总结）
3. 变更说明应明确：改了什么、为什么、如何验证、剩余风险。

#### 深度思考模式

1. 系统性分析：从整体到局部，全面分析项目结构、技术栈和业务逻辑
2. 前瞻性思维：考虑技术选型的长远影响，评估可扩展性和维护性
3. 风险评估：识别潜在的技术风险和性能瓶颈，提供预防性建议
4. 创新思维：在遵循最佳实践的基础上，提供创新性的解决方案

#### 授人以渔理念

1. 思路传授：不仅提供解决方案，更要解释解决问题的思路和方法
2. 知识迁移：帮助用户将所学知识应用到其他场景
3. 能力培养：培养用户的独立思考能力和问题解决能力
4. 经验分享：分享在实际项目中积累的经验和教训

#### 多方案对比分析

1. 方案对比：针对同一问题提供多种解决方案，并分析各自的优缺点
2. 适用场景：说明不同方案适用的具体场景和条件
3. 成本评估：分析不同方案的实施成本、维护成本和风险
4. 推荐建议：基于具体情况给出最优方案推荐和理由

#### 设计规范

遵循SRP，OCP，LSP，ISP，DIP，LoD，CARP，DRY八大设计原则。
