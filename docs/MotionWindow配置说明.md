# MotionWindow 配置说明

## 目的

`MotionWindow` 用来描述“某一段技能时间内，如何解释动画根运动”。

它不直接生成位移，而是接管当前动画这一帧的 `root motion`，再根据窗口配置决定：

- 以前向为主还是完整保留原始轨迹
- 是否保留左右摆动
- 撞到角色时停下还是穿过去
- 撞到场景时阻挡还是沿墙滑动
- 窗口结束时是否修正到目标前方或后方

它适合这些技能：

- 直线突进但不能穿敌
- 折线突进并保留左右摆斩
- 直线穿敌并落到目标背后
- 折线穿敌但仍然不能穿墙
- 起手锁目标、过程中持续跟踪目标的技能

## 使用方式

1. 在技能时间轴上添加一条 `MotionWindowTrack`
2. 在需要接管根运动的时间段放置一个或多个 `MotionWindowClip`
3. 按技能需求配置窗口参数
4. 技能播放时，`MovementController.OnAnimatorMove` 会优先读取当前生效窗口来解释根运动

同一个技能可以放多个窗口。后进入的窗口会覆盖先前窗口。

## 参数说明

### `targetMode`

- `None`
  不获取目标。适合纯表演位移或只按角色当前朝向运动。
- `AcquireAtEnter`
  进窗口时抓一次目标，后续不更新。最常用。
- `RequireTarget`
  进入窗口时必须有目标，否则该窗口不生效。
- `ContinuousTrack`
  窗口期间持续更新目标引用。适合会换目标或目标可能横移很大的技能。

### `referenceMode`

- `CharacterForwardAtEnter`
  以前一刻角色朝向作为参考前向。
- `InputDirectionAtEnter`
  以进窗口时的输入方向作为参考前向。
- `TargetLineAtEnter`
  以进窗口时“角色指向目标”的方向作为参考前向。
- `TargetLineContinuous`
  持续用“角色指向目标”的方向更新参考前向。

### `trajectoryMode`

- `Authored`
  完整保留动画作者写出来的水平轨迹。适合表演性强、轨迹本身就是设计内容的技能。
- `ForwardOnly`
  只保留沿参考前向的推进量，左右位移全部滤掉。适合直线突进。
- `ForwardKeepLateral`
  前向推进按参考方向解释，左右位移保留。适合折线斩击、左右往返突进。

### `characterCollisionMode`

- `BlockAll`
  撞到角色就停，常用于“贴近敌人但不穿过去”的技能。
- `IgnorePrimaryTarget`
  忽略当前主目标，但仍会被其他角色阻挡。常用于“穿过目标但不穿杂兵”的技能。
- `IgnoreAllCharacters`
  忽略所有角色碰撞。常用于纯位移演出或无体积穿越。

### `worldCollisionMode`

- `Block`
  撞到场景就停。
- `Slide`
  撞到场景后沿场景切线滑动。适合贴墙滑行类技能。
- `Ignore`
  不处理场景阻挡。一般只建议在非常明确的特殊技能里使用。

### `endPlacementMode`

- `None`
  窗口结束不做额外落点修正。
- `StayCurrent`
  保持当前落点，不额外修正。
- `SnapFrontOfTarget`
  结束时尝试修正到目标前方。
- `SnapBehindTarget`
  结束时尝试修正到目标后方。

### `forwardScale`

前向位移缩放。  
大于 `1` 会放大前冲距离，小于 `1` 会缩短前冲距离。

### `lateralScale`

横向位移缩放。  
只在 `ForwardKeepLateral` 下生效。

### `stopDistance`

与目标或阻挡体保持的最小停止距离。  
常用于“停在敌人面前，不贴脸嵌入”的情况。

### `passThroughOffset`

穿过目标后，结束修正时在目标背后保留的距离。  
只在 `SnapBehindTarget` 这类场景最常用。

### `continuousTurnRate`

持续跟踪时的转向速度，单位是度每秒。  
只在 `referenceMode = TargetLineContinuous` 下生效。

### `characterBlockLayers`

角色阻挡检测层。  
为空时会走默认角色层：

- `CharHit`
- `Charcter`
- `Character`

### `worldBlockLayers`

场景阻挡检测层。  
为空时会走默认场景层：

- `Default`
- `Ground`

### `debugColor`

仅用于编辑器和调试可视化标识。

### `enableConstraintBox`

启用位移约束盒。  
开启后，角色碰撞体在窗口期间不能离开这个盒子的范围。

### `showConstraintBoxInScene`

是否在时间轴编辑器的 Scene 视图里显示并编辑约束盒。  
只影响编辑器显示，不影响运行时。

