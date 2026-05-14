---
description: 瀵?SkillEditor 杩涜鍏ㄩ潰鐨勬灦鏋勫垎鏋愪笌璇勪及锛屾寜 缂栬緫鍣?杩愯鏃?脳 Data/View/Logic 缁村害鍒掑垎锛屽垎澶氭杈撳嚭鐙珛鍒嗘瀽鎶ュ憡鑷?ATEditor/docs 鐩綍
---

# SkillEditor 鍏ㄩ潰鍒嗘瀽涓庤瘎浼?Workflow

> **杈撳嚭鐩綍**: `Assets/ATEditor/docs/`
> **椤圭洰鏍圭洰褰?*: `Assets/ATEditor/`
> **澶栭儴閫傞厤鍣?*: `Assets/GameClient/Adapters/` (Skill 鐩稿叧)

---

## 鍓嶇疆鍑嗗

// turbo-all

1. **纭鐩綍缁撴瀯**
   ```
   Assets/ATEditor/
   鈹溾攢鈹€ Editor/           # 缂栬緫鍣ㄤ晶浠ｇ爜
   鈹?  鈹溾攢鈹€ Core/         # 鏍稿績鐘舵€佷笌娉ㄥ唽琛?
   鈹?  鈹溾攢鈹€ Drawers/      # Inspector 缁樺埗鍣?Base + Impl)
   鈹?  鈹溾攢鈹€ Enums/        # 缂栬緫鍣ㄦ灇涓?
   鈹?  鈹溾攢鈹€ Language/     # 澶氳瑷€绯荤粺
   鈹?  鈹溾攢鈹€ Playback/     # 缂栬緫鍣ㄩ瑙堟挱鏀?鍚?Processes/)
   鈹?  鈹溾攢鈹€ Resources/    # 缂栬緫鍣ㄨ祫婧?棰勮瑙掕壊)
   鈹?  鈹溾攢鈹€ Views/        # 瑙嗗浘缁勪欢(Timeline/TrackList/Toolbar)
   鈹?  鈹溾攢鈹€ ATEditorWindow.cs
   鈹?  鈹溾攢鈹€ ATEditorSettingsWindow.cs
   鈹?  鈹斺攢鈹€ TrackObjectWrapper.cs
   鈹溾攢鈹€ Runtime/          # 杩愯鏃朵唬鐮?
   鈹?  鈹溾攢鈹€ Attributes/   # 鑷畾涔夌壒鎬?
   鈹?  鈹溾攢鈹€ Data/         # 鏁版嵁妯″瀷(ClipBase/TrackBase/Clips/Tracks)
   鈹?  鈹溾攢鈹€ Enums/        # 杩愯鏃舵灇涓?
   鈹?  鈹溾攢鈹€ Playback/     # 杩愯鏃舵挱鏀?Core/Interfaces/Lifecycle/Processes)
   鈹?  鈹溾攢鈹€ Sample/       # 绀轰緥瀹炵幇(CharSkillActor)
   鈹?  鈹斺攢鈹€ Serialization/ # 搴忓垪鍖栧伐鍏?
   鈹溾攢鈹€ Settings/         # 閰嶇疆璧勬簮(SkillTagConfig.asset)
   鈹溾攢鈹€ Test/             # 娴嬭瘯鑴氭湰
   鈹斺攢鈹€ docs/             # 鍒嗘瀽鎶ュ憡杈撳嚭鐩綍(蹇界暐宸叉湁鍐呭)
   ```

2. **纭澶栭儴閫傞厤鍣ㄦ枃浠?*
   - `Assets/GameClient/Adapters/GameSkillAudioHandler.cs`
   - `Assets/GameClient/Adapters/SkillServiceFactory.cs`
   - `Assets/GameClient/Adapters/SkillProjectile.cs`
   - `Assets/GameClient/Adapters/SkillSpawnHandler.cs`

---

## 鍒嗘瀽姝ラ鎬昏

鏈?workflow 灏嗗垎鏋愬伐浣滄媶鍒嗕负 **8 涓嫭绔嬫姤鍛?*锛屾瘡涓姤鍛婅仛鐒︿竴涓淮搴︺€傚垎鏋愭椂椤婚€愪竴鎵ц锛岀‘淇濇瘡浠芥姤鍛婂唴瀹瑰畬鏁淬€佽灏藉悗鍐嶈繘鍏ヤ笅涓€姝ラ銆?

| 姝ラ | 鎶ュ憡鏂囦欢鍚?| 缁村害 | 鑱氱劍鑼冨洿 |
|:----:|:----------|:----:|:---------|
| 1 | `01_runtime_data_analysis.md` | 杩愯鏃?脳 Data | 鏁版嵁妯″瀷涓庡簭鍒楀寲 |
| 2 | `02_runtime_logic_analysis.md` | 杩愯鏃?脳 Logic | 鎾斁鏍稿績涓庡鐞嗗櫒 |
| 3 | `03_runtime_interfaces_analysis.md` | 杩愯鏃?脳 鎺ュ彛 | 鎺ュ彛瀹氫箟涓庨€傞厤鍣ㄥ疄鐜?|
| 4 | `04_editor_data_analysis.md` | 缂栬緫鍣?脳 Data | 缂栬緫鍣ㄧ姸鎬佷笌鏁版嵁鍖呰 |
| 5 | `05_editor_view_analysis.md` | 缂栬緫鍣?脳 View | 绐楀彛/瑙嗗浘/Inspector 缁樺埗 |
| 6 | `06_editor_logic_analysis.md` | 缂栬緫鍣?脳 Logic | 缂栬緫鍣ㄩ瑙堟挱鏀句笌澶勭悊鍣?|
| 7 | `07_track_clip_impl_analysis.md` | 璺ㄧ淮搴?| 鍚勮建閬?鐗囨鍏蜂綋瀹炵幇 |
| 8 | `08_architecture_dataflow_analysis.md` | 鏁翠綋 | 鏁翠綋鏋舵瀯涓庢暟鎹祦 |

