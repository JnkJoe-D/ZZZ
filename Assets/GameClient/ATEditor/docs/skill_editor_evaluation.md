# SkillEditor 浠ｇ爜搴撹瘎浼版姤鍛?

## 1. 鎬讳綋璇勪及

SkillEditor 鏄竴涓熀浜?Unity `ScriptableObject` 鍜?`EditorWindow` 鐨勮嚜瀹氫箟鏃堕棿杞寸紪杈戝櫒銆傛暣浣撴灦鏋勬竻鏅帮紝鍒嗕负 **Data锛堟暟鎹眰锛?*銆?*Runtime锛堣繍琛屾椂椹卞姩灞傦級** 鍜?**Editor锛堢紪杈戝櫒浜や簰灞傦級**銆?

-   **浼樼偣**锛?
    -   鏁版嵁缁撴瀯璁捐鍚堢悊锛屽埄鐢?`[SerializeReference]` 瀹炵幇浜嗗鎬佸瓨鍌紝閬垮厤浜?MonoBehaviour 鐨勯€氳繃 GameObject 鎸傝浇鐨勭箒鐞愩€?
    -   杩愯鏃堕┍鍔ㄩ€昏緫涓庢暟鎹垎绂伙紝閫氳繃 `Process` 妯″紡瑙ｈ€︼紝鏄撲簬娴嬭瘯鍜屾墿灞曘€?
    -   缂栬緫鍣ㄨ鍥撅紙UI锛夊疄鐜颁簡铏氭嫙鍖栨覆鏌擄紙鍙覆鏌撳彲瑙佸尯鍩燂級锛屾€ц兘浼樺寲鎰忚瘑杈冨ソ銆?
    -   瀹炵幇浜嗗畬鏁寸殑 Undo/Redo 绯荤粺銆?
    -   涓枃娉ㄩ噴璇﹀敖锛屼唬鐮侀鏍肩粺涓€銆?

-   **缂虹偣**锛?
    -   **缂栬緫鍣ㄥ眰杩濆弽寮€闂師鍒?(OCP)**锛氭坊鍔犳柊鐨勮建閬撶被鍨嬮渶瑕佷慨鏀?`TrackListView` 鐨勬牳蹇冧唬鐮侊紙纭紪鐮佺殑 `switch-case` 鍜岃彍鍗曟瀯寤猴級锛屽鑷存墿灞曟€у湪缂栬緫鍣ㄥ眰闈㈠彈闃汇€?
    -   **閮ㄥ垎浠ｇ爜鑰﹀悎搴﹂珮**锛歚ProcessContext` 瀵圭壒瀹氭父鎴忕粍浠讹紙濡?`AnimComponent`锛夋湁鐩存帴渚濊禆锛岄檷浣庝簡绯荤粺鐨勯€氱敤鎬с€?
    -   **娼滃湪鐨勮繍琛屾椂 Bug**锛歚RuntimeVFXProcess` 鍦ㄥ崗绋嬭皟鐢ㄤ笂瀛樺湪闅愭偅銆?

---

## 2. 鏋舵瀯鍒嗘瀽

### 2.1 鏁版嵁灞?(Runtime/Data)
-   **缁撴瀯**锛歚SkillTimeline` (鏍戞牴) -> `Group` -> `TrackBase` -> `ClipBase`銆?
-   **搴忓垪鍖?*锛氫娇鐢?Unity 鐨?`[SerializeReference]` 鐗规€у瓨鍌ㄥ鎬佸垪琛ㄣ€傝繖鏄竴涓幇浠ｄ笖楂樻晥鐨勯€夋嫨锛屽厑璁稿湪涓嶅鍔?`ScriptableObject` 鏂囦欢鏁伴噺鐨勬儏鍐典笅瀛樺偍澶嶆潅灞傜骇鏁版嵁銆?
-   **椋庨櫓**锛歚[SerializeReference]` 渚濊禆绫诲悕鍜岀▼搴忛泦鍚嶃€傚鏋滈噸鏋勪唬鐮侊紙閲嶅懡鍚嶇被鎴栫Щ鍔ㄥ懡鍚嶇┖闂达級锛屽彲鑳藉鑷存暟鎹涪澶便€傚缓璁€氳繃 `[MovedFrom]` 鐗规€ф垨鑷畾涔夊簭鍒楀寲閽╁瓙鏉ヨ閬块闄┿€?

