# Character Config Skill 设计方案

## 1. 目标

为角色配置资产提供一套可重复、可校验、可批量生成的工具链，覆盖：

- `CharacterConfigAsset` 自动创建
- `HitReactionConfig` 自动创建
- 一套 `ActionConfigAsset` / `SkillConfigAsset` / `LocomotionConfigAsset` 自动创建
- 本地 ID 注册表维护
- 基于模板的批量生成、补齐、重构

当前阶段明确不处理：

- 指令集资产创建
- 指令集内部路由细节配置
- Timeline 内容创建

指令集仍由人工维护，但本工具需要为后续挂接预留接口。

## 2. 现状约束

项目当前已有以下事实约束：

- 角色总配置类型为 `CharacterConfigAsset`
- 动作基类为 `ActionConfigAsset`
- 攻击/技能类主要使用 `SkillConfigAsset`
- 待机/移动类主要使用 `LocomotionConfigAsset`
- 受击配置为 `HitReactionConfig`
- 当前历史资产的 Action ID 并不完全遵守统一分段规则

因此新方案必须同时满足：

1. 新建资产使用统一新规则
2. 历史资产允许被扫描和登记
3. 不强制立即迁移历史资产
4. 模板不得要求用户手填每个 Action 的 ID

## 3. 核心原则

### 3.1 模板不配置具体 ID

模板文件只描述：

- 角色有哪些资产
- 每个资产的名称
- 每个资产的类型
- 每个资产的业务语义
- 哪些资产需要互相引用

模板不描述：

- 每个 Action 的具体 ID
- 每个角色的具体 Action 区间内偏移值

这些由 ID 注册表和分配器自动完成。

### 3.2 ID 分配统一由注册表驱动

用户只需要维护较少的全局规则：

- 角色 ID 范围
- 动作 ID 总范围
- 每个角色的动作 ID 占位长度
- 是否允许自定义角色动作起始段

正常使用中，用户不需要手动分配单个资产 ID。

### 3.3 生成与校验分离

所有生成操作都必须先经过：

1. 模板校验
2. ID 预分配
3. dry-run 预览
4. 真正写入

避免直接写盘后才发现冲突。

## 4. ID 规则

## 4.1 全局范围定义

第一版采用可配置规则：

- `RoleID` 范围：`1000-1999`
- `FirstActionMappedRoleId`：`1001`
- `ActionID` 范围：`10000-99999`
- 每个角色动作区间长度：`200`

示例：

- 安比 `RoleID = 1001`
- 安比动作区间：`10000-10199`
- 星见雅 `RoleID = 1002`
- 星见雅动作区间：`10200-10399`

默认计算公式：

- `roleOffset = RoleID - firstActionMappedRoleId`
- `actionRangeStart = actionIdRange.start + roleOffset * actionBlockSize`
- `actionRangeEnd = actionRangeStart + actionBlockSize - 1`

## 4.2 历史兼容策略

由于现有资产未完全遵守上面区间规则，扫描器需要兼容历史数据，但注册表不再保存每个历史 ID 的逐条明细。

第一版策略：

- 扫描器读取现有资产，实时校验重复和越界
- 注册表只保存各分配桶的“最大已分配值”
- 历史资产如果不符合新区间规则，允许存在，但不会被登记为逐条占用记录
- 新创建角色和新创建动作始终按新规则顺延分配

## 4.3 分配规则

本方案改为“按分配桶记录最大值，再按 `max + 1` 分配”。

分配桶分为两类：

- `RoleID` 分配桶
- 每个角色自己的 `ActionID` 分配桶

规则如下：

- 若某分配桶已有最大值，则新 ID = `maxAllocated + 1`
- 若某分配桶还没有任何已分配值，则新 ID = 该桶的起始值
- 分配成功后，立即回写该桶的最大值缓存

这样可以显著减少注册表数据量，不需要保存每个已分配 ID 的明细。

### 4.4 角色动作桶

虽然注册表不保存逐条 Action ID，但每个角色仍然保留自己的动作区间。

示例：

- 安比 `RoleID = 1001`
- 安比动作区间：`10000-10199`
- 若安比当前动作最大值缓存为 `10013`
- 那么安比下一个新动作 ID 为 `10014`

若某角色动作桶尚未分配过任何 ID，则直接取该角色动作区间起始值。

### 4.5 超限校验

由于每个角色动作区间长度固定为 `200`，分配时必须校验：

- 新 ID 不得超过该角色动作区间上限
- 超出上限则创建失败，并提示需要扩容规则或手工迁移

## 5. 注册表设计

注册表建议放在：

- `ProjectSettings/Codex/character_action_registry.json`

原因：

