# SkillEditor 鎾斁閫昏緫鍏ㄩ摼璺璁℃姤鍛婏紙2026-03-04锛?
## 1. 鐩爣涓庤寖鍥?
鏈姤鍛婇拡瀵瑰綋鍓?`SkillEditor` 鐨勪笁鏉℃挱鏀鹃摼璺繘琛屽璁★細

- 缂栬緫鍣ㄦ甯搁瑙堟挱鏀撅紙Play/Pause/Stop锛?- 缂栬緫鍣ㄥ崟甯ч瑙堜笌 Seek锛堟爣灏烘嫋鍔ㄣ€佸墠鍚庡抚銆侀灏惧抚锛?- 杩愯鏃舵挱鏀撅紙`SkillRunner.Tick` 椹卞姩锛?
瀹¤閲嶇偣锛?
- 閫昏緫閾捐矾閿欒锛堣皟鐢ㄩ『搴忋€佺姸鎬佹満杈圭晫锛?- 璁捐缂洪櫡锛堣亴璐ｈ竟鐣屻€佺姸鎬佷竴鑷存€э級
- 瀛楁浣跨敤涓嶅綋锛堝瓧娈碘€滄湁瀹氫箟鏃犺涔夆€濇垨鈥滆涔変笌瀹炵幇涓嶄竴鑷粹€濓級
- 閫昏緫婕忔礊锛堟湭瑕嗙洊鍦烘櫙銆佽竟鐣屼笉闂幆锛?
---

## 2. 褰撳墠鎾斁閾捐矾姊崇悊

## 2.1 缂栬緫鍣ㄦ甯搁瑙堟挱鏀?
鍏ュ彛涓昏鍦細

- `ToolbarView.OnTogglePlay()` -> `ATEditorWindow.TogglePlay()`
- `ATEditorWindow.StartPreview()` 鍒涘缓 `ProcessContext` 骞?`previewRunner.Play(...)`
- `ATEditorWindow.Update()` -> `UpdatePreview()`

鍏抽敭鏃跺簭锛堢畝鍖栵級锛?
1. `StartPreview()`
2. 鎹曡幏棰勮鍒濆浣嶅Э锛坥rigin锛?3. 搴旂敤涓诲姩鐢昏建閬撳熀鍑嗕綅濮匡紙`AnimationTrack.offsetPos/offsetRot`锛?4. 鏋勫缓 `ProcessContext`
5. `SkillRunner.Play()`: `BuildProcesses` -> 鎵€鏈?`Process.OnEnable` -> `ExecuteStartActionsOnce`
6. 姣忓抚 `UpdatePreview()` 璋?`previewRunner.Tick(dt)`

---

## 2.2 缂栬緫鍣ㄥ崟甯ч瑙?/ Seek

鍏ュ彛涓昏鍦細

- `StepForward/StepBackward/JumpToStart/JumpToEnd/SeekPreview`

鍏抽敭鏃跺簭锛堢畝鍖栵級锛?
1. 鑻ユ鍦ㄦ挱鏀撅紝鍏?`TogglePlay()` 杞殏鍋?2. `EnsureRunnerActive()`锛圛dle 鏃朵細 `StartPreview` + `PausePreview`锛?3. `SeekWithPreviewTrackBase(targetTime, deltaTime)`
4. 鍏堥噸缃埌杞ㄩ亾鍩哄噯浣嶅Э锛屽啀 `previewRunner.Seek(...)`
5. 鏇存柊 `state.timeIndicator`

---

## 2.3 杩愯鏃舵挱鏀?
褰撳墠浠ｇ爜涓紝杩愯鏃朵富瑕佺敱澶栭儴鑴氭湰鐩存帴椹卞姩锛?
- `new SkillRunner(PlayMode.Runtime)`
- `runner.Play(timeline, context)`
- 澶栭儴 `Update` 寰幆鍐呮墜鍔?`runner.Tick(step)`锛堟祴璇曡剼鏈櫘閬嶆槸 30fps 鍥哄畾姝ワ級

`SkillLifecycleManager` 瀛樺湪锛屼絾褰撳墠浠撳簱瀹為檯璋冪敤鐐归潪甯稿急锛堝熀鏈湭褰㈡垚缁熶竴鎺ュ叆锛夈€?
---

## 3. 闂鎬昏锛堟寜涓ラ噸搴︼級

