# 动画层 Mask 托管方案设计

## 1. 问题背景
在当前 `SkillEditor` 系统中，多个 `AnimationProcess` 可能同时操作同一个 `AnimLayer` 的 `AvatarMask`。
如果采用简单的“进入时备份 -> 退出时还原”策略，在**嵌套重叠**（Nest Overlap）场景下会失效。

**失效案例**：
1. A 进入，备份 Default，设置 MaskA。
2. B 进入，备份 MaskA，设置 MaskB。
3. **B 退出**，还原 MaskA。
4. **A 退出**，还原 Default。

看似正常，但如果 **B 在 A 之前退出**（即 A 包裹 B），则 A 退出时逻辑正确。
但如果 **B 在 A 之后退出**（即 B 包裹 A，或交叉）：
1. A 进入，备份 Default，设置 MaskA。
2. B 进入，备份 MaskA，设置 MaskB。
3. **A 退出**，还原 Default（错误！此时 B 还在运行，Mask 应该保持 MaskB）。
4. **B 退出**，还原 MaskA（错误！此时 A 已结束，Mask 应该恢复 Default）。

此外，如果 A 和 B 同时运行，谁的 Mask 生效？理应是**最新进入**（或优先级最高）的生效。

## 2. 核心思路：托管栈 (Mask Managed Stack)

不再由 Process 个体维护状态，而是由 `ProcessContext` 统一托管一个 **Mask 覆盖栈**。

- **入栈**：Process 进入时，向 Context 注册自己的 Mask。
- **出栈**：Process 退出时，向 Context 注销自己的 Mask。
- **仲裁**：Context 根据栈顶元素决定当前 Layer 的 Mask。

## 3. 数据结构

### 3.1 MaskState (内部类)
每个动画层对应一个 `MaskState` 实例，存储该层的覆盖状态。

```csharp
public class LayerMaskState
{
    // 原始 Mask（进入该层第一个 Clip 之前的 Mask，通常为 null 或 Base Mask）
    public AvatarMask OriginalMask;
    
    // 活跃的 Mask 列表（用 List 模拟栈，支持任意位置移除）
    // 列表尾部 = 栈顶 = 当前生效的 Mask
    public List<AvatarMask> Overrides = new List<AvatarMask>();
}
```

### 3.2 ProcessContext
在 `ProcessContext` 中维护一个字典：

```csharp
private Dictionary<int, LayerMaskState> _layerMaskStates = new Dictionary<int, LayerMaskState>();
```

## 4. 逻辑流程

### 4.1 注册 Mask (OnEnter 调用)

```csharp
public void PushLayerMask(int layerIndex, AvatarMask mask, AnimComponent animComp)
{
    if (mask == null) return;

    if (!_layerMaskStates.TryGetValue(layerIndex, out var state))
    {
        // 第一次有 Clip 进入该层，记录原始 Mask
        state = new LayerMaskState();
        state.OriginalMask = animComp.GetLayerMask(layerIndex);
        _layerMaskStates[layerIndex] = state;
    }

    // 入栈
    state.Overrides.Add(mask);

    // 应用栈顶 Mask
    animComp.SetLayerMask(layerIndex, mask);
}
```

### 4.2 注销 Mask (OnExit 调用)

```csharp
public void PopLayerMask(int layerIndex, AvatarMask mask, AnimComponent animComp)
{
    if (mask == null) return;

    if (_layerMaskStates.TryGetValue(layerIndex, out var state))
    {
        // 移除该 Mask（处理中间退出的情况）
        if (state.Overrides.Remove(mask))
        {
            // 重新计算应生效的 Mask
            if (state.Overrides.Count > 0)
            {
                // 还有其他 Override，应用栈顶（List 最后一个）
                var topMask = state.Overrides[state.Overrides.Count - 1];
                animComp.SetLayerMask(layerIndex, topMask);
            }
            else
            {
                // 栈空，恢复原始 Mask
                animComp.SetLayerMask(layerIndex, state.OriginalMask);
                
                // 可选：清理 State，节省内存（下次进入重新获取 Original）
                _layerMaskStates.Remove(layerIndex);
            }
        }
    }
}
```

## 5. 场景推演验证

### 场景 1：嵌套 (A 包含 B)
- **A 进**: Stack=[A]。Mask=A。
- **B 进**: Stack=[A, B]。Mask=B。
- **B 出**: Stack=[A]。Mask=A。 (正确：恢复 A)
- **A 出**: Stack=[]。Mask=Default。 (正确：恢复默认)

### 场景 2：交叉 (A 先进，B 后进，A 先出，B 后出)
- **A 进**: Stack=[A]。Mask=A。
- **B 进**: Stack=[A, B]。Mask=B。 (B 覆盖 A)
- **A 出**: A 从 Stack 移除。Stack=[B]。Mask=B。 (正确：B 仍在运行，保持 B)
- **B 出**: Stack=[]。Mask=Default。 (正确)

### 场景 3：同帧进出 (A 进 -> B 进 -> A 出)
- 逻辑同上，List.Remove 操作保证了即使 A 不是栈顶也能被正确移除，且不影响栈顶 B 的生效。

## 6. API 变动

### ProcessContext.cs
- 新增 `PushLayerMask(int layer, AvatarMask mask, AnimComponent comp)`
- 新增 `PopLayerMask(int layer, AvatarMask mask, AnimComponent comp)`

### RuntimeAnimationProcess.cs
- `OnEnter`: 调用 `context.PushLayerMask(...)`
- `OnExit`: 调用 `context.PopLayerMask(...)`
- 移除原有的 `originalMask` 字段和手动维护逻辑。

此方案完美解决了多 Process 对同一 Layer 属性的竞争问题。
