# MAnimSystem 甯у悓姝ユ灦鏋勪慨姝ｅ伐浣滄€荤粨

## 姒傝堪

鍩轰簬瀵瑰抚鍚屾鏋舵瀯鐨勬纭悊瑙ｏ紝瀵?MAnimSystem 鍜?SkillEditor 杩涜浜嗛噸澶т慨姝ｃ€傛牳蹇冨彉鍖栨槸灏嗗姩鐢婚┍鍔ㄦā寮忎粠"鎵嬪姩椹卞姩"鏀逛负"濮嬬粓鑷姩椹卞姩"锛屾槑纭簡杩愯鏃跺拰缂栬緫鍣ㄩ瑙堢殑涓嶅悓澶勭悊鏂瑰紡銆?
---

## 瀹屾垚鐨勫伐浣?
### 1. ISkillContext 澧炲姞 IsPreviewMode 灞炴€?
**鏂囦欢**: `Assets/ATEditor/Runtime/System/ISkillContext.cs`

- 鏂板 `IsPreviewMode` 灞炴€?- 鐢ㄤ簬鍖哄垎缂栬緫鍣ㄩ瑙堟ā寮忓拰杩愯鏃舵ā寮?
```csharp
public interface ISkillContext
{
    GameObject Owner { get; }
    bool IsPreviewMode { get; }  // 鏂板
    T GetService<T>() where T : class;
}
```

### 2. ClipContext 瀹炵幇 IsPreviewMode

**鏂囦欢**: `Assets/ATEditor/Runtime/System/SkillRunner.cs`

- ClipContext 绫诲疄鐜?`IsPreviewMode` 灞炴€?- `EvaluateAt()` 鏂规硶璁剧疆 `IsPreviewMode = true`锛堢紪杈戝櫒棰勮鍏ュ彛锛?- `ManualUpdate()` 鍜?`Tick()` 鏂规硶璁剧疆 `IsPreviewMode = false`锛堣繍琛屾椂鍏ュ彛锛?- `NotifyServicesUpdate()` 璋冪敤 `SetSpeed()` 鏇夸唬 `ManualUpdate()`

### 3. AnimationClipProcessor 鍐呴儴鍒ゆ柇妯″紡

**鏂囦欢**: `Assets/ATEditor/Runtime/Logic/Processors/AnimationClipProcessor.cs`

- `OnUpdate()` 鏂规硶鍐呴儴鍒ゆ柇 `IsPreviewMode`
- 杩愯鏃剁洿鎺ヨ繑鍥烇紝涓嶅仛閲囨牱
- 浠呯紪杈戝櫒棰勮妯″紡鎵ц `Evaluate()`

```csharp
public override void OnUpdate(ISkillContext context, float progress)
{
    if (!context.IsPreviewMode) return;  // 杩愯鏃朵笉閲囨牱
    // 缂栬緫鍣ㄩ瑙堬細鎵嬪姩閲囨牱鍔ㄧ敾甯?    animService.Evaluate(time);
}
```

### 4. AnimComponent 绉婚櫎 UpdateMode

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimComponent.cs`

- 绉婚櫎 `UpdateMode` 鏋氫妇
- 绉婚櫎 `updateMode` 瀛楁
- `Update()` 鏂规硶濮嬬粓鎵ц `UpdateInternal()`
- 鏂板 `SetSpeed(float speedScale)` 鏂规硶鐢ㄤ簬閫熷害鎺у埗

### 5. AnimLayer 澧炲姞 SetSpeed 鏂规硶

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

- 鏂板 `SetSpeed(float speed)` 鏂规硶
- 璁剧疆褰撳墠鍔ㄧ敾鐨勬挱鏀鹃€熷害

### 6. IAnimationService 鎺ュ彛鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/IServices.cs`

