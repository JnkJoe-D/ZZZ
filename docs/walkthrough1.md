# SimpleAnimancer 绯荤粺鏀硅繘宸ヤ綔鎬荤粨

## 姒傝堪

鏈鏀硅繘瀵?SimpleAnimancer 鍔ㄧ敾绯荤粺杩涜浜嗗叏闈紭鍖栵紝瑙ｅ喅浜嗚繃娓＄郴缁熺殑鏍稿績缂洪櫡锛屽苟鎻愬崌浜嗘€ц兘鍜屼唬鐮佽川閲忋€?
---

## 瀹屾垚鐨勫伐浣?
### 1. AnimState 鍩虹被浼樺寲

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimState.cs`

- **Playable 瀛楁璇箟鏄庣‘鍖?*
  - 灏?`_playable` 閲嶅懡鍚嶄负 `_playableCache`
  - 鏄庣‘娉ㄩ噴璇存槑锛氬瓙绫诲簲缁存姢鑷繁鐨勫叿浣撶被鍨?Playable 瀛楁浣滀负涓诲瓨鍌?  - 瑙ｅ喅浜嗗熀绫诲拰瀛愮被 Playable 瀛楁鍐椾綑鐨勫洶鎯?
- **鏂板鏃堕棿褰掍竴鍖?API**
  - `NormalizedTime` 灞炴€э細鑾峰彇/璁剧疆褰掍竴鍖栨挱鏀炬椂闂?(0.0 ~ 1.0)
  - `IsPaused` 灞炴€э細鑾峰彇/璁剧疆鏆傚仠鐘舵€?  - `Pause()` 鏂规硶锛氭殏鍋滄挱鏀?  - `Resume()` 鏂规硶锛氭仮澶嶆挱鏀?
### 2. AnimLayer 杩囨浮绯荤粺閲嶆瀯

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

- **涓柇鍒楄〃娉曞疄鐜?*
  - 鏂板 `FadingState` 缁撴瀯浣擄紝杩借釜鎵€鏈夋贰鍑虹姸鎬?  - 鏂板 `_fadingStates` 鍒楄〃锛岀鐞嗗鐘舵€佽繃娓?  - 琚腑鏂殑鐘舵€佽嚜鍔ㄥ姞閫熸贰鍑猴紙2鍊嶉€燂級
  - 鏉冮噸褰掍竴鍖栫‘淇濇€诲拰濮嬬粓涓?1.0

- **瑙ｅ喅鐨勯棶棰?*
  - A鈫払 杩囨浮涓垏鎹㈠埌 C锛孉 鏉冮噸涓嶅啀鍗′綇
  - 杩炵画鍒囨崲 A鈫払鈫扖鈫扗锛屾墍鏈夌姸鎬佹纭繃娓?  - 鏃犵姸鎬佷涪澶憋紝鏃犳潈閲嶅紓甯?
### 3. AnimLayer 鐘舵€佺紦瀛樻満鍒?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

- **Dictionary 缂撳瓨瀹炵幇**
  - 鏂板 `_clipStateCache` 瀛楀吀锛岀紦瀛?AnimationClip 鈫?ClipState 鏄犲皠
  - `Play(clip)` 浼樺厛杩斿洖缂撳瓨瀹炰緥
  - 缂撳瓨涓婇檺 32 涓姸鎬侊紝瓒呭嚭鏃惰嚜鍔ㄦ竻鐞嗘渶涔呮湭浣跨敤鐨勭姸鎬?  - 鏂板 `ClearCache()` 鏂规硶鎵嬪姩娓呴櫎缂撳瓨

- **鎬ц兘鎻愬崌**
  - 閬垮厤閲嶅鍒涘缓 ClipState
  - 鍑忓皯棰戠箒 GC 鍒嗛厤

### 4. AnimLayer 鐘舵€佹竻鐞嗘満鍒?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

- **寤惰繜娓呯悊闃熷垪瀹炵幇**
  - 鏂板 `_pendingCleanup` 瀛楀吀锛岃拷韪緟娓呯悊鐘舵€?  - 娣″嚭瀹屾垚鐨勭姸鎬佹爣璁颁负寰呮竻鐞?  - 寤惰繜 2 绉掑悗鑷姩閿€姣侊紙缂撳瓨鐘舵€侀櫎澶栵級
  - 鏂板 `DisconnectState()` 鍜?`DestroyState()` 鏂规硶

- **瑙ｅ喅鐨勫唴瀛樻硠婕?*
  - 鏃х姸鎬佷笉鍐嶆案涔呴┗鐣欏唴瀛?  - 绔彛姝ｇ‘鍥炴敹澶嶇敤

### 5. LinearMixerState 闃堝€艰嚜鍔ㄦ帓搴?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/LinearMixerState.cs`

