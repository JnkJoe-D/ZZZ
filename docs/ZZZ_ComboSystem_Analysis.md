# 《绝区零》(ZZZ) 连招派生与多段窗口机制分析报告

## 1. 现状痛点与《绝区零》动作逻辑拆解

**绝区零（ZZZ）安比普攻3的逻辑流拆解：**
从安比的普攻第三段派生（普攻4 vs 落雷）可以看出，高水准的 ACT 游戏并未将一条动作的时间轴仅仅粗暴地切割为“前摇生效期”和“后摇死区”。其实际的时间轴至少存在 4 个性质完全不同的操作阶段：

1. **段落1：前摇与打击生效期（Startup & Hit）**
   - **机制**：角色正在挥刀。
   - **输入响应**：忽视当前指令的立即执行，而是将指令转入“缓冲死区（Buffered Input）”，等待后续窗口开启时自动结算。
2. **段落2：标准连段窗口（Normal Combo Window）**
   - **机制**：刀刃刚刚挥完，惯性开始显现。这是游戏中最标准的“完美连击点”。
   - **输入响应**：若此时存在之前缓冲的轻击指令，或者玩家刚好在这个瞬间按下了轻击，系统将立即跳转至标准派生 —— **【普攻4：下劈】**。
3. **段落3：派生停顿/蓄力窗口（Delayed/Hold Window）**
   - **机制**：标准连段窗口已过期。此时动作并非转为完全的 Idle 后摇，而是进入到了一个特定的“收刀 / 武器蓄电”动作帧。
   - **输入响应**：官方描述为“停顿后点按”。若此时玩家按下轻击，不再触发普通的下劈，而是触发特殊派生分支 —— **【分支重击：落雷】**。
4. **段落4：空闲重置后摇（Reset/Idle Window）**
   - **机制**：特效与蓄电均已结束。角色进行真正的收招归位动作。
   - **输入响应**：所有的连段窗口均已关闭。如果在此时按下轻击，系统将其视同为在 Idle 状态下的起手，直接重置派生链 —— **【普攻1】**。

**我们现有系统的缺陷：**
我们此前仅仅使用了一个时间节点事件（`InputAvailable`）将技能一刀切成了`前两段（死区）`和`后两段（后摇期）`。在昨晚经过重构后，我们的“后摇期”直接拥有了上述**段落4（重置起手）**的逻辑，这非常棒；但核心问题是：**我们的数据结构无法表达“段落3（停顿特征派生）”，时间点过于单一，根本无法实现时间区间控制。**

---

## 2. 次世代动作派生方案：基于时间轴片段标签（Tag-based Window Clip）

为了实现免硬编码且对策划极度友好的派生设计，我强烈建议在现有的 `SkillEditor` 体系中，抛弃单点触发的 Signal Event，转而开发一套面状的 **连段窗口履带 (Combo Window Track)**。

在这个履带上，你可以放置任意长短的“窗口片段 (Combo Window Clip)”。

### 2.1 运作闭环原理解析
1. **Clip 映射 Tag 标签**：策划在 `Combo Window Track` 上放置片段时，可以在右侧属性栏为这个 Clip 填入一个 Tag 标识。例如 `"Normal"`, `"Delayed"`, `"AirOnly"`。
2. **实体的窗口沙盒**：在技能运行期间，只要时间标记（Playhead）身处于这个 Clip 当中，`CharacterSkillState` 就会向集合 `ActiveComboTags` 压入这个标签；一旦离开，就将其移除。
   - *(这意味着你可以同时存在多个合法的重叠窗口)*