- `ManualUpdate(float deltaTime)` 鈫?`SetSpeed(float speedScale)`
- 鏄庣‘璇箟锛氶€熷害鎺у埗鑰岄潪椹卞姩鏇存柊
- 娣诲姞璇︾粏鐨?XML 娉ㄩ噴璇存槑鍚勬柟娉曠殑鐢ㄩ€?
### 7. 鍒涘缓 MAnimAnimationService 閫傞厤鍣?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/MAnimAnimationService.cs` (鏂板缓)

- 瀹炵幇 `IAnimationService` 鎺ュ彛
- 灏?SkillEditor 鐨勫姩鐢昏皟鐢ㄨ浆鍙戝埌 AnimComponent
- 鍖呭惈璇︾粏鐨?XML 娉ㄩ噴璇存槑璁捐鎰忓浘

### 8. RuntimeAnimationService 鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/RuntimeAnimationService.cs`

- 瀹炵幇鏂扮殑 `IAnimationService` 鎺ュ彛
- `Evaluate()` 鏂规硶鐣欑┖锛堣繍琛屾椂涓嶉渶瑕侊級
- `SetSpeed()` 璁剧疆 Animator 鐨勬挱鏀鹃€熷害

---

## 鏋舵瀯瀵规瘮

### 淇敼鍓嶏紙閿欒鐞嗚В锛?
```
AnimComponent
鈹溾攢鈹€ UpdateMode: Auto / Manual
鈹溾攢鈹€ Auto:   Update() 椹卞姩
鈹斺攢鈹€ Manual: ManualUpdate(dt) 椹卞姩

IAnimationService
鈹溾攢鈹€ Play(clip, duration)
鈹溾攢鈹€ Evaluate(time)      鈫?杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕?鈹斺攢鈹€ ManualUpdate(dt)    鈫?椹卞姩鍔ㄧ敾鏇存柊
```

### 淇敼鍚庯紙姝ｇ‘鐞嗚В锛?
```
AnimComponent
鈹溾攢鈹€ 濮嬬粓鐢?Unity Update 鑷姩椹卞姩
鈹溾攢鈹€ Play(clip)      鈫?鎾斁鍔ㄧ敾
鈹溾攢鈹€ SetSpeed(scale) 鈫?閫熷害鎺у埗
鈹斺攢鈹€ Evaluate(time)  鈫?缂栬緫鍣ㄩ瑙堥噰鏍?
IAnimationService
鈹溾攢鈹€ Play(clip, duration)   鈫?杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕?鈹溾攢鈹€ Evaluate(time)         鈫?浠呯紪杈戝櫒棰勮
鈹斺攢鈹€ SetSpeed(speedScale)   鈫?閫熷害鎺у埗
```

---

## 杩愯鏃跺抚鍚屾娴佺▼