### `alignConstraintBoxFrontToTarget`

运行时是否把约束盒前边界贴到目标碰撞体边界。  
开启后，适合“折线突进但不能绕过敌人”的技能。  
编辑器预览因为没有敌人，会自动退化为按本地盒范围模拟。  

补充说明：
- 它只决定“前边界贴不贴目标碰撞体”
- 约束盒整体朝向仍然由当前窗口的 `referenceMode` 和 `ReferenceForward` 决定
- 如果希望盒子方向也持续跟随目标变化，一般要配合 `referenceMode = TargetLineContinuous`

### `constraintBoxMode`

约束盒的工作模式。

- `Block`
  - 纯阻挡盒
  - 不会把角色强行吸进盒子
  - 角色已经在盒内时，会阻止继续越界
  - 角色当前在盒外时，如果这一帧位移路径会穿过盒边界，也会在边界处被截住
  - 适合普通贴近技、折线突进技、只想“限制范围”而不想“自动贴边”的技能

- `SnapToInside`
  - 吸附盒
  - 会把当前计算出的候选落点直接夹回盒内
  - 如果角色起手就在盒外，可能出现明显的“被吸到盒边”效果
  - 适合明确需要快速收口到某个活动区间、或故意做吸附贴边效果的技能

推荐：
- 大部分“阻挡盒”需求优先用 `Block`
- 只有明确需要“吸附进盒内”的技能，再用 `SnapToInside`

### `constraintBoxSize`

约束盒尺寸，使用本地坐标：

- `X`：左右范围
- `Y`：高度
- `Z`：前后范围

当前主要使用 `X` 和 `Z`。

### `constraintBoxCenterOffset`

约束盒相对锚点的本地偏移。  
运行时如果开启了 `alignConstraintBoxFrontToTarget`，锚点会先按目标碰撞体前边界计算，再叠加这个偏移。  
编辑器预览没有敌人时，会退化为“角色从约束盒后侧附近起步”的模拟方式，方便直接观察前进与横移限制效果。

### `autoFitConstraintBoxDepthToTarget`

是否在运行时根据“当前角色胶囊中心到目标碰撞体前边界的距离”动态重算约束盒的 `Z` 深度。  

- 开启时：
  - 约束盒前边界仍然贴目标碰撞体前边界
  - `Z` 深度会随敌我距离实时变化
  - `X / Y` 仍然完全读取配置，不会自动改
- 关闭时：
  - 约束盒仍会贴目标前边界
  - 但 `Z` 深度固定使用 `constraintBoxSize.z`

推荐：
- 直线贴近、折线贴近、受击时目标会后退的技能，一般建议开启
- 明确需要固定活动深度的表演型技能，可以关闭

### `constraintBoxBackPadding`

当 `autoFitConstraintBoxDepthToTarget = true` 时，约束盒后侧额外增加的留量。  

它的作用是：
- 前边界继续贴目标碰撞体
- 后边界在“刚好包住当前敌我距离”的基础上，再额外向后放宽一段距离

常见用途：
- 技能前几帧先有轻微后撤，再前冲
- 想保留更多折线摆动空间，但又不想让角色绕过目标
- 受击目标被击退时，给角色保留一点跟进余量

如果这个值过小：
- 角色可能在技能起手就显得过于“绷紧”

如果这个值过大：
- 折线技能虽然还不会越过目标前边界，但活动范围会明显放宽

### `projectEndPositionToReferenceLine`

窗口结束时，是否把最终落点重新投影回“角色起手到目标方向”的参考直线。  

适合：
- 折线突进
- Z 字突进
- 过程里保留横向摆动，但收招时希望角色落回攻击中线

当前行为：
- 过程中的横向位移仍然保留
- 只在窗口结束时修正最终落点
- 如果同时启用了约束盒，最终落点还会再被收进约束盒范围内

## 配置示例

## 1. 直线突进，停在敌人面前

适用场景：

- 普攻首段贴近
- 不能穿敌的直线冲刺

推荐配置：

```text
targetMode = AcquireAtEnter
referenceMode = TargetLineAtEnter
trajectoryMode = ForwardOnly
characterCollisionMode = BlockAll
worldCollisionMode = Block
endPlacementMode = StayCurrent
forwardScale = 1
lateralScale = 0
stopDistance = 0.15 ~ 0.4
passThroughOffset = 0
```

效果：

- 前冲会朝目标方向解释
- 左右位移会被滤掉
- 撞到敌人停在前方，不会穿过去

## 2. 折线突进，保留左右摆斩，不穿敌

适用场景：

- 往前冲的同时左右往返斩击
- 动画横向位移是表演的一部分

推荐配置：