---

## 姝ラ 1锛氳繍琛屾椂 Data 灞傚垎鏋?

**杈撳嚭鏂囦欢**: `docs/01_runtime_data_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Runtime/Data/ClipBase.cs` | 鐗囨鍩虹被 |
| `Runtime/Data/TrackBase.cs` | 杞ㄩ亾鍩虹被 |
| `Runtime/Data/Group.cs` | 鍒嗙粍鏁版嵁 |
| `Runtime/Data/SkillTimeline.cs` | 鏃堕棿绾挎牳蹇冩暟鎹?|
| `Runtime/Data/SkillEnums.cs` | 鏁版嵁灞傛灇涓?|
| `Runtime/Data/SkillTagConfig.cs` | 鏍囩閰嶇疆 |
| `Runtime/Data/ISkillClipData.cs` | 鐗囨鏁版嵁鎺ュ彛 |
| `Runtime/Data/Clips/*.cs` | 鎵€鏈?涓叿浣?Clip 瀹炵幇 |
| `Runtime/Data/Tracks/*.cs` | 鎵€鏈?涓叿浣?Track 瀹炵幇 |
| `Runtime/Enums/RuntimeEnums.cs` | 杩愯鏃舵灇涓惧畾涔?|
| `Runtime/Attributes/SkillAttributes.cs` | 鑷畾涔夌壒鎬у畾涔?|
| `Runtime/Serialization/SerializationUtility.cs` | 搴忓垪鍖栧伐鍏?|
| `Settings/SkillTagConfig.asset` | 鏍囩閰嶇疆璧勪骇 |

### 鍒嗘瀽瑕佺偣

1. **鏍稿績鏁版嵁鏋舵瀯**
   - `SkillTimeline` 濡備綍缁勭粐 Track 鍜?Group
   - `TrackBase` 涓?`ClipBase` 鐨勭户鎵垮眰娆′笌搴忓垪鍖栫瓥鐣?
   - `Group` 鐨勫垎缁勭鐞嗘満鍒?

2. **鍩虹被璁捐**
   - `ClipBase` 鐨勫瓧娈靛畾涔夛紙startTime, duration 绛夋牳蹇冨睘鎬э級
   - `TrackBase` 鐨勫瓧娈靛畾涔夛紙clips 闆嗗悎銆乼rackType/displayName 绛夛級
   - 缁ф壙鍏崇郴锛氬叿浣?Track/Clip 鈫?TrackBase/ClipBase

3. **ISkillClipData 鎺ュ彛**
   - 鎺ュ彛鑱岃矗涓庡疄鐜扮被

4. **鑷畾涔夌壒鎬?*
   - `SkillAttributes.cs` 涓畾涔夌殑 Attribute锛堝 TrackColor銆乀rackIcon 绛夛級
   - 鐗规€у湪缂栬緫鍣ㄤ晶鐨勬秷璐规柟寮?

5. **搴忓垪鍖栨満鍒?*
   - `SerializationUtility.cs` 鐨?JSON 搴忓垪鍖?鍙嶅簭鍒楀寲绛栫暐
   - 澶氭€佺被鍨嬪鐞嗘柟寮忥紙$type 瀛楁绛夛級
   - 缂栬緫鍣?鈫?杩愯鏃跺簭鍒楀寲涓€鑷存€?

6. **閰嶇疆绯荤粺**
   - `SkillTagConfig` 鐨勮璁★紙ScriptableObject vs JSON锛?
   - 鏍囩鍦ㄤ激瀹虫娴?鐢熸垚绯荤粺涓殑浣跨敤

7. **鏋氫妇瀹氫箟**
   - `SkillEnums.cs` 鍜?`RuntimeEnums.cs` 涓墍鏈夋灇涓剧殑璇箟

---

## 姝ラ 2锛氳繍琛屾椂 Logic 灞傚垎鏋?

**杈撳嚭鏂囦欢**: `docs/02_runtime_logic_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Runtime/Playback/Core/SkillRunner.cs` | 杩愯鏃舵挱鏀炬帶鍒跺櫒 |
| `Runtime/Playback/Core/ProcessContext.cs` | 澶勭悊鍣ㄤ笂涓嬫枃 |
| `Runtime/Playback/Core/ProcessFactory.cs` | 澶勭悊鍣ㄥ伐鍘?|
| `Runtime/Playback/Core/ProcessBase.cs` | 澶勭悊鍣ㄥ熀绫?|
| `Runtime/Playback/Core/IProcess.cs` | 澶勭悊鍣ㄦ帴鍙?|
| `Runtime/Playback/Core/ProcessBindingAttribute.cs` | 澶勭悊鍣ㄧ粦瀹氱壒鎬?|
| `Runtime/Playback/Lifecycle/SkillLifecycleManager.cs` | 鐢熷懡鍛ㄦ湡绠＄悊鍣?|
| `Runtime/Playback/VFXPoolManager.cs` | VFX 瀵硅薄姹犵鐞嗗櫒 |
| `Runtime/Playback/Processes/*.cs` | 鎵€鏈?涓繍琛屾椂澶勭悊鍣?|
| `Runtime/Sample/CharSkillActor.cs` | 绀轰緥 Actor 瀹炵幇 |