| 缂栧彿 | 涓ラ噸搴?| 绫诲瀷 | 鏍稿績缁撹 |
|---|---|---|---|
| P0-01 | 涓ラ噸 | 閾捐矾閿欒 | 缂栬緫鍣ㄥ瓨鍦ㄢ€滃弻鏃堕挓椹卞姩鈥濓紝`state.timeIndicator` 琚袱濂楅€昏緫鍚屾椂鎺ㄨ繘銆?|
| P0-02 | 涓ラ噸 | 鐘舵€佹満杈圭晫 | `Tick` 涓?`Seek` 鐨勫尯闂村垽瀹氫笉涓€鑷达細涓€涓?`(start, end]`锛屼竴涓?`[start, end)`銆?|
| P0-03 | 涓ラ噸 | 瀛楁/鍙傛暟婊ョ敤 | `Seek` 鐨?`deltaTime` 澶嶇敤 `SnapInterval`锛屽彉閲忔闀挎ā寮忎笅浼氫紶 `-1`銆?|
| P0-04 | 涓ラ噸 | 閫昏緫婕忔礊 | 鍚岃建鐗囨涓嶆寜 `startTime` 鎺掑簭鎵ц锛岄噸鍙犱笌鍚屽抚杩涘叆椤哄簭渚濊禆鍘嗗彶缂栬緫椤哄簭銆?|
| P0-05 | 涓ラ噸 | 璁捐缂洪櫡 | 鍔ㄧ敾鐘舵€佹寜 `AnimationClip` 璧勬簮璇嗗埆锛屽鑷粹€滃悓璧勬簮澶氱墖娈碘€濆叡浜姸鎬併€?|
| P1-01 | 楂?| 璁捐缂洪櫡 | `isMasterTrack` 绾︽潫鍙湪 Inspector 缁樺埗鏃朵慨姝ｏ紝涓嶆槸鏁版嵁灞傚己绾︽潫銆?|
| P1-02 | 楂?| 瀛楁浣跨敤涓嶅綋 | `Group.isEnabled`銆乣TrackBase.isMuted` 鏈繘鍏?`BuildProcesses` 鎾斁杩囨护銆?|
| P1-03 | 楂?| 璁捐缂洪櫡 | Pause 浠呭喕缁?Runner锛屼笉瀵硅繍琛屼腑闊抽/杩愯鏃跺姩鐢诲舰鎴愮粺涓€鏆傚仠闂幆銆?|
| P1-04 | 楂?| 鍔熻兘闂幆缂哄け | 鍙嶅悜鎾斁鏈棴鐜細缁撴潫鏉′欢銆佸惊鐜噸缃€佸瓙绯荤粺閫熷害璇箟鍧囦笉瀹屾暣銆?|
| P2-01 | 涓?| 琛屼负涓嶄竴鑷?| 棰勮閫熷害鍊嶇巼鍦?`Variable` 妯″紡鏈敓鏁堛€?|
| P2-02 | 涓?| 瀛楁澶辨晥 | `SkillAnimationClip.positionOffset/rotationOffset/useMatchOffset` 鍐欏叆浣嗘挱鏀鹃摼璺笉娑堣垂銆?|
| P2-03 | 涓?| 浠ｇ爜鍗敓 | `previewSeekDirty` 鍙啓涓嶈锛沗prevoewTarget` 鎷煎啓閿欒鏆撮湶鍒板閮ㄨ皟鐢ㄣ€?|
| P2-04 | 涓?| 鎺ュ叆缂哄彛 | 杩愯鏃剁己灏戠粺涓€ Runner 鎵樼鑼冨紡锛坄SkillLifecycleManager` 涓庢墜鍔?Tick 骞跺瓨锛夈€?|

---

## 4. 閲嶇偣闂涓庨敊璇摼璺?
## P0-01 鍙屾椂閽熼┍鍔紙缂栬緫鍣級

瀹氫綅锛?
- `Editor/ATEditorWindow.cs:225-253`锛堟墜鍔ㄦ帹杩?`state.timeIndicator`锛?- `Editor/ATEditorWindow.cs:259`锛堥殢鍚庤皟鐢?`UpdatePreview()`锛?- `Editor/Playback/ATEditorWindow.Preview.cs:375`锛堝啀娆＄敤 `previewRunner.CurrentTime` 鍥炲啓锛?
闂閾捐矾锛?
1. `Update()` 鍏堟寜 `lastFrameTime` 澧為噺鎺ㄨ繘 `state.timeIndicator`
2. 鍚屼竴甯?`UpdatePreview()` 鍐嶆寜 Runner 缁撴灉鍥炲啓
3. 涓ゅ缁撴潫鍒ゆ柇骞跺瓨锛坄Update()` 涓?`UpdatePreview()` 鍚勬湁缁撴潫閫昏緫锛?
褰卞搷锛?
- 鏃堕棿鎸囩ず鍣ㄤ笌鐪熷疄 Runner 鏃堕棿鍙兘鐭殏鍒嗗弶
- 鍦ㄥ彉閫熸垨鍥哄畾姝ユā寮忎笅锛屽瓨鍦ㄦ彁鍓?寤跺悗瑙﹀彂鍋滄鐨勯闄?- 鍚庣画瀹氫綅鈥滆繃娓℃椂闀垮紓甯糕€濇椂浼氳鐘舵€佸櫔澹板共鎵?
---

