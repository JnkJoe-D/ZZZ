# SkillEditor 鏋舵瀯鍒嗘瀽鎶ュ憡

## 涓€銆侀」鐩粨鏋勬瑙?
```
ATEditor/
鈹溾攢鈹€ Data/                          # 鏍稿績鏁版嵁妯″瀷
鈹?  鈹溾攢鈹€ ClipBase.cs               # 鐗囨鍩虹被
鈹?  鈹溾攢鈹€ TrackBase.cs              # 杞ㄩ亾鍩虹被
鈹?  鈹溾攢鈹€ TrackGroup.cs             # 杞ㄩ亾鍒嗙粍
鈹?  鈹斺攢鈹€ SkillTimeline.cs          # 鏃堕棿杞翠富鏁版嵁
鈹?鈹溾攢鈹€ Runtime/                       # 杩愯鏃剁郴缁?鈹?  鈹溾攢鈹€ Attributes/
鈹?  鈹?  鈹斺攢鈹€ SkillAttributes.cs    # 灞炴€ф爣璁?鈹?  鈹溾攢鈹€ Data/
鈹?  鈹?  鈹溾攢鈹€ Base/SkillClip.cs     # 娉涘瀷鐗囨鍩虹被
鈹?  鈹?  鈹溾攢鈹€ Clips/                # 鍏蜂綋鐗囨绫诲瀷
鈹?  鈹?  鈹斺攢鈹€ Tracks/               # 鍏蜂綋杞ㄩ亾绫诲瀷
鈹?  鈹溾攢鈹€ Logic/
鈹?  鈹?  鈹溾攢鈹€ Base/BaseClipProcessor.cs
鈹?  鈹?  鈹斺攢鈹€ Processors/           # 鐗囨澶勭悊鍣?鈹?  鈹溾攢鈹€ Services/
鈹?  鈹?  鈹溾攢鈹€ IServices.cs          # 鏈嶅姟鎺ュ彛
鈹?  鈹?  鈹溾攢鈹€ RuntimeAnimationService.cs
鈹?  鈹?  鈹斺攢鈹€ RuntimeVFXService.cs
鈹?  鈹斺攢鈹€ System/
鈹?      鈹溾攢鈹€ ISkillContext.cs      # 涓婁笅鏂囨帴鍙?鈹?      鈹斺攢鈹€ SkillRunner.cs        # 杩愯鏃舵墽琛屽櫒
鈹?鈹斺攢鈹€ Editor/                        # 缂栬緫鍣ㄦ墿灞?    鈹溾攢鈹€ Drawers/
    鈹?  鈹溾攢鈹€ Base/                 # 缁樺埗鍣ㄥ熀绫?    鈹?  鈹斺攢鈹€ Impl/                 # 鍏蜂綋缁樺埗鍣?    鈹斺攢鈹€ Services/
        鈹斺攢鈹€ EditorAnimationService.cs

Editor/ATEditor/                 # 缂栬緫鍣ㄧ獥鍙?鈹溾攢鈹€ Core/
鈹?  鈹溾攢鈹€ SerializationUtility.cs   # 搴忓垪鍖栧伐鍏?鈹?  鈹溾攢鈹€ ATEditorEvents.cs      # 浜嬩欢鎬荤嚎
鈹?  鈹斺攢鈹€ ATEditorState.cs       # 鐘舵€佺鐞?鈹溾攢鈹€ Views/
鈹?  鈹溾攢鈹€ TimelineView.cs           # 鏃堕棿杞磋鍥?鈹?  鈹溾攢鈹€ ToolbarView.cs            # 宸ュ叿鏍忚鍥?鈹?  鈹斺攢鈹€ TrackListView.cs          # 杞ㄩ亾鍒楄〃瑙嗗浘
鈹溾攢鈹€ ATEditorWindow.cs          # 涓荤獥鍙?鈹溾攢鈹€ ATEditorSettingsWindow.cs  # 璁剧疆绐楀彛
鈹斺攢鈹€ TrackObjectWrapper.cs         # Inspector 鍖呰鍣?```

---

## 浜屻€佺被鑱岃矗璇﹁В