```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                    杩愯鏃舵ā寮?(Runtime)                         鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                                                                鈹?鈹? 缃戠粶灞傛敹鍒版寚浠?{ skillId, frame }                                鈹?鈹?      鈫?                                                        鈹?鈹? SkillRunner.ManualUpdate(fixedDt)                              鈹?鈹?      鈫?                                                        鈹?鈹? _context.IsPreviewMode = false                                 鈹?鈹?      鈫?                                                        鈹?鈹? Tick(fixedDt) 鎺ㄨ繘鍥哄畾姝ラ暱                                      鈹?鈹?      鈫?                                                        鈹?鈹? OnEnter 鈫?Play(clip)           鈫?鍙彂鎺у埗鍛戒护                   鈹?鈹? OnUpdate 鈫?鐩存帴杩斿洖             鈫?涓嶅仛閲囨牱                      鈹?鈹? OnTick 鈫?閫昏緫鍒ゅ畾锛堜激瀹冲垽瀹氱瓑锛?                                 鈹?鈹? OnExit 鈫?鍒囨崲鐘舵€?杩斿洖寰呮満                                       鈹?鈹?      鈫?                                                        鈹?鈹? AnimComponent 鐢?Unity Update 鑷姩椹卞姩                          鈹?鈹?                                                                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                    缂栬緫鍣ㄦā寮?(Editor Preview)                  鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                                                                鈹?鈹? SkillRunner.EvaluateAt(time)  鈫?鎷栨嫿鏃堕棿杞?                     鈹?鈹?      鈫?                                                        鈹?鈹? _context.IsPreviewMode = true                                  鈹?鈹?      鈫?                                                        鈹?鈹? OnEnter 鈫?Play(clip)                                           鈹?鈹? OnUpdate 鈫?Evaluate(time)  鈫?鎵嬪姩閲囨牱鍔ㄧ敾甯?                    鈹?鈹? OnExit 鈫?鍋滄/娓呯悊                                              鈹?鈹?                                                                鈹?鈹? 鐗圭偣锛氭椂闂村彲璺宠穬锛岄渶瑕佹墜鍔ㄩ噰鏍?                                   鈹?鈹?                                                                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

---

## 娴嬭瘯缁撴灉

| 娴嬭瘯椤?| 缁撴灉 |
|--------|------|
| ISkillContext.IsPreviewMode | 鉁?鎺ュ彛瀹氫箟姝ｇ‘ |
| ClipContext 瀹炵幇 | 鉁?灞炴€ф纭缃?|
| AnimationClipProcessor 鍒ゆ柇 | 鉁?杩愯鏃朵笉閲囨牱 |
| AnimComponent 椹卞姩 | 鉁?濮嬬粓 MonoUpdate |
| SetSpeed 鍔熻兘 | 鉁?閫熷害鎺у埗姝ｅ父 |
| IAnimationService 鎺ュ彛 | 鉁?璇箟鏄庣‘ |
| MAnimAnimationService 閫傞厤鍣?| 鉁?杞彂姝ｇ‘ |
| RuntimeAnimationService | 鉁?鎺ュ彛瀹炵幇瀹屾暣 |

---

## 鏂囦欢鍙樻洿娓呭崟

| 鏂囦欢 | 鍙樻洿绫诲瀷 | 琛屾暟鍙樺寲 |
|------|----------|----------|
| ISkillContext.cs | 淇敼 | +7 琛?|
| SkillRunner.cs | 淇敼 | +15 琛?|
| AnimationClipProcessor.cs | 閲嶅啓 | +20 琛?|
| AnimComponent.cs | 閲嶅啓 | -30 琛?|
| AnimLayer.cs | 淇敼 | +13 琛?|
| IServices.cs | 淇敼 | +15 琛?|
| MAnimAnimationService.cs | 鏂板缓 | +75 琛?|
| RuntimeAnimationService.cs | 閲嶅啓 | +25 琛?|

---

## 鐩稿叧鏂囨。

- [甯у悓姝ユ灦鏋勪慨姝ｆ柟妗圿(./MAnimSystem_FrameSync_Refactor_Plan.md)
- [SkillEditor 闆嗘垚璁″垝](./MAnimSystem_SkillEditor_Integration_Plan.md)

---

## 娉ㄦ剰浜嬮」

1. **AnimComponent 濮嬬粓鑷姩椹卞姩**
   - 涓嶅啀闇€瑕佹墜鍔ㄨ皟鐢?`ManualUpdate()`
   - 鍔ㄧ敾鐢?Unity Update 鑷姩鏇存柊

2. **Evaluate 浠呯敤浜庣紪杈戝櫒棰勮**
   - 杩愯鏃惰鍕胯皟鐢?`Evaluate()`
   - 浠呭湪缂栬緫鍣ㄦ嫋鎷芥椂闂磋酱鏃朵娇鐢?
3. **SetSpeed 鐢ㄤ簬閫熷害鎺у埗**
   - 鐢ㄤ簬甯у悓姝ュ満鏅笅鐨勯€熷害璋冩暣
   - 涓嶅奖鍝嶅姩鐢婚┍鍔ㄦ柟寮?
4. **IsPreviewMode 鍒ゆ柇**
   - 缂栬緫鍣ㄩ瑙堟椂涓?`true`
   - 杩愯鏃朵负 `false`
   - Processor 鍙嵁姝ゅ喅瀹氭槸鍚﹂噰鏍?
---

**淇鏃ユ湡**: 2026-02-14