## P0-02 Tick/Seek 鍖洪棿杈圭晫涓嶄竴鑷?
瀹氫綅锛?
- `Runtime/Playback/Core/SkillRunner.cs:186-187`锛圫eek锛歚target >= start && target < end`锛?- `Runtime/Playback/Core/SkillRunner.cs:245-246`锛圱ick锛歚time > start && time <= end`锛?
闂閾捐矾锛?
- Tick 姝ｆ斁杩涘叆鐗囨鏃朵細鈥滄櫄涓€甯р€濊Е鍙?`OnEnter`锛堝洜涓?`>`锛?- Seek 鍒板悓涓€鏃堕棿鐐瑰嵈浼氣€滅珛鍗虫縺娲烩€濈墖娈碉紙鍥犱负 `>=`锛?- 涓よ€呭湪杈圭晫甯ц涓轰笉涓€鑷达紝瀵艰嚧鈥滃悓涓€鏃跺埢锛屾挱鏀句笌鍗曞抚棰勮琛ㄧ幇涓嶄竴鑷粹€?
褰卞搷锛?
- 杩囨浮鍖烘湁鏁堟椂闀跨缉鐭紙甯歌 1 甯у亸宸紝30fps 涓嬬害 33ms锛?- 杩涘叆/閫€鍑轰簨浠跺湪 Tick 涓?Seek 闂存棤娉曞榻?
---

## P0-03 Seek 鐨?deltaTime 璇箟閿欒

瀹氫綅锛?
- `Editor/Core/ATEditorState.cs:125-134`锛坄SnapInterval` 鍙橀噺妯″紡杩斿洖 `-1`锛?- `Editor/Playback/ATEditorWindow.Preview.cs` 澶氬 `SeekWithPreviewTrackBase(..., state.SnapInterval)`锛堝 `266/285/298/312/328`锛?- `Runtime/Playback/Core/SkillRunner.cs:219,223`锛坄deltaTime` 浼犲叆 `OnUpdate` 涓?TickAction锛?- `Editor/Playback/Processes/EditorAnimationProcess.cs:28-31`锛圱ickAction 鐢?`deltaTime` 鍋?`ManualUpdate`锛?
闂閾捐矾锛?
1. 鍙橀噺姝ラ暱妯″紡 Seek 浼犲叆 `-1`
2. `EditorAnimationProcess` 灏嗗叾浣滀负 `ManualUpdate(-1)` 浣跨敤
3. 搴曞眰娣峰悎杩涘害鎸夎礋澧為噺鏇存柊锛屾潈閲?杩囨浮鍑虹幇鍙嶅悜鎴栫獊鍙?
褰卞搷锛?
- 鍗曞抚棰勮銆丼eek 杩涘叆杩囨浮鍖烘椂鐨勬潈閲嶅紓甯?- 鍥為€€甯ц涓轰笉鍙€嗭紙鈥滃姩浣滃洖閫€浣嗘潈閲嶄笉鍥為€€/鍙嶅悜閿欎贡鈥濓級

---