### 2.1 鏁版嵁妯″瀷灞?
| 绫诲悕 | 鑱岃矗 | 鍏抽敭鎴愬憳 |
|------|------|----------|
| **SkillTimeline** | 鎶€鑳芥椂闂磋酱涓绘暟鎹鍣紝瀛樺偍鎶€鑳介厤缃?| `skillId`, `duration`, `tracks`, `groups` |
| **TrackBase** | 杞ㄩ亾鎶借薄鍩虹被锛岀鐞嗙墖娈甸泦鍚?| `trackId`, `clips`, `CanOverlap` |
| **ClipBase** | 鐗囨鎶借薄鍩虹被锛屽畾涔夋椂闂村睘鎬?| `startTime`, `duration`, `blendInDuration` |
| **TrackGroup** | 杞ㄩ亾鍒嗙粍锛屾敮鎸佹姌鍙犲拰鎵归噺绠＄悊 | `groupId`, `trackIds`, `isCollapsed` |

### 2.2 杩愯鏃剁郴缁熷眰

| 绫诲悕 | 鑱岃矗 | 鍏抽敭鏂规硶 |
|------|------|----------|
| **SkillRunner** | 杩愯鏃舵墽琛屽紩鎿庯紝椹卞姩鐗囨鎾斁 | `ManualUpdate()`, `EvaluateAt()`, `Tick()` |
| **BaseClipProcessor** | 鐗囨澶勭悊鍣ㄦ娊璞″熀绫?| `OnEnter()`, `OnUpdate()`, `OnExit()`, `OnTick()` |
| **ClipContext** | 杩愯鏃朵笂涓嬫枃锛屾彁渚涙湇鍔℃敞鍐?| `GetService<T>()`, `RegisterService<T>()` |
| **ISkillContext** | 涓婁笅鏂囨帴鍙ｏ紝瑙ｈ€﹁繍琛屾椂涓庣紪杈戝櫒 | `Owner`, `IsPreviewMode`, `GetService<T>()` |

