# 鎶€鑳界紪杈戝櫒鎾斁鏋舵瀯 (Skill Editor Playback Architecture)

鏈枃妗ｆ弿杩颁簡褰撳墠鎶€鑳界紪杈戝櫒鐨勮繍琛屾椂鎾斁鏋舵瀯锛屽寘鎷牳蹇冨惊鐜€佹椂闂寸鐞嗐€佷互鍙?Editor 涓?Runtime 鐨勪氦浜掓柟寮忋€?

## 鏍稿績缁勪欢 (Core Components)

| 缁勪欢 | 鑱岃矗 | 鍏抽敭绫?|
| :--- | :--- | :--- |
| **SkillRunner** | 杩愯鏃舵牳蹇冮┍鍔ㄥ櫒锛岃礋璐ｆ椂闂寸鐞嗐€丆lip 鐢熷懡鍛ㄦ湡璋冨害 | `SkillRunner.cs` |
| **ATEditorWindow** | 缂栬緫鍣ㄦ挱鏀炬帶鍒讹紝璐熻矗棰勮妯″瀷鐨勭敓鍛藉懆鏈熺鐞嗕笌椹卞姩 | `ATEditorWindow.cs` |
| **ClipProcessor** | 鍏蜂綋閫昏緫澶勭悊鍣紝鎵ц姣忎釜 Clip 鐨勫叿浣撹涓?(鍔ㄧ敾/鐗规晥/浼ゅ绛? | `BaseClipProcessor.cs` |
| **Services** | 澶栭儴绯荤粺鎶借薄灞傦紝瑙ｈ€﹀叿浣撳姛鑳藉疄鐜?(濡?Animator, VFX) | `IServices.cs` |

## 1. 鎾斁椹卞姩寰幆 (Playback Loop)

鎾斁閫昏緫鐢?`SkillRunner` 缁熶竴椹卞姩锛屾敮鎸佷袱绉嶆洿鏂版ā寮忥細

### 1.1 Update Loop (`ManualUpdate`)

`SkillRunner.ManualUpdate(float deltaTime)` 鏄敮涓€鐨勯┍鍔ㄥ叆鍙ｃ€?

- **Auto Mode**: 鍦?Runtime 娓告垙杩愯鏃讹紝鐢?`Update()` 鑷姩璋冪敤 `ManualUpdate(Time.deltaTime)`.
- **Manual Mode**: 鍦?Editor 棰勮鏃讹紝鐢?`ATEditorWindow.Update()` 璁＄畻 `EditorApplication.timeSinceStartup` 鐨勫樊鍊煎悗鎵嬪姩璋冪敤銆?

### 1.2 鏃堕棿姝ヨ繘绛栫暐 (Time Step Strategy)

`SkillRunner` 鍐呴儴瀹炵幇浜嗚搫姘存睜 (Accumulator) 绠楁硶锛屾敮鎸佷袱绉嶆杩涙ā寮?(`TimeStepMode`)锛?

1.  **Variable (鑷敱妯″紡)**:
    - 鐩存帴閫忎紶 `deltaTime` 缁?`Tick()`銆?
    - 閫傜敤浜庡父瑙勬父鎴忛€昏緫锛屽钩婊戜絾闈炵‘瀹氭€с€?

2.  **Fixed (鍥哄畾/甯ч攣瀹氭ā寮?**:
    - 浣跨敤 `Accumulator` 绱Н鏃堕棿銆?
    - 鎸夌収鍥哄畾鐨?`fixedDt = 1.0 / FrameRate` 杩涜鍒囩墖銆?
    - 寰幆璋冪敤 `Tick(fixedDt)` 鐩村埌娑堣€楀畬绱Н鏃堕棿銆?
    - **鐢ㄩ€?*: 纭繚閫昏緫鎵ц鐨勭‘瀹氭€?(Determinism)锛屾ā鎷熸湇鍔″櫒甯у悓姝ョ幆澧冦€?

## 2. 閫昏緫璋冨害 (Logic Dispatch)

### 2.1 鍒濆鍖?(`Initialize`)
- 娓呯┖褰撳墠鐘舵€併€?
- 閬嶅巻 `activeClips`锛屼负姣忎釜 Clip 鍒涘缓瀵瑰簲鐨?`Processor` 瀹炰緥銆?
- 缁存姢 `ProcessorState` (Clip + Processor + IsRunning)銆?

### 2.2 Tick 鏍稿績 (`Tick`)
`Tick(float dt)` 璐熻矗鎺ㄨ繘鏃堕棿骞舵洿鏂版墍鏈?Processor 鐨勭姸鎬併€傚畠閲囩敤**鍖洪棿鎵弿 (Interval Scanning)** 绠楁硶锛?

1.  **鏃堕棿鎺ㄧЩ**: `CurrentTime` -> `NextTime (Current + dt)`銆?
2.  **鍖洪棿鍒ゅ畾**: 閬嶅巻鎵€鏈?Clip锛屾鏌?`[Start, End)` 涓庡綋鍓嶆椂闂寸墖 `[PrevTime, NextTime]` 鏄惁鏈変氦闆嗐€?
3.  **鐢熷懡鍛ㄦ湡浜嬩欢**:
    - **OnEnter**: Clip 鍒氳繘鍏ユ椂闂寸墖 (涔嬪墠鏈繍琛?&& 鐜板湪閲嶅彔)銆?
    - **OnUpdate**: 鎸佺画閲嶅彔銆備紶閫?`progress (0~1)`銆?
    - **OnTick**: 鎸佺画閲嶅彔銆備紶閫?`localTime` 鍜?`prevLocalTime` (鐢ㄤ簬閫昏緫甯у垽瀹?銆?
    - **OnExit**: Clip 缁撴潫鎴栦笉鍐嶉噸鍙犮€?

### 2.3 棰勮璺宠浆 (`EvaluateAt`)
鐢ㄤ簬缂栬緫鍣ㄤ笅鎷栨嫿鏃堕棿杞存椂鐨勨€滅灛绉烩€濋瑙堬細
- **涓嶈Е鍙?Tick**: 閬垮厤瑙﹀彂涓棿杩囩▼鐨勫壇浣滅敤 (濡備激瀹冲垽瀹?銆?
- **寮哄埗瑕嗙洊鏃堕棿**: 鐩存帴璁剧疆 `CurrentTime = targetTime`銆?
- **鐘舵€侀噰鏍?*: 璋冪敤 `Processor.OnSample(time)` (閫氬父鍥為€€鍒?`OnUpdate`) 浠ュ埛鏂扮敾闈㈣〃鐜?(濡傚姩鐢诲Э鎬?銆?

## 3. 鏈嶅姟灞備氦浜?(Service Interaction)

涓轰簡瑙ｈ€?Runtime 涓?Editor锛孲killRunner 閫氳繃 `ClipContext` 鎻愪緵鏈嶅姟瀹氫綅鍣細

- **Context**: 姣忎釜 `Processor` 鎺ユ敹涓€涓?`ISkillContext`銆?
- **Registration**: 澶栭儴 (濡?EditorWindow) 娉ㄥ唽鍏蜂綋鏈嶅姟 (濡?`EditorAnimationService`) 鍒?Context銆?
- **Usage**: Processor 閫氳繃 `context.GetService<IAnimationService>()` 鑾峰彇鏈嶅姟銆?

### 鍏抽敭鏈嶅姟
- `IAnimationService`: 灏佽 `Animator` 鎿嶄綔 (`Play`, `Evaluate`, `ManualUpdate`).
- `IVFXService`: 灏佽绮掑瓙鐗规晥鐢熸垚涓庣敓鍛藉懆鏈熺鐞?

## 4. 缂栬緫鍣ㄩ瑙堟祦绋?(Editor Preview Flow)

1.  **EnsurePreviewRunner**:
    - 妫€鏌ラ瑙堟ā鍨?(Preview Model)銆?
    - 鎸傝浇鎴栬幏鍙?`SkillRunner` 缁勪欢銆?
    - 娉ㄥ叆鏈嶅姟 (`EditorAnimationService`, `RuntimeVFXService`)銆?
    - 娉ㄥ叆褰撳墠 Timeline 鏁版嵁 (`activeClips`)銆?

2.  **Update**:
    - 璁＄畻 `dt`銆?
    - 璋冪敤 `_previewRunner.ManualUpdate(dt)`銆?
    - 鍚屾 UI 杩涘害鏉?(`state.timeIndicator`).
    - 澶勭悊寰幆鎾斁閫昏緫.

3.  **Timeline Drag**:
    - 瑙﹀彂 `OnPreviewTimeChanged`.
    - 璋冪敤 `_previewRunner.EvaluateAt(time)`.

## 绫诲浘鍏崇郴 (Simple Class Diagram)

```mermaid
graph TD
    Window[ATEditorWindow] -->|Drives| Runner[SkillRunner]
    Window -->|Injects| Service[Services (Anim/VFX)]
    Runner -->|Updates| Processor[BaseClipProcessor]
    Runner -->|Uses| Context[ClipContext]
    Processor -->|Calls| Context
    Context -->|Locates| Service
    
    subgraph "Runtime Core"
    Runner
    Processor
    Context
    end
```