## P0-04 鍚岃建鐗囨鎵ц椤哄簭涓嶇ǔ瀹氾紙鏈寜鏃堕棿鎺掑簭锛?
瀹氫綅锛?
- `SkillRunner.BuildProcesses()` 鐩存帴閬嶅巻 `track.clips`锛歚Runtime/Playback/Core/SkillRunner.cs:333`
- 鎷栨嫿鍙敼 `clip.startTime` 涓嶉噸鎺掑垪琛細`Editor/Views/TimelineClipInteraction.cs:439`

闂閾捐矾锛?
- 褰撳悓杞ㄥ涓墖娈甸噸鍙犳垨鍚屽抚杩涘叆鏃讹紝`OnEnter/OnUpdate` 椤哄簭鍙栧喅浜庡垪琛ㄩ『搴?- 鍒楄〃椤哄簭鍙堝彇鍐充簬鍘嗗彶缂栬緫鍔ㄤ綔锛岃€岄潪鏃堕棿椤哄簭

褰卞搷锛?
- 鍚屼竴鏁版嵁鍦ㄤ笉鍚岀紪杈戣繃绋嬩笅鍙兘浜х敓涓嶅悓鎾斁缁撴灉
- 杩囨浮鐩爣鈥滄渶鍚庝竴娆?Play 瑕嗙洊鍓嶄竴娆?Play鈥濓紝琛屼负涓嶇‘瀹?
---

## P0-05 鍚岃祫婧愬鐗囨鍏变韩鍚屼竴 AnimState

瀹氫綅锛?
- `AnimLayer.GetState(AnimationClip)`锛歚MAnimSystem/AnimLayer.cs:351-371`
- `AnimLayer.Play(...)` 瀵瑰悓涓€鐩爣鐘舵€佺洿鎺ヨ繑鍥烇細`MAnimSystem/AnimLayer.cs:251-257`
- `AnimComponent.Evaluate(...)` 涔熼€氳繃 `GetState(clip)`锛歚MAnimSystem/AnimComponent.cs:343`

闂閾捐矾锛?
- 鐗囨韬唤鏄?`SkillAnimationClip`锛堟椂闂存锛夛紝浣嗗姩鐢荤姸鎬佽韩浠藉嵈鏄?`AnimationClip` 璧勬簮
- 鍚屼竴璧勬簮鍦ㄥ悓灞傚涓墖娈典細浜夌敤涓€涓姸鎬侊細
  - 鍚庣墖娈靛彲鑳芥棤娉曡Е鍙戠嫭绔嬮噸鎾?鐙珛杩囨浮
  - 涓嶅悓鐗囨 OnUpdate 浼氳鐩栧悓涓€鐘舵€佹椂闂?
褰卞搷锛?
- 鍑虹幇鈥滅湅璧锋潵鍍忛珮棰戣嚜鍒囨崲/閲嶅叆鈥濈殑寮傚父浣撴劅
- 鍚岃祫婧愬垏娈电紪鎺掑け鐪燂紙灏ゅ叾鏄噸鍙犲拰寰幆锛?
---

## P1-01 `isMasterTrack` 涓嶆槸鏁版嵁灞傚己绾︽潫

瀹氫綅锛?
- `AnimationTrack.CanPlay => isMasterTrack`锛歚Runtime/Data/Tracks/AnimationTrack.cs:24`
- `BuildProcesses` 渚濊禆 `track.CanPlay`锛歚Runtime/Playback/Core/SkillRunner.cs:331`
- 绾︽潫閫昏緫浠呭湪 `AnimationTrackDrawer.DrawInspector` 鍐呰皟鐢細`Editor/Drawers/Impl/AnimationTrackDrawer.cs:21-23,108+`

闂锛?
- 鑻?JSON 瀵煎叆鍚庢湭鎵撳紑璇ヨ建閬?Inspector锛岄潪娉曠姸鎬侊紙鍏?false/澶?true锛変笉浼氳嚜鍔ㄤ慨姝?
褰卞搷锛?
- 鍙兘鍑虹幇鈥滄湁鍔ㄧ敾杞ㄩ亾浣嗗畬鍏ㄤ笉鎾€濇垨鈥滀富杞ㄤ笉纭畾鈥濈殑闂

---

## P1-02 `Group.isEnabled` / `Track.isMuted` 涓庢挱鏀捐劚鑺?
瀹氫綅锛?
- 瀛楁瀹氫箟锛歚Runtime/Data/Group.cs:24`锛宍Runtime/Data/TrackBase.cs:27`
- 鎾斁杩囨护浠呮鏌?`track.isEnabled && track.CanPlay`锛歚Runtime/Playback/Core/SkillRunner.cs:331`