### 2.3 鏈嶅姟灞?
| 鎺ュ彛/绫诲悕 | 鑱岃矗 | 瀹炵幇绫?|
|-----------|------|--------|
| **IAnimationService** | 鍔ㄧ敾鏈嶅姟鎺ュ彛 | `RuntimeAnimationService`, `EditorAnimationService` |
| **IVFXService** | 鐗规晥鏈嶅姟鎺ュ彛 | `RuntimeVFXService` |
| **IAudioService** | 闊抽鏈嶅姟鎺ュ彛 | (寰呭疄鐜? |

### 2.4 缂栬緫鍣ㄦ牳蹇冨眰

| 绫诲悕 | 鑱岃矗 | 鍏抽敭鎴愬憳 |
|------|------|----------|
| **ATEditorWindow** | 缂栬緫鍣ㄤ富绐楀彛锛屽崗璋冩墍鏈夊瓙瑙嗗浘 | `state`, `events`, `timelineView`, `trackListView` |
| **ATEditorState** | 鍏ㄥ眬 UI 鐘舵€佺鐞?| `zoom`, `timeIndicator`, `selectedClips`, `trackCache` |
| **ATEditorEvents** | 浜嬩欢鎬荤嚎锛岃В鑰﹁鍥鹃€氫俊 | `OnSelectionChanged`, `OnTimeIndicatorChanged` |
| **SerializationUtility** | JSON 搴忓垪鍖?鍙嶅簭鍒楀寲 | `ExportToJson()`, `ImportFromJson()` |

### 2.5 缂栬緫鍣ㄨ鍥惧眰

| 绫诲悕 | 鑱岃矗 | 浠ｇ爜琛屾暟 |
|------|------|----------|
| **TimelineView** | 鏃堕棿杞磋鍥撅紝澶勭悊鐗囨鎷栨嫿銆佸惛闄勩€佺粯鍒?| ~2540 琛?|
| **TrackListView** | 杞ㄩ亾鍒楄〃瑙嗗浘锛屽垎缁勭鐞嗐€佹嫋鎷芥帓搴?| ~1160 琛?|
| **ToolbarView** | 宸ュ叿鏍忚鍥撅紝鎾斁鎺у埗銆佸鍏ュ鍑?| ~235 琛?|

### 2.6 Inspector 鍖呰灞?
| 绫诲悕 | 鑱岃矗 |
|------|------|
| **ClipObject** | 鐗囨鐨?ScriptableObject 鍖呰锛岀敤浜?Inspector 鏄剧ず |
| **TrackObject** | 杞ㄩ亾鐨?ScriptableObject 鍖呰 |
| **GroupObject** | 鍒嗙粍鐨?ScriptableObject 鍖呰 |

---

## 涓夈€佺被渚濊禆鍏崇郴鍥?
```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                         缂栬緫鍣ㄥ眰 (Editor)                           鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? ATEditorWindow                                                   鈹?鈹?      鈹溾攢鈹€ ATEditorState (鐘舵€佺鐞?                                鈹?鈹?      鈹溾攢鈹€ ATEditorEvents (浜嬩欢鎬荤嚎)                               鈹?鈹?      鈹溾攢鈹€ TimelineView 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                                  鈹?鈹?      鈹溾攢鈹€ TrackListView 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹尖攢鈹€ 渚濊禆 SkillTimeline 鏁版嵁        鈹?鈹?      鈹斺攢鈹€ ToolbarView 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                                  鈹?鈹?                                                                     鈹?鈹? Inspector 鍖呰鍣? ClipObject, TrackObject, GroupObject             鈹?鈹?      鈹斺攢鈹€ DrawerFactory 鈹€鈹€> ClipDrawer / TrackDrawer                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                              鈹?                              鈻?鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                         鏁版嵁灞?(Data)                               鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? SkillTimeline (ScriptableObject)                                   鈹?鈹?      鈹溾攢鈹€ List<TrackGroup> groups                                   鈹?鈹?      鈹斺攢鈹€ List<TrackBase> tracks                                    鈹?鈹?             鈹斺攢鈹€ List<ClipBase> clips                               鈹?鈹?                                                                     鈹?鈹? 缁ф壙鍏崇郴:                                                           鈹?鈹? TrackBase <鈹€鈹€ AnimationTrack, VFXTrack, DamageTrack, etc.         鈹?鈹? ClipBase <鈹€鈹€ SkillClip<T> <鈹€鈹€ AnimationClip, VFXClip, etc.        鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                              鈹?                              鈻?鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                       杩愯鏃跺眰 (Runtime)                            鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? SkillRunner (MonoBehaviour)                                        鈹?鈹?      鈹溾攢鈹€ ClipContext (ISkillContext 瀹炵幇)                          鈹?鈹?      鈹溾攢鈹€ List<ProcessorState> _states                              鈹?鈹?      鈹斺攢鈹€ Services: IAnimationService, IVFXService                  鈹?鈹?                                                                     鈹?鈹? BaseClipProcessor <鈹€鈹€ AnimationClipProcessor, VFXClipProcessor    鈹?鈹?                                                                     鈹?鈹? 鏈嶅姟瀹炵幇:                                                           鈹?鈹? RuntimeAnimationService 鈹€鈹€> AnimComponent (澶栭儴渚濊禆)               鈹?鈹? RuntimeVFXService 鈹€鈹€> ParticleSystem                               鈹?鈹? EditorAnimationService 鈹€鈹€> Animator + PlayableGraph                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

---

## 鍥涖€佹暟鎹祦鍒嗘瀽

### 4.1 缂栬緫鍣ㄥ埌杩愯鏃剁殑鏁版嵁娴?
```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?    JSON 搴忓垪鍖?     鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? 缂栬緫鍣ㄦā寮?  鈹?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈻?鈹? 杩愯鏃舵ā寮?  鈹?鈹?SkillTimeline 鈹?   ExportToJson()   鈹?SkillTimeline 鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                     鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?       鈹?                                    鈹?       鈹?鐢ㄦ埛缂栬緫                             鈹?鍔犺浇閰嶇疆
       鈻?                                    鈻?鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                     鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?TimelineView 鈹?                     鈹?SkillRunner  鈹?鈹?TrackListView鈹?                     鈹?             鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                     鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                                              鈹?                                              鈻?                                       鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?                                       鈹?ClipProcessor鈹?                                       鈹?  鎵ц閫昏緫    鈹?                                       鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

### 4.2 鐗囨鎵ц娴佺▼

```csharp
// SkillRunner.Tick() 鏍稿績閫昏緫
foreach (var state in _states)
{
    var clip = state.clip;
    bool isOverlap = (clip.StartTime < nextTime) && (clip.EndTime > prevTime);
    
    if (isOverlap)
    {
        if (!state.isRunning)
        {
            state.processor.OnEnter(context);  // 杩涘叆鐗囨
            state.isRunning = true;
        }
        
        state.processor.OnUpdate(context, progress);  // 鏇存柊杩涘害
        state.processor.OnTick(context, clipLocalTime, clipPrevLocalTime);  // 甯у悓姝ラ€昏緫
        
        if (clip.EndTime <= nextTime)
        {
            state.processor.OnExit(context);  // 閫€鍑虹墖娈?            state.isRunning = false;
        }
    }
}
```

### 4.3 缂栬緫鍣ㄩ瑙堟祦绋?
```
鐢ㄦ埛鎷栨嫿鏃堕棿杞?鈹€鈹€鈻?OnTimeIndicatorChanged 浜嬩欢
                          鈹?                          鈻?               ATEditorWindow.OnPreviewTimeChanged()
                          鈹?                          鈻?                   SkillRunner.EvaluateAt(time)
                          鈹?                          鈻?                   閬嶅巻鎵€鏈?Processor
                          鈹?                          鈻?                   processor.OnSample() (缂栬緫鍣ㄩ瑙堥噰鏍?
```

---

## 浜斻€佽璁℃ā寮忎娇鐢ㄥ垎鏋?
### 5.1 妯℃澘鏂规硶妯″紡

**浣嶇疆**: `Runtime/Data/Base/SkillClip.cs`

```csharp
// 娉涘瀷绾︽潫纭繚姣忎釜 Clip 鍏宠仈鐗瑰畾鐨?Processor
public abstract class SkillClip<TProcessor> : ClipBase 
    where TProcessor : BaseClipProcessor, new()
{
    public override BaseClipProcessor CreateProcessorInternal()
    {
        return new TProcessor();  // 瀛愮被鑷姩鍒涘缓瀵瑰簲澶勭悊鍣?    }
}
```

**浼樼偣**: 缂栬瘧鏃剁被鍨嬪畨鍏紝鑷姩鍏宠仈 Clip 鍜?Processor

### 5.2 绛栫暐妯″紡

**浣嶇疆**: `Runtime/Logic/Base/BaseClipProcessor.cs`

涓嶅悓绫诲瀷鐨勭墖娈甸€氳繃涓嶅悓鐨?Processor 瀹炵幇涓嶅悓鐨勬墽琛岀瓥鐣ワ細
- `AnimationClipProcessor`: 鎾斁鍔ㄧ敾
- `VFXClipProcessor`: 鐢熸垚鐗规晥
- `DamageClipProcessor`: 鎵ц浼ゅ鍒ゅ畾

### 5.3 宸ュ巶妯″紡

**浣嶇疆**: `Editor/Drawers/Base/TrackDrawer.cs`

```csharp
public static class DrawerFactory
{
    public static TrackDrawer CreateDrawer(TrackBase track)
    {
        if (track is VFXTrack) return new VFXTrackDrawer();
        if (track is AnimationTrack) return new AnimationTrackDrawer();
        return new DefaultTrackDrawer();
    }
}
```

### 5.4 瑙傚療鑰呮ā寮?
**浣嶇疆**: `Editor/Core/ATEditorEvents.cs`

```csharp
public class ATEditorEvents
{
    public Action OnSelectionChanged;
    public Action<float> OnTimeIndicatorChanged;
    public Action OnRepaintRequest;
    
    public void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke();
        OnRepaintRequest?.Invoke();
    }
}
```

### 5.5 閫傞厤鍣ㄦā寮?
**浣嶇疆**: `Editor/TrackObjectWrapper.cs`

灏嗘櫘閫?C# 瀵硅薄閫傞厤涓?ScriptableObject锛屼互渚垮湪 Unity Inspector 涓樉绀猴細

```csharp
public class ClipObject : ScriptableObject
{
    [HideInInspector] public ClipBase clipData;
    [HideInInspector] public SkillTimeline timeline;
}
```

### 5.6 澶栬妯″紡

**浣嶇疆**: `Runtime/System/SkillRunner.cs`

涓哄鏉傜殑鐗囨鎵ц绯荤粺鎻愪緵绠€鍗曠殑缁熶竴鎺ュ彛锛?- `ManualUpdate()`: 鎵嬪姩鏇存柊
- `EvaluateAt()`: 璺冲抚棰勮
- `Initialize()`: 鍒濆鍖栨墍鏈夊鐞嗗櫒

---

## 鍏€佹灦鏋勯棶棰樺垎鏋?
### 6.1 涓ラ噸闂

#### 闂 1: TimelineView 绫昏繃浜庡簽澶?(2540+ 琛?

**浣嶇疆**: `Editor/ATEditor/Views/TimelineView.cs`

**闂鎻忚堪**:
- 鍗曚竴绫绘壙鎷呬簡缁樺埗銆佷氦浜掋€佸惛闄勩€佹嫋鎷姐€佸鍒剁矘璐寸瓑澶氱鑱岃矗
- 杩濆弽鍗曚竴鑱岃矗鍘熷垯 (SRP)
- 闅句互缁存姢鍜屾祴璇?
**寤鸿**: 鎷嗗垎涓哄涓笓鑱岀被
```
TimelineView (鍗忚皟鑰?
鈹溾攢鈹€ TimelineRenderer (缁樺埗閫昏緫)
鈹溾攢鈹€ ClipInteractionHandler (鐗囨浜や簰)
鈹溾攢鈹€ SnapManager (鍚搁檮绯荤粺)
鈹溾攢鈹€ SelectionManager (閫夋嫨绯荤粺)
鈹斺攢鈹€ ClipboardManager (澶嶅埗绮樿创)
```

#### 闂 2: GetAllClips() 鏂规硶瀛樺湪 Bug

**浣嶇疆**: `Editor/ATEditor/ATEditorWindow.cs`

```csharp
private List<ClipBase> GetAllClips()
{
    var list = new List<ClipBase>();
    foreach (var t in state.currentTimeline.tracks)
    {
        if (!t.isEnabled) continue;
        foreach (var c in t.clips)
        {
            if (c.isEnabled)
            {
                list.AddRange(t.clips);  // Bug: 搴旇鍙坊鍔?c锛岃€屼笉鏄暣涓垪琛?            }
        }
    }
    return list;
}
```

**淇寤鸿**:
```csharp
if (c.isEnabled)
{
    list.Add(c);  // 鍙坊鍔犲綋鍓嶅惎鐢ㄧ殑鐗囨
}
```

#### 闂 3: EditorAnimationService 鏈畬鏁村疄鐜?
**浣嶇疆**: `ATEditor/Editor/Services/EditorAnimationService.cs`

```csharp
public void Play(UnityEngine.AnimationClip clip, float transitionDuration)
{
    // TODO: 瀹屾暣瀹炵幇 Editor 涓嬬殑 PlayableGraph 棰勮
    Debug.Log($"[EditorAnimationService] Play {clip.name}");
}

public void Evaluate(float time)
{
    // if (!_graph.IsValid()) return;
    // _graph.Evaluate(time);  // 琚敞閲婃帀浜?}
```

**褰卞搷**: 缂栬緫鍣ㄩ瑙堟ā寮忎笅鍔ㄧ敾鏃犳硶姝ｇ‘閲囨牱

### 6.2 涓瓑闂

#### 闂 4: 缂哄皯 Track-Clip 绫诲瀷绾︽潫

**闂鎻忚堪**: 褰撳墠璁捐涓紝浠讳綍绫诲瀷鐨?Clip 閮藉彲浠ユ坊鍔犲埌浠讳綍绫诲瀷鐨?Track锛岃繖鍙兘瀵艰嚧杩愯鏃堕敊璇€?
**褰撳墠瀹炵幇**:
```csharp
// TrackBase.cs
public T AddClip<T>(float startTime) where T : ClipBase, new()
{
    T clip = new T();  // 娌℃湁绫诲瀷妫€鏌?    clips.Add(clip);
    return clip;
}
```

**寤鸿**: 娣诲姞绫诲瀷绾︽潫鏈哄埗
```csharp
// 寤鸿娣诲姞 TrackTypeAttribute
[TrackType(typeof(AnimationClip))]
public class AnimationTrack : TrackBase { }

// 鍦?AddClip 鏃堕獙璇?public void AddClip(ClipBase clip)
{
    var allowedTypes = GetAllowedClipTypes();
    if (!allowedTypes.Contains(clip.GetType()))
        throw new InvalidOperationException("Clip type not compatible with track");
}
```

#### 闂 5: 鏈嶅姟娉ㄥ唽鏈哄埗涓嶅鍋ュ．

**浣嶇疆**: `Runtime/System/SkillRunner.cs` 涓殑 ClipContext

**闂鎻忚堪**: 鏈嶅姟閫氳繃瀛楀吀瀛樺偍锛屼絾娌℃湁鐢熷懡鍛ㄦ湡绠＄悊鍜屾湇鍔′緷璧栬В鏋愩€?
```csharp
public T GetService<T>() where T : class
{
    if (_services.TryGetValue(typeof(T), out object service))
        return service as T;
    Debug.LogError($"Service {typeof(T).Name} not found!");
    return null;
}
```

**寤鸿**: 寮曞叆鏈嶅姟瀹氫綅鍣ㄦā寮忔垨渚濊禆娉ㄥ叆妗嗘灦

#### 闂 6: 搴忓垪鍖栫郴缁熺己灏戠増鏈帶鍒?
**浣嶇疆**: `Editor/Core/SerializationUtility.cs`

**闂鎻忚堪**: JSON 搴忓垪鍖栨病鏈夌増鏈彿绠＄悊锛屾湭鏉ユ暟鎹粨鏋勫彉鏇村彲鑳藉鑷存棫鏂囦欢鏃犳硶鍔犺浇銆?
**寤鸿**:
```csharp
[Serializable]
public class SkillTimelineVersion
{
    public string version = "1.0";
    public int formatVersion = 1;  // 鐢ㄤ簬杩佺Щ閫昏緫
}

// 瀵煎叆鏃舵鏌ョ増鏈苟杩佺Щ
public static SkillTimeline ImportFromJson(string path)
{
    var timeline = ...;
    if (timeline.formatVersion < CURRENT_VERSION)
        MigrateData(timeline);
    return timeline;
}
```

### 6.3 杞诲井闂

#### 闂 7: 纭紪鐮佺殑榄旀湳鏁板瓧

**浣嶇疆**: 澶氬

```csharp
// TimelineView.cs
private const float TIME_RULER_HEIGHT = 30f;
private const float TRACK_HEIGHT = 40f;
private const float GROUP_HEIGHT = 30f;

// TrackListView.cs
private const float TRACK_HEIGHT = 40f;  // 閲嶅瀹氫箟
```

**寤鸿**: 鎻愬彇鍒扮粺涓€鐨勯厤缃被
```csharp
public static class SkillEditorConfig
{
    public const float TIME_RULER_HEIGHT = 30f;
    public const float TRACK_HEIGHT = 40f;
    public const float GROUP_HEIGHT = 30f;
}
```

#### 闂 8: 缂哄皯鍗曞厓娴嬭瘯

**闂鎻忚堪**: 鏁翠釜椤圭洰娌℃湁鍙戠幇娴嬭瘯浠ｇ爜锛屽叧閿€昏緫濡傚惛闄勮绠椼€佹椂闂磋浆鎹㈢瓑缂哄皯娴嬭瘯瑕嗙洊銆?
#### 闂 9: 娉ㄩ噴璇█涓嶄竴鑷?
**闂鎻忚堪**: 閮ㄥ垎浠ｇ爜浣跨敤鑻辨枃娉ㄩ噴锛岄儴鍒嗕娇鐢ㄤ腑鏂囷紝涓嶇鍚堣鑼冭姹傘€?
```csharp
// ClipBase.cs - 娣峰悎娉ㄩ噴
/// <summary>
/// 鐗囨鍩虹被     // 涓枃
/// </summary>
public abstract class ClipBase : ISkillClipData
{
    // Legacy / Blending support  // 鑻辨枃
}
```

---

## 涓冦€佹敼杩涘缓璁?
### 7.1 鐭湡鏀硅繘 (1-2 鍛?

| 浼樺厛绾?| 浠诲姟 | 璇存槑 |
|--------|------|------|
| P0 | 淇 GetAllClips() Bug | 楂樹紭鍏堢骇锛屽奖鍝嶅姛鑳芥纭€?|
| P0 | 瀹屽杽 EditorAnimationService | 瀹炵幇瀹屾暣鐨?PlayableGraph 棰勮 |
| P1 | 缁熶竴閰嶇疆甯搁噺 | 鍒涘缓 SkillEditorConfig 绫?|
| P1 | 娣诲姞鍏抽敭鍗曞厓娴嬭瘯 | 瑕嗙洊鏃堕棿杞崲銆佸惛闄勮绠?|

### 7.2 涓湡鏀硅繘 (1-2 鏈?

| 浼樺厛绾?| 浠诲姟 | 璇存槑 |
|--------|------|------|
| P1 | 閲嶆瀯 TimelineView | 鎷嗗垎涓哄涓笓鑱岀被 |
| P1 | 娣诲姞 Track-Clip 绫诲瀷绾︽潫 | 缂栬瘧鏃剁被鍨嬪畨鍏?|
| P2 | 瀹炵幇搴忓垪鍖栫増鏈帶鍒?| 鏀寔鏁版嵁杩佺Щ |
| P2 | 瀹屽杽鏈嶅姟灞?| 娣诲姞鏈嶅姟鐢熷懡鍛ㄦ湡绠＄悊 |

### 7.3 闀挎湡鏀硅繘 (3+ 鏈?

| 浼樺厛绾?| 浠诲姟 | 璇存槑 |
|--------|------|------|
| P2 | 寮曞叆渚濊禆娉ㄥ叆妗嗘灦 | 濡?Zenject/VContainer |
| P2 | 瀹炵幇 Undo/Redo 绯荤粺 | 鍩轰簬鍛戒护妯″紡 |
| P3 | 娣诲姞鎵╁睍鏈哄埗 | 鏀寔鑷畾涔?Track/Clip 绫诲瀷鎻掍欢 |
| P3 | 鎬ц兘浼樺寲 | 澶ч噺鐗囨鏃剁殑娓叉煋浼樺寲 |

---

## 鍏€佹灦鏋勪紭鍔挎€荤粨

| 浼樺娍 | 璇存槑 |
|------|------|
| **娓呮櫚鐨勫垎灞傛灦鏋?* | 鏁版嵁灞傘€佽繍琛屾椂灞傘€佺紪杈戝櫒灞傝亴璐ｅ垎鏄?|
| **鑹ソ鐨勬墿灞曟€?* | 閫氳繃缁ф壙 TrackBase/ClipBase 鍙交鏉炬坊鍔犳柊绫诲瀷 |
| **缂栬緫鍣?杩愯鏃跺垎绂?* | 缂栬緫鍣ㄤ唬鐮佷笉浼氭墦鍖呭埌鏈€缁堜骇鍝?|
| **鏈嶅姟鎶借薄** | IAnimationService/IVFXService 鏀寔涓嶅悓鐜瀹炵幇 |
| **浜嬩欢椹卞姩** | ATEditorEvents 瀹炵幇浜嗚鍥鹃棿鐨勬澗鑰﹀悎 |
| **妯℃澘鏂规硶妯″紡** | SkillClip<TProcessor> 瀹炵幇缂栬瘧鏃剁被鍨嬪畨鍏?|

---

## 涔濄€佹€讳綋璇勪环

SkillEditor 鏄竴涓姛鑳藉畬鏁寸殑鎶€鑳界紪杈戝櫒绯荤粺锛岄噰鐢ㄤ簡鍚堢悊鐨勫垎灞傛灦鏋勫拰澶氱璁捐妯″紡銆傛牳蹇冭璁＄悊蹇碉紙Clip-Track-Timeline 缁撴瀯銆丳rocessor 绛栫暐妯″紡銆佹湇鍔℃娊璞★級鏄纭殑銆?
### 涓昏闂

1. **TimelineView 杩囦簬搴炲ぇ** - 杩濆弽鍗曚竴鑱岃矗鍘熷垯
2. **GetAllClips() Bug** - 褰卞搷鍔熻兘姝ｇ‘鎬?3. **EditorAnimationService 鏈畬鎴?* - 缂栬緫鍣ㄩ瑙堝彈闄?4. **缂哄皯绫诲瀷绾︽潫** - Track-Clip 缁勫悎鏃犳牎楠?5. **缂哄皯娴嬭瘯瑕嗙洊** - 鍏抽敭閫昏緫鏃犱繚闅?
### 鏀硅繘鏂瑰悜

閫氳繃涓婅堪鏀硅繘寤鸿锛屽彲浠ユ樉钁楁彁鍗囦唬鐮佽川閲忓拰鍙淮鎶ゆ€с€傚缓璁紭鍏堝鐞?P0 绾у埆鐨勯棶棰橈紝鐒跺悗閫愭杩涜鏋舵瀯閲嶆瀯銆?
---

**鍒嗘瀽鏃ユ湡**: 2026-02-14