### 2.2 杩愯鏃跺眰 (Runtime/Playback)
-   **椹卞姩鏍稿績**锛歚SkillRunner` 鏄竴涓函 C# 鐘舵€佹満锛屼笉渚濊禆 `MonoBehaviour`锛岃繖浣垮緱瀹冨彲浠ュ湪闈?Unity 鍦烘櫙锛堝鏈嶅姟鍣ㄩ獙璇侊級涓洿瀹规槗琚鐢紙鍓嶆彁鏄墺绂诲 Unity API 鐨勪緷璧栵級銆?
-   **Process 妯″紡**锛氶€氳繃 `ProcessFactory` 鍜?`[ProcessBinding]` 鐗规€э紝瀹炵幇浜?`Clip` 鏁版嵁鍒?`IProcess` 閫昏緫鐨勫姩鎬佺粦瀹氥€傝繖鏄竴涓潪甯镐紭绉€鐨勮璁★紙鍗崇瓥鐣ユā寮忥級锛屽畬鍏ㄧ鍚?**寮€闂師鍒?(OCP)** 鈥斺€?鏂板 Clip 绫诲瀷鍙渶缂栧啓鏂扮殑 Process 绫诲苟鎵撲笂鏍囩锛屾棤闇€淇敼 Runner 浠ｇ爜銆?
-   **瀵硅薄姹?*锛歚VFXPoolManager` 鎻愪緵浜嗗熀纭€鐨勫璞℃睜绠＄悊锛屼絾瀹炵幇涓洪潤鎬佺被锛屼笉鍒╀簬渚濊禆娉ㄥ叆鍜屽崟鍏冩祴璇曘€?

### 2.3 缂栬緫鍣ㄥ眰 (Editor)
-   **MVC 鍙樹綋**锛?
    -   **Model**: `SkillTimeline` 鍙婂叾瀛愬璞°€?
    -   **View**: `ATEditorWindow`, `TimelineView`, `TrackListView`銆?
    -   **State/Controller**: `ATEditorState` 缁存姢缂栬緫鍣ㄧ姸鎬侊紙閫変腑椤广€佹粴鍔ㄤ綅缃級锛宍ATEditorEvents` 澶勭悊娑堟伅鍒嗗彂銆?
-   **UI 娓叉煋**锛氫娇鐢ㄤ簡 `GUI.BeginGroup` 鍜屾暟瀛﹁绠楁潵瀹炵幇鑷畾涔夌殑 Timeline 鎺т欢銆傚疄鐜颁簡瑙嗗彛瑁佸壀锛圕ulling锛夛紝鍙覆鏌撳彲瑙佽寖鍥村唴鐨?Clip 鍜?Track锛屼繚璇佷簡鍦ㄩ暱 Timeline 涓嬬殑缂栬緫鍣ㄥ抚鐜囥€?

---

## 3. 浠ｇ爜璐ㄩ噺涓庤璁℃ā寮?(SOLID 鍒嗘瀽)

### 渚濊禆鍊掔疆鍘熷垯 (DIP) - **鑹ソ**
Runtime 灞傞€氳繃 `IProcess` 鎺ュ彛涓庡叿浣撲笟鍔￠€昏緫瑙ｈ€︺€俙ProcessFactory` 璐熻矗渚濊禆娉ㄥ叆鐨勫疄渚嬪寲宸ヤ綔銆?

