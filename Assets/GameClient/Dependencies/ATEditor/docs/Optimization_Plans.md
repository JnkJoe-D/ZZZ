# 鎶€鑳界紪杈戝櫒浼樺寲鏂瑰悜涓庢柟妗?

鍩轰簬褰撳墠鏋舵瀯鍜屼唬鐮佸垎鏋愶紝浠ヤ笅鏄拡瀵规妧鑳界紪杈戝櫒鐨勬綔鍦ㄤ紭鍖栨柟鍚戙€?

## 1. 绮惧害涓庢椂闂存帶鍒朵紭鍖?(Precision & Time Control)

**鐩爣**: 鍦ㄤ繚鎸佺伒娲绘€х殑鍚屾椂锛屾敮鎸佷弗鏍肩殑娓告垙甯у悓姝ラ渶姹傦紙濡傛牸鏂?鍔ㄤ綔娓告垙锛夈€?

### 鏂规 A: 寮曞叆鈥滃抚鍚搁檮妯″紡鈥?(Frame Snapping Mode)
*   **鎻忚堪**: 鍦ㄥ伐鍏锋爮澧炲姞涓€涓笅鎷夎彍鍗?[ 鑷敱妯″紡 | 30 FPS | 60 FPS ]銆?
*   **瀹炵幇**:
    *   鍦?`ATEditorState` 涓鍔?`float frameRate` 鍜?`bool useFrameSnap`銆?
    *   淇敼 `TimelineView.SnapTime` 鏂规硶锛氬綋寮€鍚抚鍚搁檮鏃讹紝鏃犺鍔ㄦ€佺綉鏍硷紝寮哄埗 `return Mathf.Round(time * frameRate) / frameRate;`銆?
    *   淇敼娓叉煋閫昏緫锛屽湪鏍囧昂涓婄粯鍒跺浐瀹氱殑甯у埢搴︾嚎锛團rame 0, 1, 2...锛夎€岄潪绉掓暟銆?

### 鏂规 B: 閫昏緫灞傛敼鐢ㄥ抚鏁板瓨鍌?(Integer Frames)
*   **鎻忚堪**: 灏嗘暟鎹眰鐨?`float startTime` 鏀逛负 `int startFrame`銆?
*   **浼樼偣**: 褰诲簳鏍归櫎娴偣璇樊锛屼繚璇佺粷瀵圭‘瀹氭€с€?
*   **浠ｄ环**: 閲嶆瀯鎴愭湰楂橈紝闇€瑕佸ぇ閲忎慨鏀?`ClipBase` 鍙婃墍鏈夊簭鍒楀寲鏁版嵁銆?
*   **寤鸿**: 闄ら潪鏄柊绔嬮」鐨勭‖鏍告牸鏂楁父鎴忥紝鍚﹀垯涓嶆帹鑽愰噸鏋勭幇鏈夐」鐩紝寤鸿閲囩敤 **鏂规 A**锛圲I灞傞檺鍒讹級銆?

---

## 2. 浜や簰浣撻獙浼樺寲 (UX Improvements)

**鐩爣**: 鎻愬崌鎿嶄綔鎵嬫劅锛屽噺灏戣鎿嶄綔锛屾彁楂樺埗浣滄晥鐜囥€?

### 2.1 缂╂斁浣撻獙浼樺寲
*   **褰撳墠**: 缂╂斁涓績浼间箮鏄浐瀹氱殑鎴栬窡闅忓乏渚с€?
*   **浼樺寲**: 
    1.  **榧犳爣涓績缂╂斁**: `Ctrl + 婊氳疆` 鏃讹紝淇濇寔榧犳爣鎸囧悜鐨勬椂闂寸偣鍦ㄥ睆骞曚笂鐨勪綅缃笉鍙樸€?
    2.  **蹇嵎閿?*: 寮曞叆 `F` 閿?(Focus)锛岃嚜鍔ㄧ缉鏀捐鍥句互瀹圭撼褰撳墠閫変腑鐨勭墖娈垫垨鏁翠釜 Timeline銆?

### 2.2 妗嗛€変紭鍖?
*   **褰撳墠**: 涔熷氨鏄熀纭€鐨勭偣閫夈€?
*   **浼樺寲**:
    *   **妗嗛€?(Marquee Selection)**: 瀹炵幇榧犳爣鎷栨嫿鐢绘锛屾壒閲忛€変腑澶氫釜鐗囨銆?
    *   **澶氶€夋嫋鎷?*: 鍏佽鍚屾椂鎷栧姩澶氫釜閫変腑鐨勭墖娈碉紝骞朵繚鎸佸畠浠箣闂寸殑鐩稿浣嶇疆锛堝凡鍦ㄤ唬鐮佷腑鐪嬭 `DragMode.MoveClip` 瀵?`draggingClip` 鐨勫鐞嗭紝闇€纭鏄惁鏀寔 `state.selectedClips` 鐨勬壒閲忔洿鏂帮級銆?