### 鍒嗘瀽瑕佺偣

1. **SkillRunner 鏍稿績娴佺▼**
   - 鍒濆鍖?鈫?鎾斁 鈫?Tick 鈫?鏆傚仠/鎭㈠ 鈫?缁撴潫鐨勫畬鏁寸敓鍛藉懆鏈?
   - 鏃堕棿鎺ㄨ繘涓庣墖娈垫縺娲?鍋滅敤閫昏緫
   - 澶氳建閬撳苟琛屽鐞嗘満鍒?

2. **Process 绯荤粺鏋舵瀯**
   - `IProcess` 鎺ュ彛瀹氫箟锛圗nter/Tick/Exit/Cleanup锛?
   - `ProcessBase` 鍩虹被鐨勯€氱敤瀹炵幇
   - `ProcessBindingAttribute` 濡備綍灏?Process 缁戝畾鍒?Clip 绫诲瀷
   - `ProcessFactory` 鐨勫弽灏勫彂鐜颁笌瀹炰緥鍖栨満鍒?

3. **ProcessContext 璁捐**
   - 涓婁笅鏂囦紶閫掍簡鍝簺淇℃伅锛圓ctor銆丼erviceFactory 绛夛級
   - 渚濊禆娉ㄥ叆妯″紡

4. **鐢熷懡鍛ㄦ湡绠＄悊**
   - `SkillLifecycleManager` 濡備綍绠＄悊澶氫釜 SkillRunner 鐨勭敓鍛藉懆鏈?
   - 鎶€鑳芥墦鏂?鎺掗槦/鍙犲姞绛栫暐

5. **VFX 瀵硅薄姹?*
   - `VFXPoolManager` 鐨勬睜鍖栫瓥鐣ワ紙棰勭儹/鍥炴敹/涓婇檺锛?
   - 缂栬緫鍣ㄩ瑙堜笌杩愯鏃跺叡浜繕鏄嫭绔?

6. **鍚勮繍琛屾椂澶勭悊鍣ㄨ瑙?*
   - `RuntimeAnimationProcess` 鈫?鍔ㄧ敾鎾斁
   - `RuntimeAudioProcess` 鈫?闊抽鎾斁
   - `RuntimeVFXProcess` 鈫?鐗规晥绠＄悊
   - `RuntimeDamageProcess` 鈫?浼ゅ妫€娴嬩笌鎵ц
   - `RuntimeSpawnProcess` 鈫?瀹炰綋鐢熸垚
   - `RuntimeEventProcess` 鈫?鑷畾涔変簨浠?
   - `CameraProcess` 鈫?闀滃ご鎺у埗
   - `MovementProcess` 鈫?瑙掕壊浣嶇Щ

7. **鏁版嵁娴?*
   - SkillTimeline 鈫?SkillRunner.Init 鈫?ProcessFactory 鈫?Process 瀹炰緥
   - 姣忓抚 Tick 涓?SkillRunner 鈫?鍚?Process 鐨勮皟搴︽祦绋?

---

## 姝ラ 3锛氳繍琛屾椂鎺ュ彛涓庨€傞厤鍣ㄥ垎鏋?

**杈撳嚭鏂囦欢**: `docs/03_runtime_interfaces_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Runtime/Playback/Interfaces/ISkillActor.cs` | 鎶€鑳芥墽琛岃€呮帴鍙?|
| `Runtime/Playback/Interfaces/ISkillAnimationHandler.cs` | 鍔ㄧ敾澶勭悊鎺ュ彛 |
| `Runtime/Playback/Interfaces/ISkillAudioHandler.cs` | 闊抽澶勭悊鎺ュ彛 |
| `Runtime/Playback/Interfaces/ISkillDamageHandler.cs` | 浼ゅ澶勭悊鎺ュ彛 |
| `Runtime/Playback/Interfaces/ISkillEventHandler.cs` | 浜嬩欢澶勭悊鎺ュ彛 |
| `Runtime/Playback/Interfaces/ISkillSpawnHandler.cs` | 鐢熸垚澶勭悊鎺ュ彛 |
| `Runtime/Playback/Interfaces/ISkillProjectile.cs` | 寮瑰皠鐗╂帴鍙?|
| `Runtime/Playback/Interfaces/IServiceFactory.cs` | 鏈嶅姟宸ュ巶鎺ュ彛 |
| `Assets/GameClient/Adapters/GameSkillAudioHandler.cs` | 闊抽閫傞厤鍣?|
| `Assets/GameClient/Adapters/SkillServiceFactory.cs` | 鏈嶅姟宸ュ巶閫傞厤鍣?|
| `Assets/GameClient/Adapters/SkillProjectile.cs` | 寮瑰皠鐗╅€傞厤鍣?|
| `Assets/GameClient/Adapters/SkillSpawnHandler.cs` | 鐢熸垚澶勭悊閫傞厤鍣?|
| `Runtime/Sample/CharSkillActor.cs` | 绀轰緥 Actor |