### 鍗曚竴鑱岃矗鍘熷垯 (SRP) - **鑹ソ**
绫昏亴璐ｅ垝鍒嗘槑纭€?
-   `SkillRunner` 鍙鏃堕棿鎺ㄨ繘鍜岀姸鎬佽皟搴︺€?
-   `RuntimeVFXProcess` 鍙鐗规晥鐢熷懡鍛ㄦ湡銆?
-   `TimelineView` 鍙缁樺埗鍜岃緭鍏ヨ浆鍙戙€?

### 寮€闂師鍒?(OCP) - **娣峰悎**
-   **Runtime**: **浼樼**銆傛柊澧炲姛鑳斤紙濡傛柊鐨勭壒鏁堢被鍨嬶級涓嶉渶瑕佷慨鏀规牳蹇冧唬鐮併€?
-   **Editor**: **杈冨樊**銆傚湪 `TrackListView.cs` 涓細
    -   `CreateTrackByType` 鏂规硶鍖呭惈纭紪鐮佺殑 `switch-case`銆?
    -   `ShowGroupContextMenu` 鏂规硶鍖呭惈纭紪鐮佺殑鑿滃崟椤规坊鍔犻€昏緫銆?
    -   **鍚庢灉**锛氭瘡澧炲姞涓€绉嶆柊鐨勮建閬撶被鍨嬶紝閮介渶瑕佷慨鏀?`TrackListView.cs`锛岃繖瀹规槗寮曞叆 Bug 涓旈毦浠ョ淮鎶ゃ€?

### 鎺ュ彛闅旂鍘熷垯 (ISP) - **涓€鑸?*
`IProcess` 鎺ュ彛瀹氫箟绠€娲?(`OnEnter`, `OnUpdate`, `OnExit`, `Reset`)锛岀鍚堣姹傘€?

---

## 4. 鍙戠幇鐨勯棶棰樹笌闅愭偅