- **鎻掑叆鎺掑簭瀹炵幇**
  - `Add(clip, threshold)` 鑷姩鎸夐槇鍊兼帓搴?  - 鏂板 `ReorderMixerPorts()` 鏂规硶閲嶆柊杩炴帴绔彛
  - 鏂板 `GetThreshold()` 鏂规硶鑾峰彇闃堝€?
- **瑙ｅ喅鐨勯棶棰?*
  - 鐢ㄦ埛鏃犻渶鎵嬪姩鎸夐『搴忔坊鍔?  - 鎻掑€艰绠楀缁堟纭?
### 6. BlendTreeState2D 鏁扮粍棰勫垎閰嶄紭鍖?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/BlendTreeState2D.cs`

- **棰勫垎閰嶇紦鍐插尯瀹炵幇**
  - 鏂板 `_weightBuffer` 鏁扮粍锛屽垵濮嬪閲?8
  - 鎸夐渶鑷姩鎵╁锛?鍊嶏級
  - 鏂板 `GetPosition()` 鏂规硶鑾峰彇 2D 鍧愭爣

- **鎬ц兘鎻愬崌**
  - 娑堥櫎姣忓抚 `new float[count]` 鐨?GC 鍒嗛厤

### 7. MixerState 瀛楁閲嶅懡鍚?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/MixerState.cs`

- 灏?`_mixer` 閲嶅懡鍚嶄负 `_mixerPlayable`
- 涓?`ClipState._clipPlayable` 鍛藉悕椋庢牸涓€鑷?- 鏇存柊鎵€鏈夊紩鐢?
### 8. 娴嬭瘯鑴氭湰鏇存柊

**鏂囦欢**: `Assets/GameClient/MAnimSystem/SimpleAnimancerTest.cs`

- **鏂板娴嬭瘯鐢ㄤ緥**
  - 鎸?F锛氶绻佸垏鎹㈡祴璇曪紙楠岃瘉涓柇鍒楄〃娉曪級
  - 鎸?G锛氱姸鎬佺紦瀛橀獙璇?  - 鎸?H锛氬綊涓€鍖栨椂闂?API 娴嬭瘯
  - 鎸?P锛氭殏鍋?鎭㈠娴嬭瘯

- **鏀硅繘婕旂ず**
  - 1D 娣峰悎鍣ㄦ紨绀洪槇鍊间贡搴忔坊鍔?  - 鏃ュ織杈撳嚭鏇磋缁?
---

## 娴嬭瘯缁撴灉

| 娴嬭瘯椤?| 缁撴灉 |
|--------|------|
| 鍩虹鎾斁涓庤繃娓?| 鉁?閫氳繃 |
| 浜嬩欢瑙﹀彂 (OnEnd/OnFadeComplete) | 鉁?閫氳繃 |
| 棰戠箒鍒囨崲娴嬭瘯 (20娆?50ms闂撮殧) | 鉁?閫氳繃锛屾棤鏉冮噸鍗′綇 |
| 鐘舵€佺紦瀛橀獙璇?| 鉁?閫氳繃锛屽悓涓€ Clip 杩斿洖鐩稿悓瀹炰緥 |
| 褰掍竴鍖栨椂闂?API | 鉁?閫氳繃 |
| 鏆傚仠/鎭㈠鍔熻兘 | 鉁?閫氳繃 |
| 1D 娣峰悎鍣ㄩ槇鍊兼帓搴?| 鉁?閫氳繃锛屼贡搴忔坊鍔犲悗姝ｇ‘鎻掑€?|
| 2D 娣峰悎鍣?| 鉁?閫氳繃 |

---

## 鏋舵瀯鏀硅繘瀵规瘮

### 杩囨浮绯荤粺