### 鍒嗘瀽瑕佺偣

1. **鎺ュ彛灞傝璁?*
   - 姣忎釜鎺ュ彛瀹氫箟鐨勬柟娉曠鍚嶄笌璇箟
   - 鎺ュ彛闂寸殑渚濊禆鍏崇郴锛堝 ISkillSpawnHandler 渚濊禆 ISkillProjectile锛?
   - DIP锛堜緷璧栧€掔疆鍘熷垯锛夌殑浣撶幇

2. **IServiceFactory 妯″紡**
   - 宸ュ巶鎺ュ彛濡備綍瀹炵幇鏈嶅姟瀹氫綅
   - 杩愯鏃朵緷璧栬В鏋愮瓥鐣?

3. **閫傞厤鍣ㄥ疄鐜?*
   - 姣忎釜 GameClient 閫傞厤鍣ㄥ浣曟ˉ鎺?SkillEditor 鎺ュ彛涓庢父鎴忛€昏緫
   - 閫傞厤鍣ㄤ腑鐨勫叿浣撳疄鐜扮粏鑺傦紙瀵硅薄姹犮€佺鎾炴娴嬬瓑锛?
   - ISP锛堟帴鍙ｉ殧绂诲師鍒欙級鐨勯伒瀹堢▼搴?

4. **鏁版嵁瀹夊叏妯″紡**
   - DamageData / SpawnData 绛夊€肩被鍨嬬粨鏋勪綋鐨勪娇鐢?
   - 闃叉澶栭儴淇敼鍐呴儴鐘舵€佺殑绛栫暐

---

## 姝ラ 4锛氱紪杈戝櫒 Data 灞傚垎鏋?

**杈撳嚭鏂囦欢**: `docs/04_editor_data_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Editor/Core/ATEditorState.cs` | 缂栬緫鍣ㄥ叏灞€鐘舵€?|
| `Editor/Core/ATEditorEvents.cs` | 缂栬緫鍣ㄤ簨浠剁郴缁?|
| `Editor/Core/TrackRegistry.cs` | 杞ㄩ亾绫诲瀷娉ㄥ唽琛?|
| `Editor/TrackObjectWrapper.cs` | 杞ㄩ亾瀵硅薄鍖呰鍣?|
| `Editor/Enums/EditorEnums.cs` | 缂栬緫鍣ㄦ灇涓?|
| `Editor/Language/ILanguages.cs` | 璇█鎺ュ彛 |
| `Editor/Language/Lan.cs` | 璇█绠＄悊鍣?|
| `Editor/Language/LanCHS.cs` | 涓枃璇█鍖?|
| `Editor/Language/LanEN.cs` | 鑻辨枃璇█鍖?|
| `Editor/Drawers/CustomDrawerAttribute.cs` | 鑷畾涔?Drawer 鐗规€?|

### 鍒嗘瀽瑕佺偣

1. **ATEditorState 鏍稿績鐘舵€?*
   - 缂栬緫鍣ㄥ叏灞€鐘舵€佺鐞嗭紙褰撳墠 Timeline銆侀€変腑瀵硅薄銆佹挱鏀剧姸鎬佺瓑锛?
   - 鐘舵€佺殑瀛樺彇鏂瑰紡锛堥潤鎬?vs 鍗曚緥 vs 瀹炰緥锛?
   - 鐘舵€佸彉鏇撮€氱煡鏈哄埗

2. **浜嬩欢绯荤粺**
   - `ATEditorEvents` 瀹氫箟浜嗗摢浜涗簨浠?
   - 浜嬩欢鐨勫彂甯?璁㈤槄妯″紡
   - 缂栬緫鍣ㄥ悇缁勪欢闂寸殑閫氫俊鏂瑰紡

3. **TrackRegistry 娉ㄥ唽琛?*
   - 杞ㄩ亾绫诲瀷鐨勬敞鍐屼笌鍙戠幇鏈哄埗
   - 鏄惁浣跨敤鍙嶅皠/鐗规€ц嚜鍔ㄦ敞鍐?
   - 娉ㄥ唽琛ㄥ浣曞叧鑱?Track 鈫?Drawer 鈫?Process

4. **TrackObjectWrapper**
   - SerializedObject 鍖呰鍣ㄧ殑璁捐
   - 缂栬緫鍣ㄤ晶瀵硅薄缂栬緫鐨勫皝瑁呯瓥鐣?
   - 涓?Unity SerializedProperty 鐨勪氦浜?

5. **澶氳瑷€绯荤粺**
   - `ILanguages` 鎺ュ彛涓庡疄鐜?
   - 璇█鍒囨崲鏈哄埗
   - 瀛楃涓查敭鍊肩鐞嗘柟寮?

6. **CustomDrawerAttribute**
   - Drawer 鍙戠幇涓庣粦瀹氭満鍒?
   - 涓?TrackRegistry 鐨勫叧绯?

---

## 姝ラ 5锛氱紪杈戝櫒 View 灞傚垎鏋?