闂锛?
- UI 涓婂彲鎿嶄綔鐨勫紑鍏冲苟鏈繘鍏ユ挱鏀惧垽瀹?
褰卞搷锛?
- 缂栬緫鍣ㄧ湅鍒扳€滅鐢ㄥ垎缁?闈欓煶杞ㄩ亾鈥濓紝瀹為檯浠嶅彲鎾斁锛堝彇鍐充簬 track.isEnabled锛?
---

## P1-03 Pause 璇箟涓嶅畬鏁?
瀹氫綅锛?
- `SkillRunner.Pause` 浠呮敼鐘舵€侊細`Runtime/Playback/Core/SkillRunner.cs:148-153`
- 杩愯鏃跺姩鐢讳粛鐢?`AnimComponent.Update` 椹卞姩锛歚MAnimSystem/AnimComponent.cs:76-84,99-112`
- 棰勮闊抽鏆傚仠渚濊禆 `GlobalPlaySpeed==0`锛歚Editor/Playback/Processes/EditorAudioProcess.cs:87-90`

闂锛?
- Pause 娌℃湁缁熶竴涓嬪彂鍒扳€滄寔缁嚜琛屾洿鏂扳€濈殑瀛愮郴缁燂紙闊抽銆佽繍琛屾椂鍔ㄧ敾锛?
褰卞搷锛?
- 鏃堕棿杞存殏鍋滃悗锛屽眬閮ㄥ瓙绯荤粺鍙兘缁х画鎺ㄨ繘

---

## P1-04 鍙嶅悜鎾斁闂幆缂哄け

瀹氫綅锛?
- `CurrentTime += deltaTime * speed`锛歚SkillRunner.cs:239`
- 缁撴潫鍒ゅ畾浠?`>= Duration`锛歚SkillRunner.cs:285`
- loop 閲嶇疆鍥哄畾鍥?`0`锛歚SkillRunner.cs:290`
- `RuntimeVFXProcess.SyncSpeed` 璐熼€熺洿鎺?return锛歚Runtime/Playback/Processes/RuntimeVFXProcess.cs:112`

闂锛?
- 鍙嶅悜鎾斁娌℃湁鈥滃埌 0 鐨勭粓姝?寰幆鈥濊涔?- 瀛愮郴缁熷璐熼€熷鐞嗕笉缁熶竴

褰卞搷锛?
- 鍙嶅悜鎾椂琛屼负涓嶅彲棰勬祴锛堟棤娉曚繚璇?Enter/Exit/Loop 瀹屾暣闂幆锛?
---

## P2-01 棰勮閫熷害鍊嶇巼鍦?Variable 妯″紡澶辨晥

瀹氫綅锛?
- 鍥哄畾姝ュ垎鏀娇鐢?`previewSpeedMultiplier`锛歚ATEditorWindow.Preview.cs:351`
- 鍙橀噺姝ュ垎鏀湭浣跨敤锛歚ATEditorWindow.Preview.cs:368`

褰卞搷锛?
- 鍚屼竴涓€熷害璁剧疆鍦ㄤ笉鍚屾杩涙ā寮忎笅琛屼负涓嶄竴鑷?
---

## P2-02 鍔ㄧ敾 Offset 鍖归厤瀛楁鏈繘鍏ユ挱鏀鹃摼璺?
瀹氫綅锛?
- 瀛楁瀹氫箟锛歚SkillAnimationClip.positionOffset/rotationOffset/useMatchOffset`锛坄Runtime/Data/Clips/SkillAnimationClip.cs:24-30`锛?- 鍖归厤宸ュ叿鍐欏叆杩欎簺瀛楁锛歚Editor/Utils/AnimationMatchUtility.cs:37-43`
- 鍔ㄧ敾 Process 鎾斁/閲囨牱涓嶆秷璐逛笂杩板瓧娈碉細`EditorAnimationProcess.cs`銆乣RuntimeAnimationProcess.cs`

褰卞搷锛?
- 鈥滃尮閰嶄笂涓€涓墖娈?offset鈥濇暟鎹眰鏈夊€硷紝浣嗘挱鏀惧眰鏃犳晥鏋?- 鍔熻兘琛ㄧ幇涓庣敤鎴疯鐭ヤ笉涓€鑷?
---