```
鏃у疄鐜帮細
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? _currentState 鈹€鈹€鈫?_targetState     鈹?鈹? (鍙拷韪?涓姸鎬侊紝涓棿鐘舵€佷涪澶?        鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?
鏂板疄鐜帮細
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? _fadingStates[0] 鈹€鈹€鈫?娣″嚭涓?       鈹?鈹? _fadingStates[1] 鈹€鈹€鈫?娣″嚭涓?涓柇)   鈹?鈹? _fadingStates[2] 鈹€鈹€鈫?娣″嚭涓?涓柇)   鈹?鈹? _targetState     鈹€鈹€鈫?娣″叆涓?       鈹?鈹? (杩借釜鎵€鏈夌姸鎬侊紝鏃犱涪澶?              鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

### 鐘舵€佺敓鍛藉懆鏈?
```
鏃у疄鐜帮細
Play(clip) 鈫?鍒涘缓 ClipState 鈫?杩炴帴 鈫?[姘镐箙瀛樺湪]

鏂板疄鐜帮細
Play(clip) 鈫?妫€鏌ョ紦瀛?鈫?鍛戒腑锛氳繑鍥炵紦瀛樺疄渚?                        鏈懡涓細鍒涘缓骞剁紦瀛?         鈫?娣″嚭瀹屾垚 鈫?鏍囪寰呮竻鐞?鈫?寤惰繜2绉?鈫?閿€姣?```

---

## 娉ㄦ剰浜嬮」

1. **缂撳瓨鐘舵€佷笉浼氳娓呯悊**
   - 閫氳繃 `Play(clip)` 鎾斁鐨勭姸鎬佷細琚紦瀛?   - 鐩存帴 `Play(state)` 鎾斁鐨勭姸鎬佷笉浼氳缂撳瓨

2. **涓柇鍔犻€熷€嶇巼鍙厤缃?*
   - 褰撳墠璁剧疆涓?2 鍊嶉€?(`INTERRUPT_SPEED_MULTIPLIER`)
   - 鍙牴鎹渶姹傝皟鏁?
3. **缂撳瓨澶у皬闄愬埗**
   - 褰撳墠涓婇檺 32 涓姸鎬?(`MAX_CACHE_SIZE`)
   - 瓒呭嚭鏃惰嚜鍔ㄦ竻鐞嗘渶涔呮湭浣跨敤鐨勯潪褰撳墠鎾斁鐘舵€?
4. **娓呯悊寤惰繜鏃堕棿**
   - 褰撳墠璁剧疆涓?2 绉?(`CLEANUP_DELAY`)
   - 鍙牴鎹渶姹傝皟鏁?
---

## 鍚庣画鍙墿灞曟柟鍚?
1. ~~**澶氬眰娣峰悎鏀寔**~~ 鉁?宸插畬鎴?   - 娣诲姞 `AnimationLayerMixerPlayable`
   - 鏀寔 AvatarMask 瀹炵幇涓婁笅鍗婅韩鍒嗗眰

2. **鍔ㄧ敾浜嬩欢绯荤粺**
   - 鏀寔 AnimationClip 鍐呭祵浜嬩欢
   - 鏀寔鍏抽敭甯у洖璋?
3. ~~**鍔ㄧ敾閬僵**~~ 鉁?宸插畬鎴?   - 鏀寔 Additive 娣峰悎妯″紡
   - 鏀寔閮ㄥ垎楠ㄩ閬僵

---

## Layer 绯荤粺瀹炵幇璁板綍 (2026-02-13)

### 姒傝堪

瀹炵幇浜嗗畬鏁寸殑澶氬眰鍔ㄧ敾娣峰悎绯荤粺锛屾敮鎸?AvatarMask銆丄dditive 妯″紡鍜屽眰鏉冮噸娣″叆娣″嚭銆?
### 鏋舵瀯璁捐

```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                     AnimComponent                             鈹?鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹?鈹? 鈹?             AnimationLayerMixerPlayable                 鈹? 鈹?鈹? 鈹?        (绠＄悊鎵€鏈夊眰鐨勬贩鍚堛€丮ask銆丄dditive)                鈹? 鈹?鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹?鈹?             鈻?             鈻?             鈻?                  鈹?鈹?             鈹?             鈹?             鈹?                  鈹?鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹粹攢鈹€鈹€鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹粹攢鈹€鈹€鈹€鈹€鈹€鈹? 鈹屸攢鈹€鈹€鈹粹攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?      鈹?鈹? 鈹?  Layer 0     鈹? 鈹?  Layer 1   鈹? 鈹?  Layer 2     鈹?      鈹?鈹? 鈹? (Base)       鈹? 鈹?(UpperBody) 鈹? 鈹? (Effects)    鈹?      鈹?鈹? 鈹? Weight: 1.0  鈹? 鈹? Weight: w  鈹? 鈹? Weight: w    鈹?      鈹?鈹? 鈹? Mask: null   鈹? 鈹? Mask: ...  鈹? 鈹? Additive     鈹?      鈹?鈹? 鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹? 鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? 鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?      鈹?鈹? 鈹? 鈹?Mixer   鈹? 鈹? 鈹? 鈹?Mixer  鈹?鈹? 鈹? 鈹?Mixer  鈹?  鈹?      鈹?鈹? 鈹? 鈹?鐘舵€佹贩鍚?鈹? 鈹? 鈹? 鈹?       鈹?鈹? 鈹? 鈹?       鈹?  鈹?      鈹?鈹? 鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹? 鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹? 鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?      鈹?鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?      鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