- 不属于运行时资源
- 不希望被 Unity 当作内容资产参与加载
- 适合作为编辑器工具的状态文件

### 5.1 注册表结构

```json
{
  "version": 1,
  "rules": {
    "roleIdRange": { "start": 1000, "end": 1999 },
    "actionIdRange": { "start": 10000, "end": 99999 },
    "actionBlockSize": 200
  },
  "allocators": {
    "roleId": {
      "maxAllocated": 1003
    }
  },
  "roleActionAllocators": [
    {
      "roleId": 1001,
      "roleName": "安比",
      "actionRange": { "start": 10000, "end": 10199 },
      "maxAllocated": 10013
    },
    {
      "roleId": 1002,
      "roleName": "星见雅",
      "actionRange": { "start": 10200, "end": 10399 },
      "maxAllocated": 10205
    }
  ]
}
```

### 5.2 注册表职责

- 保存全局 ID 规则
- 保存各分配桶的最大已分配值
- 支撑 dry-run 分配
- 支撑后续迁移和重构

### 5.3 注册表边界

注册表不再保存：

- 每个角色资产路径
- 每个 Action 资产路径
- 每个已分配 ID 的完整清单

这些信息在需要时由扫描器现查现算。

也就是说：

- 注册表负责“记住分配进度”
- 扫描器负责“校验当前真实资产状态”

## 6. 模板文件设计

模板文件建议放在：

- `Configs/CharacterTemplates/<角色名>.character-template.json`

模板只描述“需要什么”，不描述“最终 ID 是多少”。

### 6.1 模板结构

```json
{
  "version": 1,
  "character": {
    "roleName": "新角色",
    "prefabPath": "Assets/Prefabs/Characters/新角色.prefab",
    "actionRootKey": "基础_待机",
    "autoAllocateRoleId": true
  },
  "hitReaction": {
    "enabled": true,
    "lightKey": "受击_轻",
    "heavyKey": "受击_重",
    "knockAwayKey": null
  },
  "actions": [
    {
      "key": "基础_待机",
      "displayName": "基础_待机",
      "assetType": "LocomotionConfigAsset",
      "group": "base",
      "preload": true,
      "enterState": "Idle",
      "completeMode": "Default"
    },
    {
      "key": "普通攻击_1",
      "displayName": "普通攻击_1",
      "assetType": "SkillConfigAsset",
      "group": "normal_attack",
      "category": "LightAttack",
      "turnMode": "EnemyPriorityThenInput"
    }
  ]
}
```

### 6.2 模板中的关键字段

- `key`
  - 模板内唯一键
  - 用于引用、构建图关系
- `displayName`
  - 最终生成到文件名和 `m_Name`
- `assetType`
  - `SkillConfigAsset`
  - `LocomotionConfigAsset`
  - 后续可扩展更多 `ActionConfigAsset` 子类
- `group`
  - 用于业务分类、筛选和后续扩展
  - 不再直接参与 ID 分配
- `preload`
  - 是否加入 `ActionProLoadList`

### 6.3 模板派生能力

模板应支持通过少量配置快速表达常见角色差异：

- 普攻几段
- 是否有派生普攻
- 是否有特殊冲刺攻击
- 是否有闪前/闪后
- 是否有轻受击/重受击/击飞
- 是否有切入/切出

建议第二层抽象使用“模板参数 + 展开器”：

```json
{
  "params": {
    "normalAttackCount": 4,
    "hasDashAttack": true,
    "hasSpecialDashAttack": false,
    "hasEvadeForward": true,
    "hasEvadeBackward": true,
    "hasHeavyHit": true
  }
}
```

然后由展开器生成最终 `actions[]` 清单。

这样不同角色只改参数，不需要每次手写完整资产列表。

## 7. 自动分配器设计

## 7.1 RoleID 分配

当 `autoAllocateRoleId = true` 时：

1. 从注册表读取 `roleIdRange`
2. 读取 `allocators.roleId.maxAllocated`
3. 若存在最大值，则新 `RoleID = maxAllocated + 1`
4. 若不存在最大值，则新 `RoleID = roleIdRange.start`
5. 校验不能超过 `roleIdRange.end`
6. 若用户显式指定 `RoleID`，则校验后使用

## 7.2 ActionID 分配

当角色 `RoleID` 确定后：

1. 根据全局规则计算该角色理论动作区间
2. 读取该角色的动作桶最大值 `maxAllocated`
3. 若存在最大值，则新 `ActionID = maxAllocated + 1`
4. 若不存在最大值，则新 `ActionID = actionRange.start`
5. 每创建一个 Action，都在当前批次内向后递增
6. 若超过 `actionRange.end`，则报错终止

### 7.3 批量创建时的顺延规则