## P2-03 浠ｇ爜鍗敓闂锛氭瀛楁涓庢嫾鍐欓敊璇?
瀹氫綅锛?
- `previewSeekDirty` 浠呭啓鍏ユ湭璇诲彇锛歚Editor/Core/ATEditorState.cs:146` + 澶氬鍐欏叆
- `prevoewTarget` 鎷煎啓閿欒骞惰澶栭儴浣跨敤锛歚Editor/Playback/ATEditorWindow.Preview.cs:14`銆乣Editor/Drawers/Impl/AnimationClipDrawer.cs:61,70`

褰卞搷锛?
- 澧炲姞缁存姢鎴愭湰涓庤鐢ㄩ闄?
---

## P2-04 杩愯鏃?Runner 鎵樼鏂瑰紡鏈粺涓€

鐜扮姸锛?
- 鏃㈡湁 `SkillLifecycleManager`锛堢粺涓€ Update Tick锛夛紝涔熸湁澶栭儴鎵嬪姩 `runner.Tick(step)` 鏂瑰紡
- 褰撳墠浠撳簱涓讳娇鐢ㄥ満鏅互鎵嬪姩 Tick 娴嬭瘯鑴氭湰涓轰富

褰卞搷锛?
- 鎺ュ叆灞傛槗鍑虹幇鈥滆皝璐熻矗椹卞姩銆佽皝璐熻矗鍙嶆敞鍐屸€濈殑鑱岃矗涓嶆竻

---

## 5. 瀛楁浣跨敤瀹¤缁撹

### 5.1 浣跨敤涓嶅緱褰撴垨璇箟鏈惤鍦?
- `previewSeekDirty`锛氬彧鍐欎笉璇伙紝寤鸿绉婚櫎鎴栬ˉ榻愭秷璐归摼璺€?- `SkillAnimationClip.useMatchOffset`锛氫粎浣滀负鏍囪瀛樺偍锛屾棤杩愯鏃?棰勮璇箟銆?- `SkillAnimationClip.positionOffset/rotationOffset`锛氬姩鐢绘挱鏀鹃摼璺湭浣跨敤銆?- `Group.isEnabled`銆乣TrackBase.isMuted`锛氫笌 `BuildProcesses` 杩囨护鏉′欢鑴辫妭銆?
### 5.2 璇箟寮辩害鏉熷瓧娈?
- `AnimationTrack.isMasterTrack`锛氫緷璧?Inspector 缁樺埗鏃剁煫姝ｏ紝涓嶆槸鏁版嵁妯″瀷灞傜害鏉熴€?
---

## 6. 椋庨櫓浼樺厛绾у缓璁?
寤鸿鎸変互涓嬮『搴忓鐞嗭細

1. 鍏堜慨 P0-01 / P0-02 / P0-03 / P0-04 / P0-05锛堢洿鎺ュ奖鍝嶁€滆繃娓℃椂闀裤€佹潈閲嶃€佺ǔ瀹氭€р€濓級
2. 鍐嶄慨 P1-01 / P1-02 / P1-03 / P1-04锛堟彁鍗囩姸鎬佹満闂幆鍜屼竴鑷存€э級
3. 鏈€鍚庢竻鐞?P2锛堝瓧娈?鎺ュ彛鍗敓銆佸彲缁存姢鎬э級

---

## 7. 缁撹

褰撳墠鎾斁绯荤粺鈥滀富骞插彲璺戔€濓紝浣嗗湪杈圭晫涓€鑷存€с€佺姸鎬佹満闂幆銆佸瓧娈佃涔夎惤鍦颁笂瀛樺湪绯荤粺鎬ч闄┿€傛渶鍏抽敭鐨勬牴鍥犱笉鏄崟鐐?bug锛岃€屾槸浠ヤ笅涓夌被鍙犲姞锛?
- 鏃堕棿涓庣姸鎬佸垽瀹氬瓨鍦ㄥ婧愶紙鍙屾椂閽?+ 杈圭晫涓嶄竴鑷达級
- 鐗囨韬唤涓庡姩鐢荤姸鎬佽韩浠戒笉涓€鑷达紙鍚岃祫婧愮墖娈靛叡浜姸鎬侊級
- 缂栬緫鍣ㄩ瑙堝弬鏁拌涔夊鐢ㄤ笉褰擄紙`SnapInterval` 鍏间綔 `deltaTime`锛?
鑻ヤ笉鍏堢粺涓€杩欎簺鍩虹璇箟锛屽悗缁湪鈥滆繃娓℃椂闀垮噯纭€с€丼eek 鍙€嗘€с€佸弽鍚戞挱鏀俱€佸亸绉诲尮閰嶁€濅笂鐨勯棶棰樹細鍙嶅鍑虹幇銆?