**杈撳嚭鏂囦欢**: `docs/05_editor_view_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Editor/ATEditorWindow.cs` | 涓荤紪杈戝櫒绐楀彛 |
| `Editor/ATEditorSettingsWindow.cs` | 璁剧疆绐楀彛 |
| `Editor/Views/TimelineView.cs` | 鏃堕棿绾胯鍥撅紙鏍稿績锛?|
| `Editor/Views/TimelineClipInteraction.cs` | 鏃堕棿绾跨墖娈典氦浜?|
| `Editor/Views/TimelineClipOperations.cs` | 鏃堕棿绾跨墖娈垫搷浣?|
| `Editor/Views/TimelineCoordinates.cs` | 鏃堕棿绾垮潗鏍囩郴缁?|
| `Editor/Views/TrackListView.cs` | 杞ㄩ亾鍒楄〃瑙嗗浘 |
| `Editor/Views/ToolbarView.cs` | 宸ュ叿鏍忚鍥?|
| `Editor/Drawers/Base/SkillInspectorBase.cs` | Inspector 鍩虹被 |
| `Editor/Drawers/Base/ClipDrawer.cs` | 鐗囨 Drawer 鍩虹被 |
| `Editor/Drawers/Base/TrackDrawer.cs` | 杞ㄩ亾 Drawer 鍩虹被 |
| `Editor/Drawers/Impl/*.cs` | 鎵€鏈?涓叿浣?Drawer 瀹炵幇 |

### 鍒嗘瀽瑕佺偣

1. **ATEditorWindow 涓荤獥鍙?*
   - EditorWindow 鐨勭敓鍛藉懆鏈熺鐞?
   - 绐楀彛甯冨眬锛堝乏渚ц建閬撳垪琛?+ 鍙充晶鏃堕棿绾?+ 宸ュ叿鏍?+ Inspector锛?
   - OnGUI / OnEnable / OnDisable 鐨勫叧閿祦绋?
   - 鏁版嵁鍔犺浇/淇濆瓨娴佺▼

2. **TimelineView 鏃堕棿绾胯鍥?*
   - 鏃堕棿鍒诲害缁樺埗
   - 鐗囨鐨勫彲瑙嗗寲娓叉煋锛堜綅缃€侀鑹层€佹爣绛俱€侀€変腑鎬侊級
   - 鍒嗙粍鎶樺彔/灞曞紑鐨勮瑙夊鐞?
   - 婊氬姩涓庣缉鏀?

3. **TimelineClipInteraction 浜や簰绯荤粺**
   - 鐗囨鐨勯€夋嫨銆佹嫋鎷姐€佺缉鏀句氦浜?
   - 鍙抽敭鑿滃崟
   - 澶氶€変笌妗嗛€?

4. **TimelineClipOperations 鎿嶄綔绯荤粺**
   - 鐗囨鐨勬坊鍔?鍒犻櫎/澶嶅埗/绮樿创
   - Undo/Redo 鏀寔
   - 瀵归綈涓庡惛闄?

5. **TimelineCoordinates 鍧愭爣绯荤粺**
   - 鏃堕棿 鈫?鍍忕礌鍧愭爣鐨勪簰杞?
   - 缂╂斁绯绘暟绠＄悊
   - 鍙鍖哄煙璁＄畻

6. **TrackListView 杞ㄩ亾鍒楄〃**
   - 杞ㄩ亾/鍒嗙粍鐨勬爲褰㈠睍绀?
   - 鎷栨嫿鎺掑簭
   - 杞ㄩ亾娣诲姞/鍒犻櫎鐨?UI

7. **ToolbarView 宸ュ叿鏍?*
   - 鎾斁鎺у埗鎸夐挳
   - 鏃堕棿鏄剧ず/杈撳叆
   - 鏂囦欢鎿嶄綔锛堟柊寤?鎵撳紑/淇濆瓨锛?

8. **Inspector 缁樺埗**
   - `SkillInspectorBase` 鐨勫弽灏勯┍鍔ㄥ瓧娈电粯鍒?
   - `ClipDrawer` / `TrackDrawer` 鍩虹被鐨勬墿灞曠偣
   - 鍚勫叿浣?Drawer 鐨勭壒鍖栧瓧娈垫覆鏌?
   - SerializedObject 鐨勮嚜鍔ㄦ洿鏂版満鍒?

9. **璁剧疆绐楀彛**
   - `ATEditorSettingsWindow` 鎻愪緵鍝簺閰嶇疆椤?
   - 閰嶇疆鐨勬寔涔呭寲鏂瑰紡锛圗ditorPrefs / ScriptableObject锛?

---

## 姝ラ 6锛氱紪杈戝櫒 Logic 灞傚垎鏋?