### 2.3 纾佸惛鍔熻兘澧炲己
*   **浼樺寲**: 澧炲姞纾佸惛寮€鍏虫寜閽€?
    *   **Magnet Toggle**: 鍏佽鐢ㄦ埛鏆傛椂鍏抽棴鑷姩鍚搁檮锛堟寜浣?Ctrl 涓存椂鍏抽棴锛夈€?
    *   **Snap Target**: 鍏佽鐢ㄦ埛鐙珛鍕鹃€夆€滃惛闄勫埌鐗囨鈥濄€佲€滃惛闄勫埌鍏夋爣鈥濄€佲€滃惛闄勫埌缃戞牸鈥濄€?

---

## 3. 鎬ц兘浼樺寲 (Performance)

**鐩爣**: 褰?Timeline 鍖呭惈鏁扮櫨涓墖娈垫垨鍑犲崄鏉¤建閬撴椂淇濇寔娴佺晠銆?

### 3.1 娓叉煋鍓旈櫎 (UI Culling)
*   **鐜扮姸**: `TimelineView` 鐩墠铏界劧鏈夌畝鍗曠殑 `continue` 鍒ゆ柇锛屼絾閬嶅巻閫昏緫鍙兘鍦ㄥぇ閲?Clip 鏃朵粛鏈夊紑閿€銆?
*   **浼樺寲**: 
    *   **瑙嗛敟鍓旈櫎**: 浠呰绠楀拰缁樺埗浣嶄簬 `scrollOffset` 鍜?`scrollOffset + viewWidth` 涔嬮棿鐨勭墖娈点€?
    *   **杞ㄩ亾鍓旈櫎**: 浠呯粯鍒?Y 杞村湪灞忓箷鍙鑼冨洿鍐呯殑杞ㄩ亾銆?