### 4.1 杩愯鏃?Bug (RuntimeVFXProcess.cs)
鍦?`RuntimeVFXProcess.OnExit` 涓細
```csharp
var runner = context.GetService<MonoBehaviour>("CoroutineRunner");
// ...
runner.StartCoroutine(DelayReturn(vfxInstance, maxLifetime));
```
**闅愭偅**锛歚runner` 鏄粠 `context` 鍔ㄦ€佽幏鍙栫殑銆傚鏋?Context 涓病鏈夋敞鍐?"CoroutineRunner"锛屼笖 `SkillLifecycleManager.Instance` 涔熶笉瀛樺湪锛堜緥濡傚湪闈炴爣鍑嗗満鏅垨娴嬭瘯鍦烘櫙锛夛紝`runner` 涓?null锛屽鑷?`RuntimeVFXProcess` 鏃犳硶姝ｇ‘寤惰繜鍥炴敹鐗规晥锛屾垨鑰呮姏鍑虹┖寮曠敤寮傚父锛堣櫧鐒舵湁 null check锛屼絾 fallback 閫昏緫涔熷彲鑳藉け璐ワ級銆?
**寤鸿**锛氬簲璇ュ湪 `ProcessContext` 鍒濆鍖栨椂寮哄埗妫€鏌ュ繀瑕佺殑 Service锛屾垨鑰呭皢 `DelayReturn` 鏀逛负闈?Coroutine 鐨勮鏃跺櫒瀹炵幇锛堜緷璧?`OnUpdate` 璁℃椂锛夈€?

### 4.2 缂栬緫鍣ㄦ墿灞曟€у彈闄?
濡傚墠鎵€杩帮紝`TrackListView` 纭紪鐮佷簡杞ㄩ亾绫诲瀷銆?
**寤鸿**锛氬紩鍏?`TrackDrawer` 鎴?`TrackDescriptor` 姒傚康銆備娇鐢ㄥ弽灏勬垨鐗规€э紙绫讳技 Runtime 鐨?`ProcessBinding`锛夋潵鑷姩鍙戠幇鎵€鏈夌户鎵胯嚜 `TrackBase` 鐨勭被鍨嬶紝骞惰嚜鍔ㄦ瀯寤哄彸閿彍鍗曘€?

### 4.3 闈欐€佸己鑰﹀悎
`VFXPoolManager` 鏄潤鎬佺被锛屼笖鍦?`RuntimeVFXProcess` 涓洿鎺ヨ皟鐢ㄣ€傝繖浣垮緱鎯宠鏇挎崲瀵硅薄姹犲疄鐜帮紙渚嬪鎹㈡垚 Addressables 鐨勫璞℃睜锛夊彉寰楀洶闅俱€?
**寤鸿**锛氬畾涔?`IVFXPool` 鎺ュ彛锛屽苟閫氳繃 `ProcessContext` 娉ㄥ叆鍏蜂綋鐨?Pool 瀹炵幇銆?

---

## 5. 鏀硅繘寤鸿

### 5.1 閲嶆瀯缂栬緫鍣ㄧ殑杞ㄩ亾鍒涘缓閫昏緫
鍒涘缓 `TrackDefinition` 鐗规€э紝鐢ㄤ簬鏍囪杞ㄩ亾绫荤殑鍏冩暟鎹紙鏄剧ず鍚嶇О銆佽彍鍗曡矾寰勩€佸浘鏍囩瓑锛夈€?
淇敼 `TrackListView`锛屼娇鍏跺湪鍒濆鍖栨椂鎵弿鎵€鏈夊甫鏈?`TrackDefinition` 鐨勭被锛屽姩鎬佺敓鎴愬彸閿彍鍗曞拰鍒涘缓閫昏緫銆?

### 5.2 澧炲己 ProcessContext
绉婚櫎 `ProcessContext` 涓 `Game.MAnimSystem` 鍛藉悕绌洪棿鐨勭洿鎺ヤ緷璧栵紙濡?`AnimComponent`锛夈€傜洰鍓嶇殑 `PushLayerMask` 鐩存帴鎿嶄綔鍏蜂綋缁勪欢锛屽鑷撮€氱敤 SkillEditor 蹇呴』渚濊禆鐗瑰畾娓告垙閫昏緫銆?
**寤鸿**锛氫娇鐢ㄤ簨浠舵垨娉涘瀷鎺ュ彛 `ILayerMaskHandler` 鏉ユ娊璞¤繖涓€琛屼负銆?

### 5.3 浼樺寲 Coroutine 渚濊禆
鍦?`RuntimeVFXProcess` 涓紝閬垮厤渚濊禆澶栭儴 `MonoBehaviour` 鏉ヨ繍琛屽崗绋嬨€傚彲浠ヨ€冭檻鍦?`SkillRunner` 涓粺涓€绠＄悊寤惰繜浠诲姟锛屾垨鑰呰 `Process` 鍦?`OnUpdate` 涓嚜琛屽鐞嗗墿浣欏鍛藉€掕鏃讹紙鍗充娇 Clip 宸茬粨鏉燂紝Process 涔熷彲浠ヨ繘鍏ヤ竴涓€滄竻鐞嗛樁娈碘€濈洿鍒扮湡姝ｇ粨鏉燂紝浣嗚繖闇€瑕佷慨鏀?Runner 鐨勭敓鍛藉懆鏈熺鐞嗭級銆傛洿涓虹畝鍗曠殑鍋氭硶鏄‘淇?Context 涓缁堝瓨鍦ㄤ竴涓彲闈犵殑 Runner銆?

---

## 6. 鎬荤粨
SkillEditor 鏄竴涓畬鎴愬害杈冮珮涓旀灦鏋勫簳瀛愪笉閿欑殑缂栬緫鍣ㄥ伐鍏枫€傚叾 Runtime 璁捐浼樹簬 Editor 璁捐銆備富瑕佹敼杩涚偣搴旈泦涓湪 **鎻愬崌缂栬緫鍣ㄧ殑鎵╁睍鎬?* 鍜?**闄嶄綆 Runtime 瀵瑰叿浣撲笟鍔′唬鐮佺殑鑰﹀悎** 涓娿€?