**杈撳嚭鏂囦欢**: `docs/06_editor_logic_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

| 鏂囦欢 | 璇存槑 |
|:-----|:-----|
| `Editor/Playback/ATEditorWindow.Preview.cs` | 棰勮鎺у埗鍣紙partial class锛?|
| `Editor/Playback/EditorAudioManager.cs` | 缂栬緫鍣ㄩ煶棰戠鐞?|
| `Editor/Playback/EditorVFXManager.cs` | 缂栬緫鍣?VFX 绠＄悊 |
| `Editor/Playback/Processes/EditorAnimationProcess.cs` | 缂栬緫鍣ㄥ姩鐢诲鐞嗗櫒 |
| `Editor/Playback/Processes/EditorAudioProcess.cs` | 缂栬緫鍣ㄩ煶棰戝鐞嗗櫒 |
| `Editor/Playback/Processes/EditorDamageProcess.cs` | 缂栬緫鍣ㄤ激瀹冲鐞嗗櫒 |
| `Editor/Playback/Processes/EditorEventProcess.cs` | 缂栬緫鍣ㄤ簨浠跺鐞嗗櫒 |
| `Editor/Playback/Processes/EditorSpawnProcess.cs` | 缂栬緫鍣ㄧ敓鎴愬鐞嗗櫒 |
| `Editor/Playback/Processes/EditorVFXProcess.cs` | 缂栬緫鍣?VFX 澶勭悊鍣?|
| `Editor/TestLayerMaskJson.cs` | LayerMask 娴嬭瘯宸ュ叿 |

### 鍒嗘瀽瑕佺偣

1. **棰勮鎾斁绯荤粺**
   - `ATEditorWindow.Preview.cs` 濡備綍椹卞姩缂栬緫鍣ㄥ唴棰勮
   - 棰勮涓庤繍琛屾椂 SkillRunner 鐨勫叧绯伙紙澶嶇敤杩樻槸鐙珛锛?
   - EditorApplication.update 鐨勪娇鐢?
   - 棰勮鏃堕棿鐨勭簿纭帶鍒?

2. **缂栬緫鍣ㄤ笓鐢ㄧ鐞嗗櫒**
   - `EditorAudioManager` 鐨勯煶棰戦瑙堟満鍒讹紙AudioSource 绠＄悊锛?
   - `EditorVFXManager` 鐨?VFX 棰勮鏈哄埗锛堝疄渚嬪寲/閿€姣?浣嶇疆璺熻釜锛?

3. **缂栬緫鍣ㄥ鐞嗗櫒 vs 杩愯鏃跺鐞嗗櫒**
   - 姣忎釜缂栬緫鍣ㄥ鐞嗗櫒涓庡搴旇繍琛屾椂澶勭悊鍣ㄧ殑宸紓
   - 缂栬緫鍣ㄥ鐞嗗櫒濡備綍妯℃嫙杩愯鏃惰涓猴紙鏃?Physics 绛夐檺鍒讹級
   - Process 缁ф壙浣撶郴鍦ㄧ紪杈戝櫒/杩愯鏃剁殑缁熶竴鎬?

4. **鍏抽敭宸紓鍒嗘瀽**
   - 缂栬緫鍣ㄩ煶棰戯細浣跨敤 Unity Editor 闊抽 API
   - 缂栬緫鍣?VFX锛氱洿鎺ュ疄渚嬪寲 Prefab 鍒?SceneView
   - 缂栬緫鍣ㄥ姩鐢伙細浣跨敤 AnimationMode / SampleAnimation
   - 缂栬緫鍣ㄤ激瀹筹細浠呭彲瑙嗗寲 Gizmos锛堜笉鍋氬疄闄呬激瀹筹級
   - 缂栬緫鍣ㄧ敓鎴?浜嬩欢锛氶瑙堟爣璁版垨鏃ュ織

---

## 姝ラ 7锛氬悇杞ㄩ亾/鐗囨鍏蜂綋瀹炵幇鍒嗘瀽

**杈撳嚭鏂囦欢**: `docs/07_track_clip_impl_analysis.md`

### 闇€瑕侀槄璇荤殑鏂囦欢

鎵€鏈?`Runtime/Data/Clips/*.cs`銆乣Runtime/Data/Tracks/*.cs`銆佸搴旂殑 `Editor/Drawers/Impl/*.cs`銆?
瀵瑰簲鐨?`Editor/Playback/Processes/Editor*.cs`銆佸搴旂殑 `Runtime/Playback/Processes/Runtime*.cs`

### 鍒嗘瀽瑕佺偣

鎸夎建閬撶被鍨嬮€愪竴鍒嗘瀽锛屾瘡绉嶈建閬撳寘鍚互涓嬬淮搴︼細

#### 7.1 鍔ㄧ敾杞ㄩ亾 (Animation)
- **Clip 鏁版嵁**: `SkillAnimationClip` 鐨勫瓧娈碉紙animClip銆乻peed銆乵ask銆乫adeDuration 绛夛級
- **Track 鏁版嵁**: `AnimationTrack` 鐨勭壒鏈夐厤缃?
- **Drawer**: `AnimationClipDrawer` / `AnimationTrackDrawer` 鐨勮嚜瀹氫箟瀛楁缁樺埗
- **缂栬緫鍣ㄩ瑙?*: `EditorAnimationProcess` 濡備綍椹卞姩 AnimationMode
- **杩愯鏃舵墽琛?*: `RuntimeAnimationProcess` 濡備綍璋冪敤 ISkillAnimationHandler

#### 7.2 闊抽杞ㄩ亾 (Audio)
- **Clip 鏁版嵁**: `AudioClip(SkillEditor)` 鐨勫瓧娈?
- **Track 鏁版嵁**: `AudioTrack`
- **Drawer**: `AudioClipDrawer`
- **缂栬緫鍣ㄩ瑙?*: `EditorAudioProcess` 鈫?`EditorAudioManager`
- **杩愯鏃舵墽琛?*: `RuntimeAudioProcess` 鈫?`ISkillAudioHandler`

#### 7.3 VFX 杞ㄩ亾 (Visual Effects)
- **Clip 鏁版嵁**: `VFXClip` 鐨勫瓧娈碉紙prefab銆乷ffset/rotation/scale銆両SerializationCallbackReceiver锛?
- **Track 鏁版嵁**: `VFXTrack`
- **Drawer**: `VFXClipDrawer` / `VFXTrackDrawer`
- **缂栬緫鍣ㄩ瑙?*: `EditorVFXProcess` 鈫?`EditorVFXManager`
- **杩愯鏃舵墽琛?*: `RuntimeVFXProcess` 鈫?`VFXPoolManager`

#### 7.4 浼ゅ杞ㄩ亾 (Damage)
- **Clip 鏁版嵁**: `DamageClip` + `HitBoxShape` 鐨勭鎾炰綋瀹氫箟
- **Track 鏁版嵁**: `DamageTrack`
- **Drawer**: `DamageClipDrawer`锛堝鏉傜殑纰版挒浣撶紪杈戝櫒锛?
- **缂栬緫鍣ㄩ瑙?*: `EditorDamageProcess`锛圙izmos 缁樺埗锛?
- **杩愯鏃舵墽琛?*: `RuntimeDamageProcess` 鈫?`ISkillDamageHandler`

#### 7.5 鐢熸垚杞ㄩ亾 (Spawn)
- **Clip 鏁版嵁**: `SpawnClip` + `SpawnData`
- **Track 鏁版嵁**: `SpawnTrack`
- **Drawer**: `SpawnClipDrawer`
- **缂栬緫鍣ㄩ瑙?*: `EditorSpawnProcess`
- **杩愯鏃舵墽琛?*: `RuntimeSpawnProcess` 鈫?`ISkillSpawnHandler` 鈫?`ISkillProjectile`

#### 7.6 浜嬩欢杞ㄩ亾 (Event)
- **Clip 鏁版嵁**: `EventClip` 鐨勮嚜瀹氫箟浜嬩欢鍙傛暟
- **Track 鏁版嵁**: `EventTrack`
- **缂栬緫鍣ㄩ瑙?*: `EditorEventProcess`
- **杩愯鏃舵墽琛?*: `RuntimeEventProcess` 鈫?`ISkillEventHandler`

#### 7.7 鐩告満杞ㄩ亾 (Camera)
- **Clip 鏁版嵁**: `CameraClip`
- **Track 鏁版嵁**: `CameraTrack`
- **杩愯鏃舵墽琛?*: `CameraProcess`

#### 7.8 绉诲姩杞ㄩ亾 (Movement)
- **Clip 鏁版嵁**: `MovementClip`
- **Track 鏁版嵁**: `MovementTrack`
- **杩愯鏃舵墽琛?*: `MovementProcess`

---

## 姝ラ 8锛氭暣浣撴灦鏋勪笌鏁版嵁娴佸垎鏋?

**杈撳嚭鏂囦欢**: `docs/08_architecture_dataflow_analysis.md`

### 鍒嗘瀽鍐呭

鏈姤鍛婂熀浜庡墠 7 浠芥姤鍛婄殑鍙戠幇锛岃繘琛屾暣浣撴€ф€荤粨銆?

### 鍒嗘瀽瑕佺偣

1. **鏁翠綋鏋舵瀯鍥?*
   - 鍒嗗眰鏋舵瀯锛欴ata Layer 鈫?Logic Layer 鈫?View Layer
   - Editor 涓?Runtime 鐨勮竟鐣岀嚎
   - 渚濊禆鏂瑰悜锛圧untime 涓嶄緷璧?Editor锛?

2. **鏍稿績璁捐妯″紡**
   - 绛栫暐妯″紡锛圛Process / ProcessBase锛?
   - 宸ュ巶妯″紡锛圥rocessFactory / IServiceFactory锛?
   - 瑙傚療鑰呮ā寮忥紙ATEditorEvents锛?
   - 閫傞厤鍣ㄦā寮忥紙GameClient Adapters锛?
   - 瀵硅薄姹犳ā寮忥紙VFXPoolManager锛?
   - 鍛戒护/鎿嶄綔妯″紡锛圱imelineClipOperations锛?

3. **缂栬緫鏃舵暟鎹祦**
   ```
   JSON 鏂囦欢  鈫? SerializationUtility 鍙嶅簭鍒楀寲
              鈫? SkillTimeline (鍐呭瓨妯″瀷)
              鈫? ATEditorState (缂栬緫鍣ㄧ姸鎬?
              鈫? Views (GUI 娓叉煋)
              鈫? Drawers (Inspector 缁樺埗)
              鈫? 鐢ㄦ埛缂栬緫
              鈫? SkillTimeline 鏇存柊
              鈫? SerializationUtility 搴忓垪鍖?
              鈫? JSON 鏂囦欢
   ```

4. **缂栬緫鍣ㄩ瑙堟暟鎹祦**
   ```
   ATEditorWindow.Preview
     鈫?ATEditorState (鑾峰彇褰撳墠 Timeline)
     鈫?閬嶅巻鎵€鏈?Track/Clip
     鈫?閫氳繃 ProcessFactory 鎴栫洿鎺ヨ皟鐢?EditorProcess
     鈫?EditorProcess.Enter/Tick/Exit
       鈫?EditorAudioManager / EditorVFXManager
       鈫?AnimationMode.SampleAnimation (鍔ㄧ敾)
       鈫?Gizmos (浼ゅ鍙鍖?
   ```

5. **杩愯鏃舵暟鎹祦**
   ```
   SkillTimeline (浠?JSON 鍙嶅簭鍒楀寲)
     鈫?SkillRunner.Init(timeline, context)
       鈫?ProcessContext 鏋勫缓 (ISkillActor, IServiceFactory)
       鈫?ProcessFactory.CreateProcesses (鍙嶅皠鍙戠幇 + ProcessBindingAttribute)
       鈫?姣忓抚 SkillRunner.Tick(deltaTime)
         鈫?閬嶅巻婵€娲荤殑 Clip
         鈫?Process.Enter / Process.Tick / Process.Exit
           鈫?閫氳繃 Interface 璋冪敤閫傞厤鍣?(ISkillAnimationHandler 绛?
     鈫?SkillRunner.Stop / SkillLifecycleManager 鍥炴敹
   ```

6. **搴忓垪鍖栨暟鎹祦**
   ```
   缂栬緫鍣?SkillTimeline (C# 瀵硅薄)
     鈫?SerializationUtility.Serialize (Newtonsoft.Json + TypeNameHandling)
     鈫?JSON 鏂囦欢 (鍚?$type 澶氭€佺被鍨嬩俊鎭?
     鈫?SerializationUtility.Deserialize
     鈫?杩愯鏃?SkillTimeline (C# 瀵硅薄)
   ```

7. **鍚勫瓙绯荤粺鏁版嵁娴?*
   - 鍔ㄧ敾锛欰nimationClip 鈫?SkillAnimationClip 鈫?Process 鈫?AnimationHandler/AnimationMode
   - 闊抽锛欰udioClip 鈫?AudioClip(SkillEditor) 鈫?Process 鈫?AudioHandler/EditorAudioManager
   - VFX锛歅refab 鈫?VFXClip (ISerializationCallbackReceiver) 鈫?Process 鈫?VFXPoolManager/EditorVFXManager
   - 浼ゅ锛欻itBoxShape 鈫?DamageClip 鈫?DamageData 鈫?Process 鈫?DamageHandler/Gizmos
   - 鐢熸垚锛歋pawnClip 鈫?SpawnData 鈫?Process 鈫?SpawnHandler 鈫?Projectile
   - 浜嬩欢锛欵ventClip 鈫?Process 鈫?EventHandler
   - 鐩告満/绉诲姩锛欳lip 鈫?Process锛堟殏涓洪鏋跺疄鐜帮級

8. **鏋舵瀯浼樼己鐐硅瘎浼?*
   - SRP 閬靛畧绋嬪害
   - OCP 鎵╁睍鑳藉姏
   - DIP 渚濊禆鍊掔疆瀹炶返
   - ISP 鎺ュ彛闅旂鎯呭喌
   - 搴忓垪鍖栧畨鍏ㄦ€?
   - 鍙墿灞曟€т笌鍙淮鎶ゆ€?
   - 缂栬緫鍣?杩愯鏃朵唬鐮佸鐢ㄥ害
   - 娼滃湪鐨勬敼杩涘缓璁?

---

## 鎵ц瑙勫垯

1. **姣忔鐙珛**: 姣忎釜姝ラ鐢熸垚涓€浠界嫭绔嬬殑鍒嗘瀽鎶ュ憡锛岀‘淇濆嵆浣垮崟姝ユ墽琛屼篃鑳戒骇鍑哄畬鏁存枃妗?
2. **鍏堣鍚庡啓**: 姣忔寮€濮嬫椂鍏堢敤 `view_file` 闃呰鎵€鏈夌浉鍏虫枃浠讹紝鍒嗘瀽閫忓交鍚庡啀鎾板啓鎶ュ憡
3. **瀹㈣涓ヨ皑**: 涓嶅仛涓昏缇庡寲锛屽瀹炶褰曡璁′紭缂虹偣鍜屾綔鍦ㄩ棶棰?
4. **蹇界暐 docs**: 鍒嗘瀽杩囩▼涓拷鐣?`ATEditor/docs/` 涓凡鏈夌殑鏂囨。鍐呭
5. **浠ｇ爜寮曠敤**: 鍏抽敭璁捐鐐瑰繀椤婚檮甯﹀叿浣撶殑浠ｇ爜寮曠敤锛堟枃浠跺悕 + 琛屽彿/浠ｇ爜鐗囨锛?
6. **鍥捐〃杈呭姪**: 浣跨敤 Mermaid 鍥捐〃鍙鍖栨灦鏋勩€佺被鍥俱€佹暟鎹祦绛?
7. **涓枃鎾板啓**: 鎵€鏈夋姤鍛婁娇鐢ㄤ腑鏂?
8. **鍒嗘瀹屾垚**: 鍗曟瀵硅瘽鍙兘鏃犳硶瀹屾垚鎵€鏈夋楠わ紝鎸夋楠ゅ簭鍙烽『搴忔墽琛岋紝涓柇鍚庝粠涓婃鏈畬鎴愮殑姝ラ缁х画
