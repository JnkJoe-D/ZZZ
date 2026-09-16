# Skill Editor Phase 3: Editor Experience Refinements (Context & Gizmos)

鍦ㄦ妧鑳界紪杈戝櫒鐨?Phase 3 (鍙鍖栦笌棰勮浣撻獙浼樺寲) 闃舵锛屾垜浠Н绱簡涓€绯诲垪鎻愬崌妗嗘灦鍋ュ．鎬у拰鎵╁睍鎬х殑鍏抽敭寮€鍙戠粡楠岋紝涓昏闆嗕腑鍦?**杩愯鏃剁幆澧冩ā鎷燂紙Context 棰勭儹锛?* 鍜?**缂栬緫鍣ㄧ姸鎬侀┍鍔ㄧ粯鍒讹紙Inspector 鍙樺姩鍝嶅簲锛?* 涓や釜鏂瑰悜銆?

## 1. 闈欐€?Context 鐨勬蹇典笌棰勭儹绛栫暐 (Prewarming ProcessContext)

### 鑳屾櫙涓庨棶棰?
鍘熸湰鍙湪 `SkillRunner.Play()` 琚皟鐢ㄥ悗鎵嶄細鐪熸鐢熸垚骞舵敞鍏?`ProcessContext`銆傝繖灏卞鑷寸紪杈戝櫒鍦ㄣ€屾湭鎾斁锛堝垰鍚姩銆佸仠姝級銆嶇殑鐘舵€佷笅锛堝嵆闈欐€侀瑙堢姸鎬侊級锛屽簳灞傜殑鍚勭 `Drawer` (濡?`DamageClipDrawer`) 鏃犳硶鑾峰彇鍒拌濡?`ISkillActor` 杩欑被闇€瑕佸姩鎬佽В鏋愮殑瀹炰綋銆傜敱姝ゅ嚭鐜颁簡鈥滄病鎾斁鏃朵笉鐢?Gizmos鈥濇垨鈥滅┖寮曠敤鎶ラ敊鈥濈殑涓ラ噸缂洪櫡銆?

### 瑙ｅ喅鏂规
閫氳繃寮曞叆 **鈥滆繍琛屾椂鐜瑙ｈ€﹂鐑€?* 妯″紡瑙ｅ喅锛?
1. **Runner 鏆撮湶搴曞眰鐘舵€佹敞鍏ョ偣**锛氬湪 `SkillRunner` 涓鍔?`PrewarmContext(ProcessContext initialContext)` 鏂规硶銆傚畠鐨勮亴璐ｆ槸鍏佽璋冪敤鏂瑰湪涓嶆敼鍙樼姸鎬佹満 (`SkillRunnerState.Stopped`) 鐨勫墠鎻愪笅寮鸿濉炲叆涓€涓复鏃?Context銆?
2. **鍏ㄥ眬鐘舵€侀€忎紶**锛氬湪鍏ㄧ紪杈戝櫒鍏变韩鐨?`ATEditorState` 涓鍔?`PreviewContext => previewRunner?.Context` 鍏ㄥ眬 Getter銆?
3. **缂栬緫鍣ㄥ姞杞界敓鍛藉懆鏈熸帴绠?*锛氬湪 `ATEditorWindow.InitPreview()`锛堝嵆绐楀彛鍚姩鎴栨洿鎹㈤瑙堟ā鍨嬫椂瑙﹀彂鐨勬柟娉曪級鍐呴儴鏋勯€犱竴涓粎渚涢瑙堢敤鐨?`SkillServiceFactory` 鍜?`ProcessContext`锛屽苟璋冪敤 `PrewarmContext`銆?
4. **鍔犺浇淇濆簳鍥為€€**锛氳€冭檻鍒扮敤鎴峰彲鑳介娆℃墦寮€骞舵病鏈夊叧鑱旈瑙堥鍒朵綋锛屽紩鍏ヤ簡鈥滈粯璁ら瑙堥鍒朵綋鈥濈殑姒傚康骞跺皢璺緞閰嶇疆鎵撻€氫簡 `EditorPrefs` 鍜?`ATEditorSettingsWindow`锛屼繚璇侀鐑摼璺笂蹇呯劧瀛樺湪鍚堟硶鐨?`GameObject` 瀹炰緥銆?

