# MAnimSystem 鎺ュ叆 SkillEditor 瀹炵幇璁″垝

## 涓€銆佷换鍔℃杩?
灏?MAnimSystem 鎵╁睍浠ユ敮鎸?SkillEditor 鐨?`IAnimationService` 鎺ュ彛锛屽疄鐜板姩鐢绘湇鍔￠泦鎴愩€?
**閲嶈鏇存柊 (2026-02-14)**锛氬熀浜庡抚鍚屾鏋舵瀯鐨勬纭悊瑙ｏ紝瀵瑰師鏈夎璁¤繘琛屼簡閲嶅ぇ淇銆傝瑙?[甯у悓姝ユ灦鏋勪慨姝ｆ柟妗圿(./MAnimSystem_FrameSync_Refactor_Plan.md)銆?
---

## 浜屻€佷换鍔℃竻鍗?
### 浠诲姟 1: ISkillContext 澧炲姞 IsPreviewMode 灞炴€?
**鏂囦欢**: `Assets/ATEditor/Runtime/System/ISkillContext.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

```csharp
public interface ISkillContext
{
    GameObject Owner { get; }
    
    /// <summary>
    /// 鏄惁涓虹紪杈戝櫒棰勮妯″紡銆?    /// true: 缂栬緫鍣ㄩ瑙堬紝闇€瑕佹墜鍔ㄩ噰鏍峰姩鐢诲抚銆?    /// false: 杩愯鏃舵ā寮忥紝鍔ㄧ敾鐢?Unity 鑷姩椹卞姩銆?    /// </summary>
    bool IsPreviewMode { get; }
    
    T GetService<T>() where T : class;
}
```

---

### 浠诲姟 2: ClipContext 瀹炵幇 IsPreviewMode

**鏂囦欢**: `Assets/ATEditor/Runtime/System/SkillRunner.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

1. ClipContext 绫绘柊澧?`IsPreviewMode` 灞炴€?2. `EvaluateAt` 鏂规硶璁剧疆 `IsPreviewMode = true`
3. `Tick` 鏂规硶璁剧疆 `IsPreviewMode = false`

---

### 浠诲姟 3: AnimationClipProcessor.OnUpdate 鍐呴儴鍒ゆ柇妯″紡

**鏂囦欢**: `Assets/ATEditor/Runtime/Logic/Processors/AnimationClipProcessor.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

```csharp
public override void OnUpdate(ISkillContext context, float progress)
{
    // 杩愯鏃剁洿鎺ヨ繑鍥烇紝涓嶅仛閲囨牱
    if (!context.IsPreviewMode) return;
    
    // 浠ヤ笅浠呯紪杈戝櫒棰勮妯″紡鎵ц
    var animService = context.GetService<IAnimationService>();
    var data = context.GetData<AnimationClip>();
    
    if (data != null && animService != null)
    {
        float time = data.StartTime + data.Duration * progress;
        animService.Evaluate(time);
    }
}
```

---

### 浠诲姟 4: AnimComponent 绉婚櫎 UpdateMode

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimComponent.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

1. 绉婚櫎 `UpdateMode` 鏋氫妇鍜?`updateMode` 瀛楁
2. `Update()` 鏂规硶濮嬬粓鎵ц `UpdateInternal()`
3. `ManualUpdate` 鏍囪涓?`[Obsolete]`锛屾帹鑽愪娇鐢?`SetSpeed`
4. 鏂板 `SetSpeed(float speedScale)` 鏂规硶

**鏍稿績鏀瑰姩**:

```csharp
private void Update()
{
    if (!_isGraphCreated) return;
    // 濮嬬粓鑷姩鏇存柊锛岀敱 Unity 椹卞姩
    UpdateInternal(Time.deltaTime);
}

public void SetSpeed(float speedScale)
{
    if (!_isGraphCreated) return;
    foreach (var layer in _layers)
    {
        layer?.SetSpeed(speedScale);
    }
}
```

---

### 浠诲姟 5: AnimLayer 澧炲姞 SetSpeed 鏂规硶

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

**鐘舵€?*: 寰呮墽琛?
**鏂板鍐呭**:

```csharp
public void SetSpeed(float speed)
{
    if (_targetState != null)
    {
        _targetState.Speed = speed;
    }
}
```

---

### 浠诲姟 6: IAnimationService 鎺ュ彛鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/IServices.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

灏?`ManualUpdate(float deltaTime)` 鏀逛负 `SetSpeed(float speedScale)`锛屾槑纭涔夈€?
```csharp
public interface IAnimationService
{
    void Play(UnityEngine.AnimationClip clip, float transitionDuration);
    void Evaluate(float time);  // 浠呯紪杈戝櫒棰勮
    void SetSpeed(float speedScale);  // 閫熷害鎺у埗
}
```

---

### 浠诲姟 7: 鍒涘缓 MAnimAnimationService 閫傞厤鍣?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/MAnimAnimationService.cs` (鏂板缓)

**鐘舵€?*: 寰呮墽琛?
**鏍稿績璁捐**:

- `Play`: 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕?- `Evaluate`: 浠呯紪杈戝櫒棰勮闇€瑕?- `SetSpeed`: 鐢ㄤ簬閫熷害鎺у埗

---

### 浠诲姟 8: RuntimeAnimationService 鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/RuntimeAnimationService.cs`

**鐘舵€?*: 寰呮墽琛?
**淇敼鍐呭**:

瀹炵幇鏂扮殑 `IAnimationService` 鎺ュ彛锛宍Evaluate` 鏂规硶鐣欑┖锛堣繍琛屾椂涓嶉渶瑕侊級銆?
---

## 涓夈€佹枃浠跺彉鏇存竻鍗?
| 鏂囦欢 | 鎿嶄綔 | 鍙樻洿鍐呭 |
|------|------|----------|
| `ISkillContext.cs` | 淇敼 | 鏂板 `IsPreviewMode` 灞炴€?|
| `SkillRunner.cs` | 淇敼 | ClipContext 瀹炵幇 IsPreviewMode |
| `AnimationClipProcessor.cs` | 淇敼 | OnUpdate 鍐呴儴鍒ゆ柇 IsPreviewMode |
| `AnimComponent.cs` | 淇敼 | 绉婚櫎 UpdateMode锛屾柊澧?SetSpeed |
| `AnimLayer.cs` | 淇敼 | 鏂板 SetSpeed 鏂规硶 |
| `IServices.cs` | 淇敼 | ManualUpdate 鏀逛负 SetSpeed |
| `MAnimAnimationService.cs` | 鏂板缓 | IAnimationService 閫傞厤鍣?|
| `RuntimeAnimationService.cs` | 淇敼 | 鏇存柊鎺ュ彛瀹炵幇 |

---

## 鍥涖€侀獙璇佽鍒?
### 4.1 鍗曞厓娴嬭瘯

1. **AnimComponent 椹卞姩娴嬭瘯**
   - 楠岃瘉 Update 濮嬬粓鑷姩鎵ц
   - 楠岃瘉 SetSpeed 姝ｇ‘褰卞搷鎾斁閫熷害

2. **IsPreviewMode 娴嬭瘯**
   - 缂栬緫鍣ㄩ瑙堟椂 IsPreviewMode = true
   - 杩愯鏃?Tick 鏃?IsPreviewMode = false

3. **AnimationClipProcessor 娴嬭瘯**
   - 棰勮妯″紡锛歄nUpdate 璋冪敤 Evaluate
   - 杩愯鏃舵ā寮忥細OnUpdate 涓嶈皟鐢?Evaluate

### 4.2 闆嗘垚娴嬭瘯

1. **缂栬緫鍣ㄩ瑙堟祦绋?*
   - 鎷栨嫿鏃堕棿杞达紝楠岃瘉鍔ㄧ敾姝ｇ‘閲囨牱

2. **杩愯鏃跺抚鍚屾娴佺▼**
   - 妯℃嫙缃戠粶鎸囦护锛岃Е鍙戞妧鑳芥挱鏀?   - 楠岃瘉 Play 琚皟鐢紝Evaluate 涓嶈璋冪敤

---

## 浜斻€佸疄鏂介『搴?
```
浠诲姟 1: ISkillContext 澧炲姞 IsPreviewMode
    鈹?    鈻?浠诲姟 2: ClipContext 瀹炵幇 IsPreviewMode
    鈹?    鈻?浠诲姟 3: AnimationClipProcessor 鍐呴儴鍒ゆ柇
    鈹?    鈻?浠诲姟 4: AnimComponent 绉婚櫎 UpdateMode
    鈹?    鈻?浠诲姟 5: AnimLayer 澧炲姞 SetSpeed
    鈹?    鈻?浠诲姟 6: IAnimationService 鎺ュ彛鏇存柊
    鈹?    鈻?浠诲姟 7: MAnimAnimationService 閫傞厤鍣?    鈹?    鈻?浠诲姟 8: RuntimeAnimationService 鏇存柊
    鈹?    鈻?楠岃瘉娴嬭瘯
```

---

## 鍏€佹灦鏋勫姣?
### 淇敼鍓嶏紙閿欒鐞嗚В锛?
```
AnimComponent
鈹溾攢鈹€ UpdateMode: Auto / Manual
鈹溾攢鈹€ Auto:   Update() 椹卞姩
鈹斺攢鈹€ Manual: ManualUpdate(dt) 椹卞姩
```

### 淇敼鍚庯紙姝ｇ‘鐞嗚В锛?
```
AnimComponent
鈹溾攢鈹€ 濮嬬粓鐢?Unity Update 鑷姩椹卞姩
鈹溾攢鈹€ Play(clip)      鈫?鎾斁鍔ㄧ敾
鈹溾攢鈹€ SetSpeed(scale) 鈫?閫熷害鎺у埗
鈹斺攢鈹€ Evaluate(time)  鈫?缂栬緫鍣ㄩ瑙堥噰鏍?```

---

**鏂囨。鏃ユ湡**: 2026-02-14
**鏇存柊璇存槑**: 鍩轰簬甯у悓姝ユ灦鏋勬纭悊瑙ｏ紝閲嶆柊璁捐瀹炵幇鏂规