### 瀹屾垚鐨勫伐浣?
#### 1. AnimLayer 灞傚睘鎬ф墿灞?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

- **鏂板灞傚睘鎬?*
  - `Weight`: 灞傛潈閲?(0 ~ 1)
  - `Mask`: AvatarMask 楠ㄩ閬僵
  - `IsAdditive`: 鍙犲姞妯″紡寮€鍏?
- **鏂板灞傛贰鍏ユ贰鍑?*
  - `StartFade(float targetWeight, float duration)`: 灞傛潈閲嶆贰鍏ユ贰鍑?  - `UpdateLayerFade(float deltaTime)`: 鍐呴儴鏇存柊鏂规硶

- **鏋勯€犲嚱鏁版墿灞?*
  - 鏂板 `AnimationLayerMixerPlayable` 鍙傛暟
  - 鑷姩鍚屾鍒濆鏉冮噸鍒?LayerMixer

#### 2. AnimComponent 澶氬眰绠＄悊

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimComponent.cs`

- **鏂板瀛楁**
  - `_layerMixer`: AnimationLayerMixerPlayable 瀹炰緥
  - `_layers`: List<AnimLayer> 灞傚垪琛?  - `LayerCount`: 灞傛暟閲忓睘鎬?
- **鏂板绱㈠紩鍣?*
  - `this[int index]`: 鎳掑垱寤哄眰

- **鏂板鏂规硶**
  - `GetLayer(int index)`: 鑾峰彇鎴栧垱寤烘寚瀹氬眰
  - `CreateLayer(int index)`: 鍐呴儴鍒涘缓灞傛柟娉?  - `Play(clip, layerIndex, fadeDuration)`: 鍦ㄦ寚瀹氬眰鎾斁

- **鍥捐繛鎺ラ噸鏋?*
  ```
  鏃? AnimLayer.Mixer -> AnimationPlayableOutput
  鏂? Layer[0].Mixer 鈹€鈹?      Layer[1].Mixer 鈹€鈹尖攢鈹€> LayerMixer -> AnimationPlayableOutput
      Layer[2].Mixer 鈹€鈹?  ```

#### 3. 娴嬭瘯鐢ㄤ緥

**鏂囦欢**: `Assets/GameClient/MAnimSystem/Test1.cs`

- **鏂板娴嬭瘯璧勬簮瀛楁**
  - `upperBodyClip`: 涓婂崐韬姩鐢?  - `breatheClip`: 鍙犲姞鍔ㄧ敾
  - `upperBodyMask`: 涓婂崐韬伄缃?
- **鏂板娴嬭瘯鎸夐敭**
  - U: 涓婂崐韬眰娴嬭瘯锛堝甫 AvatarMask锛?  - I: 鍙犲姞灞傛祴璇曪紙Additive锛?  - O: 灞傛贰鍏ユ贰鍑烘祴璇?  - L: 鍔ㄦ€佸垱寤哄眰娴嬭瘯
  - M: 澶氬眰鍚屾椂鎾斁娴嬭瘯

### API 浣跨敤绀轰緥

```csharp
// 鍩虹灞傛挱鏀?animComponent.Play(walkClip);