3. **OutTransition 鉴权升级**：改造原有的 `ComboTransition` SO 数据结构，除了 `RequiredCommand`（输入指令）之外，再新增一个字段 `RequiredWindowTag`。
4. **跳转判定流**：
   - 玩家按下攻击（或者有遗留缓冲需结算）。
   - 引擎检查 `ActiveComboTags` 是否为空：
     - **不为空**：遍历 `OutTransitions` 连段表。如果表里的派生要求 `RequiredWindowTag="Delayed"`，而当前人物的 `ActiveComboTags` 里正好包含 `"Delayed"`，且按键吻合！瞬间跳转！
     - **为空**（或查表无果）：说明所有能够用于派生的窗口统统结束了（或不包含该分支）。此时直接执行**保底重置操作**（像 Idle 一样重新播普攻1，也就是我们重构的逻辑）。

### 2.2 应对“安比普攻3”的配置实战沙盘：
*假设我们正在编辑 安比普攻3 的 SkillEditor Timeline*：

- 时间轴 `0.0s ~ 0.5s`：**挥刀与伤害判定**。（此处没有划定连招窗口 Clip，所以所有的按键只会被缓冲）
- 时间轴 `0.3s ~ 0.6s`：**【创建 Combo Window Clip：设为 "Normal"】**
- 时间轴 `0.6s ~ 1.0s`：**【创建 Combo Window Clip：设为 "Delayed"】**

**角色 ScriptableObject (OutTransitions) 配置**：
- **派生项A**：按键=`BasicAttack`，窗口标签=`"Normal"`，指向动作=`下劈`
- **派生项B**：按键=`BasicAttack`，窗口标签=`"Delayed"`，指向动作=`落雷`

**玩家操作实况：**
1. **瞎按狂魔**：在 0.1s 时就开始狂按轻击。系统将轻击存入 `_bufferedInput`。跑到 `0.3s` 瞬间，系统进入了 "Normal" Clip 触发鉴权，发现配置匹配派生项A，直接使出【下劈】。
2. **高端玩家**：知道落雷有多强，打完第3段后强忍住不按（缓冲池一直为空）。跑到 `0.6s` 以后，"Normal" 过期，"Delayed" 生效。此时他轻轻点按轻击，进入鉴权，匹配派生项B被激活，触发【落雷】！
3. **发呆萌新**：打完第3段挂机，直到 `1.2s` （也就是收刀要结束时）才按轻击。此时没有任何 Clip，`ActiveComboTags` 为空，查表失败，直接触发保底重置机制，人物重新从背后抽出刀，老老实实打出【普攻1】。

---

## 3. 具体改造路线图（Implementation Plan）

如果你认可这套符合工业级 ACT 流水线的理念，我建议分为三步进行落地：

### 第一期：数据底座升级
1. `ComboTransition.cs` 新增 `public string RequiredWindowTag;` （如果为空代表无条件放行，向下兼容）。
2. 在 `CharacterSkillState` 中引入 `public HashSet<string> ActiveComboTags` 用于存储当前已激活的所有标签。

### 第二期：Skill Editor 拓展
1. **Data 层**：新增 `ComboWindowTrack`， `ComboWindowClip`（字段包含 `public string ComboTag;`）。
2. **Process 层**：编写对应的播放器 `ComboWindowProcess`。在其 `OnEnter()` 时向 Context（或抛全局事件）写入对应的 Tag，在其 `OnExit()` 时立刻剥离对应的 Tag。

### 第三期：状态机输入判定对接
1. 废弃曾经死板的单一截断点事件 `OnReceiveTimelineEvent("InputAvailable")`。
2. 使用 `ComboWindowProcess` 每当 `OnEnter` 抛起一个合法标签的同时，主动通知状态机执行一次 `TryConsumeBufferedInput()`，从而完美衔接缓冲派生判定。 
3. 在玩家按键派发（即当前的后摇期判定中），如果 `ActiveComboTags` 有值，则优先走 `TryAdvanceComboFromTransitions`；完全无值才走基底重置逻辑。

通过上述架构，你甚至能够不写任何代码，用它去实现极其变态的逻辑，比如：完美闪避后的特立独行大冲刺分支、只有在特定蓄气帧按下指定键才触发的反解，潜力均可由策划自由掌控！