如果一次模板创建多个 Action，则按模板展开后的顺序依次分配：

- 第 1 个 Action 使用当前桶的下一个可分配值
- 第 2 个 Action 在第 1 个基础上继续 `+1`
- 以此类推

创建成功后，再把该角色动作桶的 `maxAllocated` 一次性更新为本次创建后的最终最大值

## 8. 生成流程

### 8.1 扫描

- 扫描已有角色资产
- 扫描已有 Action 资产
- 扫描已有受击配置
- 计算当前各分配桶最大值
- 更新注册表

扫描阶段仍然需要做即时校验：

- 是否存在重复 `RoleID`
- 是否存在重复 `ActionID`
- 是否存在越界 ID
- 是否存在角色与动作区间不匹配

### 8.2 校验模板

校验项包括：

- 模板结构是否合法
- `key` 是否唯一
- `actionRootKey` 是否存在
- `hitReaction` 引用是否存在
- `assetType` 是否受支持
- `group` 是否受支持
- 是否存在循环引用风险

### 8.3 dry-run

输出预览信息：

- 将分配的 `RoleID`
- 将分配的每个 `ActionID`
- 将更新到的最大值缓存
- 将创建的目录
- 将创建的资产路径
- 将写入的引用关系

### 8.4 真正创建

按顺序执行：

1. 创建角色目录
2. 创建各 Action 资产
3. 创建 `HitReactionConfig`
4. 创建 `CharacterConfigAsset`
5. 绑定交叉引用
6. 刷新注册表
7. 输出结果报告

## 9. 工具接口设计

第一版建议实现以下工具。

### 9.1 `character-config-scan`

职责：

- 扫描现有角色/Action/受击资产
- 重建或刷新注册表

输入：

- `fullRescan: bool`

输出：

- 扫描到的角色数
- 扫描到的动作数
- 各分配桶最大值
- 冲突列表

### 9.2 `character-config-validate-template`

职责：

- 校验模板结构和逻辑

输入：

- `templatePath`

输出：

- 是否通过
- 错误列表
- 警告列表

### 9.3 `character-config-dry-run-create`

职责：

- 只做预分配和创建预览

输入：

- `templatePath`
- `roleNameOverride`
- `prefabPathOverride`

输出：

- 预分配 `RoleID`
- 预分配 `ActionID` 列表
- 将写回的最大值变更
- 预创建路径列表

### 9.4 `character-config-create-from-template`

职责：

- 真正创建角色配置和动作配置

输入：

- `templatePath`
- `roleNameOverride`
- `prefabPathOverride`
- `apply: bool`

输出：

- 创建结果
- 资产路径
- 分配的全部 ID

### 9.5 `character-config-list`

职责：

- 列出已登记角色
- 查看某角色名下动作配置

### 9.6 `character-config-refactor`

职责：

- 基于新模板补齐或重构已有角色

第一版可以只支持：

- 新增缺失动作
- 标记多余动作

先不自动删资产。

## 10. 第一版范围

建议 v1 只做以下内容：

- 注册表扫描与重建
- 全局规则配置
- 无 ID 模板格式
- dry-run
- 创建 `CharacterConfigAsset`
- 创建 `HitReactionConfig`
- 创建 `SkillConfigAsset` / `LocomotionConfigAsset`
- 自动连 `ActionRoot`
- 自动连 `ActionProLoadList`
- 自动连 `hitReactionConfig`

v1 不做：

- 指令集创建
- Route 自动配置
- Timeline 自动生成
- 复杂迁移
- 自动删除旧资产

## 11. 实现落点建议

建议分成两层。

### 11.1 Codex Skill 层

建议放在：

- `.agents/skills/character-config-codex/`

包含：

- `SKILL.md`
- `references/`
- `scripts/`

这一层负责：

- 触发条件
- 工作流约束
- 低 token 操作策略
- Agent 调用顺序

### 11.2 Skill 脚本层

建议放在：

- `.agents/skills/character-config-codex/scripts/`

包含：

- 注册表读写
- 模板解析
- ID 分配器
- 扫描器
- 轻量缓存更新脚本

这样角色配置操作仍然由 Codex Agent 驱动，但把易出错的注册表和分配逻辑收敛到本地脚本里，减少重复检索和 token 消耗。

## 12. 下一步实施建议

建议下一步按以下顺序推进：

1. 先敲定模板 schema
2. 再敲定注册表 JSON schema
3. 然后实现扫描器和 ID 分配器
4. 再实现 dry-run
5. 最后实现真正创建资产

如果继续实现，第一批最值得先做的是：

- 注册表数据结构
- 扫描器
- 模板解析器
- ID 分配器

因为这四块一旦稳定，后面的创建器和重构器就会顺很多。