// 涓婂崐韬眰鎾斁锛堝甫 Mask锛?var upperLayer = animComponent[1];
upperLayer.Mask = upperBodyMask;
upperLayer.Play(attackClip);

// 鍙犲姞灞傛挱鏀?var additiveLayer = animComponent[2];
additiveLayer.IsAdditive = true;
additiveLayer.Play(breatheClip);

// 灞傛贰鍏ユ贰鍑?upperLayer.StartFade(0f, 0.25f);  // 娣″嚭
upperLayer.StartFade(1f, 0.25f);  // 娣″叆

// 鍦ㄦ寚瀹氬眰鎾斁
animComponent.Play(clip, layerIndex: 1, fadeDuration: 0.25f);
```

### 鍏抽敭瀹炵幇缁嗚妭

| 鍔熻兘 | Unity API |
|------|-----------|
| 鍒涘缓灞傛贩鍚堝櫒 | `AnimationLayerMixerPlayable.Create(graph, inputCount)` |
| 璁剧疆 AvatarMask | `layerMixer.SetLayerMaskFromAvatarMask(index, mask)` |
| 璁剧疆鍙犲姞妯″紡 | `layerMixer.SetLayerAdditive(index, true)` |
| 璁剧疆灞傛潈閲?| `layerMixer.SetInputWeight(index, weight)` |

### 娴嬭瘯缁撴灉

| 娴嬭瘯椤?| 缁撴灉 |
|--------|------|
| 鍩虹灞傛挱鏀?| 鉁?閫氳繃 |
| 涓婂崐韬眰锛圓vatarMask锛?| 鉁?閫氳繃 |
| 鍙犲姞灞傦紙Additive锛?| 鉁?閫氳繃 |
| 灞傛贰鍏ユ贰鍑?| 鉁?閫氳繃 |
| 鍔ㄦ€佸垱寤哄眰 | 鉁?閫氳繃 |
| 澶氬眰鍚屾椂鎾斁 | 鉁?閫氳繃 |

### 鏂囦欢鍙樻洿娓呭崟

| 鏂囦欢 | 鍙樻洿绫诲瀷 | 琛屾暟鍙樺寲 |
|------|----------|----------|
| AnimLayer.cs | 淇敼 | +110 琛?|
| AnimComponent.cs | 閲嶅啓 | +80 琛?|
| Test1.cs | 淇敼 | +140 琛?|

---

## Bug 淇璁板綍 (2026-02-13)

### 闂鎻忚堪

蹇€熷垏鎹㈠姩鐢绘椂锛屾湁鏃朵細鍑虹幇 Mixer 鎵€鏈夎緭鍏ョ鍙ｆ潈閲嶉兘涓?0 鐨勬儏鍐碉紝瀵艰嚧鍔ㄧ敾"娑堝け"銆?
### 鏍规湰鍘熷洜

1. **褰掍竴鍖栭€昏緫缂洪櫡**锛氬彧澶勭悊 `totalWeight > 1` 鐨勬儏鍐碉紝鏈鐞?`totalWeight < 1`
2. **閲嶅鍔犲叆娣″嚭鍒楄〃**锛氬悓涓€鐘舵€佸彲鑳借澶氭鍔犲叆 `_fadingStates`锛屽鑷存潈閲嶈閲嶅鍑忓皯

### 淇鍐呭

#### 1. 褰掍竴鍖栭€昏緫淇

**鏂囦欢**: `AnimLayer.cs`

```csharp
// 淇鍓嶏細鍙鐞?> 1 鐨勬儏鍐?if (totalWeight > 1.001f && totalFadeOutWeight > 0.001f)