### 3.2 缁樺埗浼樺寲
*   **浼樺寲**: 鍑忓皯 `GUI.Label` 鍜?`EditorGUI.DrawRect` 鐨勮皟鐢ㄣ€傚浜庡瘑闆嗙殑鍒诲害绾匡紝鍙互鑰冭檻浣跨敤 `GL` 搴曞眰缁樺埗鎴栦竴寮犲钩閾虹殑 Texture锛屽ぇ骞呴檷浣?DrawCall (Editor GUI 涔熸槸鏈夊紑閿€鐨?銆?

---

## 4. 鏋舵瀯涓庣ǔ瀹氭€?(Architecture & Reliability)

**鐩爣**: 鎻愰珮绯荤粺鐨勫仴澹€э紝闃叉鏁版嵁鎹熷潖銆?

### 4.1 鎾ら攢绯荤粺 (Undo/Redo) 鍔犲浐
*   **鐜扮姸**: 渚濊禆 `Undo.RegisterCompleteObjectUndo`銆?
*   **浼樺寲**: 瀵逛簬鎷栨嫿绛夎繛缁搷浣滐紝搴斾娇鐢?`Undo.RecordObject` 骞堕厤鍚?`Undo.CollapseUndoOperations` (Group)锛岄伩鍏嶆瘡绉诲姩涓€鍍忕礌浜х敓涓€涓挙閿€姝ャ€?

### 4.2 鑴忔爣璁扮鐞?(Dirty Flag)
*   **浼樺寲**: 纭繚鎵€鏈変慨鏀规搷浣滐紙鍖呮嫭鍙抽敭鑿滃崟鍒犻櫎銆佸揩鎹烽敭澶嶅埗绮樿创锛夐兘姝ｇ‘璋冪敤 `EditorUtility.SetDirty`锛岄槻姝慨鏀逛涪澶便€傜洰鍓嶄唬鐮佷腑宸茶緝濂藉湴澶勭悊浜嗚繖涓€鐐癸紝闇€淇濇寔銆?

### 4.3 寮傚父澶勭悊涓庢棩蹇?
*   **浼樺寲**: 鍦?`SkillRunner.EvaluateAt` 鍜?`Processor` 涓鍔?`try-catch` 鍧椼€傚鏋滄槸鐢ㄦ埛缂栧啓鐨勮剼鏈紙濡傝嚜瀹氫箟鐗规晥閫昏緫锛夋姤閿欙紝涓嶅簲瀵艰嚧鏁翠釜缂栬緫鍣ㄥ穿婧冩垨棰勮鍗℃銆?

---

## 5. 鍔熻兘鎵╁睍 (Features)

### 5.1 鏇茬嚎缂栬緫鍣ㄩ泦鎴?
*   **鏂规**: 鏌愪簺鐗规晥灞炴€э紙濡傞€忔槑搴︽笎鍙樸€佷綅绉绘洸绾匡級闇€瑕佹洸绾挎帶鍒躲€傚彲浠ラ泦鎴?Unity 鍐呯疆鐨?`AnimationCurve` 缁樺埗鎺ュ彛锛屽湪 Clip 涓嬫柟鎵╁睍鍑烘洸绾跨紪杈戝尯銆?

### 5.2 宓屽 Timeline (Sub-Timeline)
*   **鏂规**: 鍏佽涓€涓?Clip 寮曠敤鍙︿竴涓?`SkillTimeline` 璧勬簮锛屽疄鐜版妧鑳界殑妯″潡鍖栧鐢紙渚嬪閫氱敤鐨勫彈鍑诲姩浣滃簭鍒楋級銆?

---

## 鎺ㄨ崘瀹炴柦璺嚎鍥?

1.  **P0 (楂樹紭)**: 瀹炴柦 **1. 鏂规 A (甯у惛闄勬ā寮?**銆傝繖鑳界洿鎺ュ洖搴斾綘瀵光€滄闀?绮惧害鈥濈殑鍏虫敞锛屽鎿嶄綔鎵嬫劅鎻愬崌鏈€鏄庢樉銆?
3.  **P2**: 闅忕潃鐗规晥鍙樺鏉傦紝瀹炴柦 **3.1 娓叉煋鍓旈櫎**銆?

---

## 6. 閫昏緫涓€鑷存€т笌甯у悓姝?(Logic Consistency & Frame Sync)

**鐩爣**: 瑙ｅ喅 Unity Update 涓嶇ǔ瀹氬鑷寸殑閫昏緫绌块€忛棶棰橈紝瀹炵幇涓ユ牸鐨勨€滃抚鍚屾鈥濋€昏緫鏇存柊銆?

### 6.1 杩愯鏃舵洿鏂版満鍒跺崌绾?(SkillRunner Upgrade)
*   **褰撳墠**: `CurrentTime += deltaTime` (Simple Accumulator)銆?
*   **浼樺寲**: 寮曞叆钃勬按姹犵畻娉?(Accumulator / Fixed Step)銆?
    *   **Logic Step**: 瀹氫箟鍥哄畾鐨?`LOGIC_STEP` (濡?0.033f)銆?
    *   **Accumulator**: 鍦?`ManualUpdate(dt)` 涓疮鍔?`dt` 鍒?`accumulator`銆?
    *   **While Loop**: `while (accumulator >= LOGIC_STEP)` 鎵ц `TickProcessors`锛屾瘡娆″彧鎺ㄨ繘 `LOGIC_STEP` 鏃堕棿銆?
    *   **Interpolation**: (鍙€? 浣跨敤 `accumulator / LOGIC_STEP` 浣滀负 alpha 鍊硷紝瀵瑰彲瑙嗗眰瀵硅薄锛堝妯″瀷浣嶇疆锛夎繘琛屾彃鍊硷紝娑堥櫎瑙嗚鎶栧姩銆?

### 6.2 澶勭悊鍣ㄩ€昏緫閲嶆瀯 (Processor Refactor)
*   **褰撳墠**: 鍒ゆ柇 `if (Start <= Current && End > Current)`銆?
*   **浼樺寲**: 
    *   鏀逛负**鍖洪棿鎵弿鍒ゅ畾**銆?
    *   `TickProcessors` 浼犲叆 `(prevTime, currentTime)`銆?
    *   鍒ゆ柇 `if (ClipEnd > prevTime && ClipStart <= currentTime)`銆?
    *   杩欐牱鍗充娇涓€娆?Update 璺ㄨ秺浜嗗涓?Logic Step锛屼篃鑳界簿纭崟鎹夊埌澶瑰湪涓棿鐨勭煭鐗囨銆?

### 6.3 鎺ㄨ崘瀹炴柦姝ラ
1.  **Refactor SkillRunner**: 淇敼 `ManualUpdate` 涓鸿搫姘存睜妯″紡銆?
2.  **Define UpdateMode**: 鍦?`SkillRunner` 涓鍔犳灇涓?`UpdateMode { Free, FrameLocked }`锛屽厑璁哥紪杈戝櫒棰勮淇濇寔 Free 妯″紡锛屾父鎴忚繍琛屼娇鐢?FrameLocked 妯″紡銆?