```text
targetMode = AcquireAtEnter
referenceMode = TargetLineAtEnter
trajectoryMode = ForwardKeepLateral
characterCollisionMode = BlockAll
worldCollisionMode = Block
endPlacementMode = StayCurrent
forwardScale = 1
lateralScale = 1
stopDistance = 0.15 ~ 0.3
```

效果：

- 前向推进仍对齐目标
- 左右摆动保留
- 正面贴近时不会继续把角色往目标体内推

## 3. 直线突进，穿过目标

适用场景：

- 突刺穿体
- 冲过目标后停在背后

推荐配置：

```text
targetMode = AcquireAtEnter
referenceMode = TargetLineAtEnter
trajectoryMode = ForwardOnly
characterCollisionMode = IgnorePrimaryTarget
worldCollisionMode = Block
endPlacementMode = SnapBehindTarget
forwardScale = 1
stopDistance = 0
passThroughOffset = 0.8 ~ 1.5
```

效果：

- 可以穿过主目标
- 仍然会被场景阻挡
- 结束时会尝试修正到目标背后

## 4. 折线突进，穿过目标

适用场景：

- 横向往返斩击并穿过目标
- 动作期间需要保留左右摆动

推荐配置：

```text
targetMode = AcquireAtEnter
referenceMode = TargetLineAtEnter
trajectoryMode = ForwardKeepLateral
characterCollisionMode = IgnorePrimaryTarget
worldCollisionMode = Block
endPlacementMode = SnapBehindTarget
forwardScale = 1
lateralScale = 1
passThroughOffset = 1.0 ~ 1.5
```

效果：

- 穿目标
- 保留折线路径
- 仍然不会穿墙

## 5. 完整保留动画轨迹的表演技

适用场景：

- 轨迹就是动画设计本体
- 不希望系统强行把动作拉直

推荐配置：

```text
targetMode = None
referenceMode = CharacterForwardAtEnter
trajectoryMode = Authored
characterCollisionMode = IgnoreAllCharacters
worldCollisionMode = Block 或 Slide
endPlacementMode = None
```

效果：

- 尽量按动画原始水平轨迹播放
- 只在需要时处理场景阻挡

## 7. 折线突进，但不能绕过敌人

适用场景：

- Z 字突进
- 左右往返斩击
- 需要保留横移表现，但不允许角色绕过目标

推荐配置：

```text
targetMode = AcquireAtEnter
referenceMode = TargetLineAtEnter
trajectoryMode = ForwardKeepLateral
characterCollisionMode = BlockAll
worldCollisionMode = Block
endPlacementMode = StayCurrent

enableConstraintBox = true
alignConstraintBoxFrontToTarget = true
constraintBoxMode = Block
constraintBoxSize = (3.0, 2.0, 3.5)
constraintBoxCenterOffset = (0.0, 0.0, 0.0)
autoFitConstraintBoxDepthToTarget = true
constraintBoxBackPadding = 0.3 ~ 1.0
projectEndPositionToReferenceLine = true
```

效果：

- 横向摆斩仍然保留
- 角色碰撞体不能越过盒子前边界
- 目标较近时也不会因为横移绕到敌人身后

## 约束盒与其他参数的职责边界

为了避免调参时互相打架，可以把这几组参数理解成不同层级：

- `trajectoryMode`
  - 决定“这一帧动画根运动怎么解释”
  - 例如是只保留前向，还是前向保留同时保留横向摆动

- `characterCollisionMode`
  - 决定“角色碰撞是否会挡住这段位移”
  - 例如停在敌人前面、忽略主目标、忽略全部角色

- `worldCollisionMode`
  - 决定“场景碰撞如何处理”
  - 例如停下、滑墙、忽略世界

- `ConstraintBox`
  - 决定“即使前面的位移解释和碰撞都通过了，角色最终还能活动到多大范围”
  - 它是最终活动区域约束，不负责决定朝哪走，也不负责决定能不能穿目标

可以把它们理解成：

1. 先由 `trajectoryMode` 解释动画位移  
2. 再由 `characterCollisionMode / worldCollisionMode` 过滤碰撞  
3. 最后由 `ConstraintBox` 收口最终可活动区域

所以它们不是简单重复关系，而是前后串联关系。

## 哪些组合容易重叠或冲突

### `ForwardOnly` 和 `ConstraintBox`

这两个不是冲突，而是强弱不同：

- `ForwardOnly` 已经会把横向位移清掉
- 再开 `ConstraintBox` 主要是进一步限制前后活动边界

适合：
- 想要“绝对不能偏出去”的直线贴近技

不一定需要：
- 普通的直线停前技能，如果现在表现已经稳定，可以先不开约束盒