// 淇鍚庯細澶勭悊鎵€鏈夐潪 1 鐨勬儏鍐?if (totalWeight < 0.001f) { /* 寮傚父澶勭悊 */ }
if (Mathf.Abs(totalWeight - 1f) > 0.001f && totalFadeOutWeight > 0.001f)
```

#### 2. 闃叉閲嶅鍔犲叆娣″嚭鍒楄〃

鏂板 `AddToFadingStates` 鏂规硶锛?- 妫€鏌ョ姸鎬佹槸鍚﹀凡瀛樺湪
- 宸插瓨鍦ㄥ垯鏇存柊閫熷害锛堝彇杈冨ぇ鍊硷級
- 涓嶅瓨鍦ㄦ墠娣诲姞鏂拌褰?
#### 3. 鏂板娴嬭瘯鐢ㄤ緥

- 鎸?**N** 閿細鏉冮噸褰掍竴鍖栭獙璇佹祴璇?- 30 娆″揩閫熷垏鎹紝姣忔楠岃瘉鏉冮噸鎬诲拰鏄惁涓?1

### 鏂囦欢鍙樻洿

| 鏂囦欢 | 鍙樻洿绫诲瀷 |
|------|----------|
| AnimLayer.cs | 淇敼锛堝綊涓€鍖栭€昏緫銆佹柊澧炴柟娉曪級 |
| Test1.cs | 鏂板娴嬭瘯鐢ㄤ緥 |

---

## 鏂囦欢鍙樻洿娓呭崟

| 鏂囦欢 | 鍙樻洿绫诲瀷 | 琛屾暟鍙樺寲 |
|------|----------|----------|
| AnimState.cs | 淇敼 | +47 琛?|
| AnimLayer.cs | 閲嶅啓 | +210 琛?|
| ClipState.cs | 淇敼 | +1 琛?|
| MixerState.cs | 淇敼 | +5 琛?|
| LinearMixerState.cs | 閲嶅啓 | +60 琛?|
| BlendTreeState2D.cs | 閲嶅啓 | +30 琛?|
| Test1.cs | 閲嶅啓 | +140 琛?|

---

**鏀硅繘鏃ユ湡**: 2026-02-13

---

## 甯у悓姝ユ灦鏋勪慨姝ｈ褰?(2026-02-14)

### 姒傝堪

鍩轰簬瀵瑰抚鍚屾鏋舵瀯鐨勬纭悊瑙ｏ紝瀵?MAnimSystem 鍜?SkillEditor 杩涜浜嗛噸澶т慨姝ｃ€傛牳蹇冨彉鍖栨槸灏嗗姩鐢婚┍鍔ㄦā寮忎粠"鎵嬪姩椹卞姩"鏀逛负"濮嬬粓鑷姩椹卞姩"銆?
### 闂鍒嗘瀽

涔嬪墠鐨勯敊璇悊瑙ｏ細
- 杩愯鏃堕渶瑕?`ManualUpdate` 椹卞姩鍔ㄧ敾鏇存柊
- `Evaluate` 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕?
姝ｇ‘鐨勭悊瑙ｏ細
- 鍔ㄧ敾濮嬬粓鐢?Unity MonoUpdate 鑷姩椹卞姩
- `ManualUpdate` 搴旀敼涓?`SetSpeed`锛岀敤浜庨€熷害鎺у埗
- `Evaluate` 浠呯紪杈戝櫒棰勮闇€瑕?
### 瀹屾垚鐨勫伐浣?
#### 1. ISkillContext 澧炲姞 IsPreviewMode 灞炴€?
**鏂囦欢**: `Assets/ATEditor/Runtime/System/ISkillContext.cs`

```csharp
public interface ISkillContext
{
    GameObject Owner { get; }
    bool IsPreviewMode { get; }  // 鏂板
    T GetService<T>() where T : class;
}
```

#### 2. ClipContext 瀹炵幇 IsPreviewMode

**鏂囦欢**: `Assets/ATEditor/Runtime/System/SkillRunner.cs`

- ClipContext 鏂板 `IsPreviewMode` 灞炴€?- `EvaluateAt()` 璁剧疆 `IsPreviewMode = true`锛堢紪杈戝櫒棰勮锛?- `ManualUpdate()` 鍜?`Tick()` 璁剧疆 `IsPreviewMode = false`锛堣繍琛屾椂锛?
#### 3. AnimationClipProcessor 鍐呴儴鍒ゆ柇妯″紡

**鏂囦欢**: `Assets/ATEditor/Runtime/Logic/Processors/AnimationClipProcessor.cs`

```csharp
public override void OnUpdate(ISkillContext context, float progress)
{
    // 杩愯鏃剁洿鎺ヨ繑鍥烇紝涓嶅仛閲囨牱
    if (!context.IsPreviewMode) return;
    
    // 浠ヤ笅浠呯紪杈戝櫒棰勮妯″紡鎵ц
    animService.Evaluate(time);
}
```

#### 4. AnimComponent 绉婚櫎 UpdateMode

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimComponent.cs`