### 缁忛獙鎬荤粨
鍦ㄥ埗浣滈噸搴﹁В鑰︼紙渚濊禆鍊掔疆锛夌殑鏃跺簭绫荤紪杈戝櫒鏃讹紝**缂栬緫鍣ㄧ殑鈥滃仠姝㈡€佲€濅笉绛変簬鍘熶欢鐨勨€滈攢姣佹€佲€?*銆備负浜嗚鍚勭鍩轰簬缁戝畾瀵硅薄銆侀楠间箖鑷崇壒鏁堥敋鐐瑰畾浣嶇殑缂栬緫鍣ㄨ緟鍔╃嚎 (Gizmo) 姝ｅ父宸ヤ綔锛屼綘蹇呴』涓虹紪杈戝櫒鍑嗗涓€濂楄兘琚叏灞€璁块棶鍒扮殑 **Dummy Context锛堣櫄鎷熶笂涓嬫枃锛?* 骞剁淮鎶ゅ畠鐨勭敓鍛藉懆鏈熴€?

---

## 2. 鍩轰簬浜嬩欢椹卞姩鐨勫師鐢?Inspector 鍝嶅簲鏈哄埗 (OnInspectorChange & OnSceneView Repaint)

### 鑳屾櫙涓庨棶棰?
Unity 鐨勫師鐢?Inspector 闈㈡澘锛堝€熷姪 `[CustomEditor]` 娓叉煋鍘熺敓鐣岄潰锛夋槸鎶€鑳界紪杈戝櫒鏁版嵁淇敼鐨勪富闃靛湴锛堝璋冩暣 `HitBoxShape` 鍗婂緞銆佷綅缃亸绉伙級銆?
浣嗗湪缂栬緫鍣ㄤ腑璋冩暣鏁版嵁鏃讹紝濡傛灉娌℃湁鏄惧紡瑙﹀彂 Scene 瑙嗗浘閲嶇粯鐨勬柟娉曡皟鐢紝鐢ㄦ埛鍦?Inspector 鎷栨嫿鐨勬暟鎹彉鍖栧氨涓嶄細鍗虫椂鍙嶆槧鍦?Scene 瑙嗗浘鐨勮緟鍔╃嚎涓娿€備箣鍓嶇粡甯稿嚭鐜扮敱浜庢病鏈夌劍鐐瑰垏鎹㈠鑷?Scene 娌″埛鏂扮殑闃绘柇鎰熴€?

### 瑙ｅ喅鏂规
閫氳繃 **浜嬩欢鎬荤嚎妗ユ帴** 鍜?**缂栬緫鍣ㄧ姸鎬佽剰鏍囪 (Dirty Flag / Change Check)** 鏉ユˉ鑱斿弻绔€?
1. **鎷︽埅灞炴€у彉鍔?*锛氬湪 `SkillInspectorBase` (缁熶竴鎺ョ鎵€鏈夎嚜瀹氱被鐨?Inspector 娓叉煋鍩虹被) 鐨?`OnInspectorGUI` 涓紝鐢?`EditorGUI.BeginChangeCheck()` 鍜?`EditorGUI.EndChangeCheck()` 鍖呰９鎵€鏈夊弽灏勫嚭鏉ョ殑 GUI 娓叉煋鍐呭銆?
2. **浜嬩欢鎬荤嚎鎶涘嚭鍙樺姩**锛氫竴鏃︽崟鑾峰埌鏀瑰姩 (`EndChangeCheck` 杩斿洖 true)锛屽氨寮哄埗瑙﹀彂 `events.OnInspectorChanged?.Invoke()` 骞朵笖鎶婃敼鍔ㄥ悗鐨勬暟鎹洖鍐欏洖 Timeline 鏁版嵁灞備互纭繚 Undo 绯荤粺鑳芥嫤鎴埌銆?
3. **Scene 鍒锋柊璁㈤槄**锛歍imeline 绯荤粺浣滀负浜嬩欢鏍稿績锛堝叿浣撳湪 `ATEditorWindow` 涓級锛屼粠瀹冪殑 `OnEnable` 灏遍€氳繃 `events.OnInspectorChanged += RepaintScene` 璁㈤槄浜嗚繖涓簨浠讹紝骞跺湪澶勭悊鍑芥暟閲岄€氳繃 `SceneView.RepaintAll()` 杩涜涓诲姩閲嶇粯鍝嶅簲銆?