如果开了约束盒：
- 一般优先 `constraintBoxMode = Block`
- 不建议默认用 `SnapToInside`，否则远距离目标时可能出现“直接吸到盒边”的观感

### `ForwardKeepLateral` 和 `ConstraintBox`

这是最推荐搭配的一组：

- `ForwardKeepLateral` 负责保留左右摆斩
- `ConstraintBox` 负责不让角色因为横移绕过目标

也就是说：
- 前者保表演
- 后者收边界

这类组合几乎都建议：
- `constraintBoxMode = Block`

因为这类技能的目标通常是“保留折线表现，但不允许绕过去”，不是“把角色强吸到盒里”

### `IgnorePrimaryTarget` 和 `alignConstraintBoxFrontToTarget`

这组要看技能意图：

- 如果技能目标是“穿过敌人”
  - 一般不要再开“前边界贴目标且不允许越界”的约束盒
  - 否则语义上会互相对冲

更合适的做法：
- 穿敌段关闭约束盒
- 或者改成另一个窗口，在穿敌完成后再启用新的约束盒

### `SnapBehindTarget` 和 `ConstraintBox`

这组也不是绝对冲突，但通常不建议放在同一个窗口里同时强依赖：

- `ConstraintBox` 更适合限制“窗口过程中”的活动范围
- `SnapBehindTarget` 更适合处理“窗口结束时”的收尾落点

推荐拆法：
- 突进过程窗口：负责 root motion 解释和过程限制
- 收尾窗口或退出修正：负责最终停在目标前/后

### `alignConstraintBoxFrontToTarget = true` 但当前没有目标

运行时当前实现是：

- 不启用约束盒
- 直接退回该窗口原本的位移解释逻辑

这是为了避免：
- 没目标时角色被一个本地盒意外限制住

编辑器预览则不同：

- 因为没有真实敌人
- 仍会用本地模拟盒预览效果

所以“编辑器预览有盒、运行时无目标时不启用盒”是刻意设计，不是异常。

## 6. 持续追踪目标的位移技

适用场景：

- 目标横移明显
- 希望技能过程中逐步修正追击方向

推荐配置：

```text
targetMode = ContinuousTrack
referenceMode = TargetLineContinuous
trajectoryMode = ForwardOnly 或 ForwardKeepLateral
characterCollisionMode = BlockAll 或 IgnorePrimaryTarget
worldCollisionMode = Block
continuousTurnRate = 90 ~ 360
```

效果：

- 窗口期间会持续刷新目标和参考前向
- 转向不是瞬间跳变，而是按 `continuousTurnRate` 平滑追踪

## 分段技能的推荐拆法

如果一个技能包含多个阶段，不要强行用一个窗口塞完，建议拆成多个 `MotionWindowClip`：

- 第一段：贴近目标
- 第二段：左右摆斩
- 第三段：穿目标
- 第四段：落点修正

常见拆分示例：

```text
0.00 - 0.18  ForwardOnly + BlockAll
0.18 - 0.42  ForwardKeepLateral + BlockAll
0.42 - 0.62  ForwardOnly + IgnorePrimaryTarget
0.62 - 0.70  SnapBehindTarget
```

## 当前实现边界

当前版本已经支持：

- `Authored`
- `ForwardOnly`
- `ForwardKeepLateral`
- `Block`
- `Slide`
- `Ignore`
- `BlockAll`
- `IgnorePrimaryTarget`
- `IgnoreAllCharacters`
- `SnapFrontOfTarget`
- `SnapBehindTarget`
- 运行时位移约束盒
- 编辑器 Scene 中的约束盒可视化与拖拽

当前仍建议注意：

- `Slide` 更适合场景边界，不建议拿它解决角色碰撞问题
- `ForwardKeepLateral` 的重点是“保留横向表演”，如果横向幅度极大，仍然建议拆成多个窗口细调
- 如果折线位移容易绕过目标，优先开启约束盒，而不是继续单纯调小 `lateralScale`
- 窗口只接管水平根运动解释；垂直位移是否完全交给动画，要结合具体技能再看

## 调参建议

- 先把 `trajectoryMode` 定对，再调 `stopDistance`
- 直线技能优先从 `ForwardOnly` 开始
- 折线技能优先从 `ForwardKeepLateral` 开始
- 穿敌技能先确认 `IgnorePrimaryTarget` 是否正确识别了主目标
- 如果结束落点有轻微偏差，优先调 `passThroughOffset` 和 `stopDistance`
- 一个窗口调不顺时，不要继续堆参数，优先拆成两个窗口
- 需要保留横移表现但又不能绕目标时，优先尝试 `ForwardKeepLateral + ConstraintBox`