- 绉婚櫎 `UpdateMode` 鏋氫妇鍜?`updateMode` 瀛楁
- `Update()` 濮嬬粓鎵ц `UpdateInternal()`
- 鏂板 `SetSpeed(float speedScale)` 鏂规硶

#### 5. AnimLayer 澧炲姞 SetSpeed 鏂规硶

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

```csharp
public void SetSpeed(float speed)
{
    if (_targetState != null)
    {
        _targetState.Speed = speed;
    }
}
```

#### 6. IAnimationService 鎺ュ彛鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/IServices.cs`

- `ManualUpdate(float deltaTime)` 鈫?`SetSpeed(float speedScale)`
- 鏄庣‘璇箟锛氶€熷害鎺у埗鑰岄潪椹卞姩鏇存柊

#### 7. 鍒涘缓 MAnimAnimationService 閫傞厤鍣?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/MAnimAnimationService.cs` (鏂板缓)

- 瀹炵幇 `IAnimationService` 鎺ュ彛
- 灏?SkillEditor 璋冪敤杞彂鍒?AnimComponent

#### 8. RuntimeAnimationService 鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/RuntimeAnimationService.cs`

- 瀹炵幇鏂扮殑 `IAnimationService` 鎺ュ彛
- `Evaluate()` 鏂规硶鐣欑┖锛堣繍琛屾椂涓嶉渶瑕侊級

### 鏋舵瀯瀵规瘮

```
淇敼鍓嶏紙閿欒锛?                   淇敼鍚庯紙姝ｇ‘锛?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
UpdateMode: Auto / Manual    鈫?   濮嬬粓 MonoUpdate 椹卞姩
ManualUpdate(dt) 椹卞姩鍔ㄧ敾    鈫?   SetSpeed(scale) 鎺у埗閫熷害
杩愯鏃朵篃闇€瑕?Evaluate        鈫?   浠呯紪杈戝櫒棰勮闇€瑕?Evaluate
```

### 杩愯鏃舵祦绋?
```
缃戠粶灞傛敹鍒版寚浠?{ skillId, frame }
    鈫?SkillRunner.ManualUpdate(fixedDt)
    鈫?Tick(fixedDt) 鎺ㄨ繘鍥哄畾姝ラ暱
    鈫?OnEnter 鈫?Play(clip)           鈫?鍙彂鎺у埗鍛戒护
OnTick 鈫?閫昏緫鍒ゅ畾
OnExit 鈫?鍒囨崲鐘舵€?    鈫?AnimComponent 鐢?Unity Update 鑷姩椹卞姩
锛堜笉璋冪敤 OnUpdate锛屼笉鍋?Evaluate锛?```

### 鏂囦欢鍙樻洿娓呭崟

| 鏂囦欢 | 鍙樻洿绫诲瀷 | 璇存槑 |
|------|----------|------|
| ISkillContext.cs | 淇敼 | 鏂板 IsPreviewMode |
| SkillRunner.cs | 淇敼 | ClipContext 瀹炵幇 IsPreviewMode |
| AnimationClipProcessor.cs | 淇敼 | OnUpdate 鍐呴儴鍒ゆ柇 |
| AnimComponent.cs | 閲嶅啓 | 绉婚櫎 UpdateMode锛屾柊澧?SetSpeed |
| AnimLayer.cs | 淇敼 | 鏂板 SetSpeed |
| IServices.cs | 淇敼 | ManualUpdate 鈫?SetSpeed |
| MAnimAnimationService.cs | 鏂板缓 | 閫傞厤鍣ㄥ疄鐜?|
| RuntimeAnimationService.cs | 閲嶅啓 | 鏇存柊鎺ュ彛瀹炵幇 |

### 鐩稿叧鏂囨。

- [甯у悓姝ユ灦鏋勪慨姝ｆ柟妗圿(./MAnimSystem_FrameSync_Refactor_Plan.md)
- [SkillEditor 闆嗘垚璁″垝](./MAnimSystem_SkillEditor_Integration_Plan.md)

---

**淇鏃ユ湡**: 2026-02-14