### 缁忛獙鎬荤粨
瀵逛簬鈥滄墍瑙佸嵆鎵€寰椻€濈殑鑷埗澶氱獥鍙ｅ崗浣滃瀷缂栬緫鍣紝**鏁版嵁灞傦紙Model锛夈€侀潰鏉胯鍥撅紙Inspector/Property Editor锛夈€佸満鏅鍥撅紙Scene/Timeline锛変箣闂寸粷瀵逛笉鑳界洿鎺ュ己鑰﹀悎璋冪敤鍒锋柊閫昏緫**銆傚繀椤讳娇鐢ㄤ竴濂楃函绮圭殑 `EventBus` (鍦ㄨ繖涓灦鏋勯噷鏄?`ATEditorEvents`) 鎶娾€滅敤鎴锋敼浜嗘暟鎹€濊繖涓€浜嬩欢骞挎挱鍑哄幓銆?

---

## 3. 2D/Cylindrical 鍦ㄥ叏绌洪棿 (3D) 搴曞眰涓嬬殑鐗╃悊闄嶇淮

### 鑳屾櫙涓庨棶棰?
鏈€鍒濈殑瀹炵幇涓紝纰版挒鐩掑垽瀹氬ぇ閲忎緷璧栦簬鐞冨績鍒ゅ畾锛坄OverlapSphere`锛夈€傜敱浜?`Vector3.Angle` 鍜?`Vector3.Distance` 澶╃敓鍥婃嫭鍏ㄧ珛浣撶淮搴︼紝缁撴灉鍘熸湰鎯冲疄鐜颁簩缁村垏闈綋楠岀殑鈥滄墖褰?(Sector)鈥濆拰鈥滅幆褰?(Ring)鈥濆彉鎴愪簡閿ュ舰鍜屽疄蹇冪悆澹筹紝涓庝紶缁熷姩浣滃湴闈㈡父鎴忕殑鍋氭硶鑳岄亾鑰岄┌銆?

### 瑙ｅ喅鏂规
鍒╃敤 **姝ｆ柟褰㈠垵绛?(OverlapBox) + 灞€閮ㄥ潗鏍囬檷缁磋繃婊?(Vector2 XZ Planar Calculation + Y Height Cutoff)** 瑙ｅ喅銆?
1. 灏?Broad-phase 淇敼涓哄鍘氱殑 `OverlapBox`銆?
2. 鐢?`Quaternion.Inverse * offset` 鎶婄墿鐞嗙鎾炵偣寮哄埗閫嗚浆鍥炴柦娉曡€?/ 闄勭潃鐐圭殑**灞€閮ㄧ┖闂寸郴**銆?
3. 杩涜鏆村姏楂樺害瑁佸垏 `abs(local.y) < height/2`銆?
4. 鍦ㄥ眬閮ㄥ潗鏍囩郴涓嬪彇鎶曞奖绯荤殑 `Vector2(local.x, local.z)` 閲嶅仛瑙掑害璁＄畻鍜屽钩闈㈠悜蹇冨崐寰勮窛绂昏绠椼€?
5. 鍦?Gizmo 灞傚簲鐢ㄧ函鏁板寤烘ā鏂规硶锛堥《闈㈠姬绾裤€佽繛鎺ユ１銆佷氦鍙夊崐鍦嗙悆椤讹級锛岄€氳繃绔嬩綋鐐归樀绾胯緟鍔╃敤鎴疯繕鍘?3D 杞?2D 鏌变綋瑁佸垏鐨勬娊璞″舰鎬併€?
