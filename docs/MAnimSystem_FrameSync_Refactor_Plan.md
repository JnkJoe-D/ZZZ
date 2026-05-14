# MAnimSystem 甯у悓姝ユ灦鏋勪慨姝ｆ柟妗?
## 涓€銆佽儗鏅笌闂

### 1.1 褰撳墠璁捐鐨勯棶棰?
涔嬪墠鐨勫疄鐜板亣璁捐繍琛屾椂闇€瑕?`ManualUpdate` 椹卞姩鍔ㄧ敾鏇存柊锛屼絾杩欎笌甯у悓姝ョ殑姝ｇ‘鍋氭硶涓嶇锛?
| 姒傚康 | 閿欒鐞嗚В | 姝ｇ‘鐞嗚В |
|:---|:---|:---|
| **鍔ㄧ敾椹卞姩** | 杩愯鏃堕渶瑕佹墜鍔ㄩ┍鍔?| 濮嬬粓鐢?Unity MonoUpdate 鑷姩椹卞姩 |
| **ManualUpdate** | 鐢ㄤ簬椹卞姩鍔ㄧ敾鎾斁 | 鐢ㄤ簬鎺у埗锛堥€熷害銆佺姸鎬佸垏鎹級 |
| **Evaluate** | 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕?| 浠呯紪杈戝櫒棰勮闇€瑕?|

### 1.2 姝ｇ‘鐨勫抚鍚屾娴佺▼

```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                    杩愯鏃舵ā寮?(Runtime)                         鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                                                                鈹?鈹? 缃戠粶灞傛敹鍒版寚浠?{ skillId, frame }                                鈹?鈹?      鈫?                                                        鈹?鈹? SkillRunner.ManualUpdate(fixedDt)                              鈹?鈹?      鈫?                                                        鈹?鈹? Tick(fixedDt) 鎺ㄨ繘鍥哄畾姝ラ暱                                      鈹?鈹?      鈫?                                                        鈹?鈹? OnEnter 鈫?Play(clip)           鈫?鍙彂鎺у埗鍛戒护                   鈹?鈹? OnTick 鈫?閫昏緫鍒ゅ畾锛堜激瀹冲垽瀹氥€佺姸鎬佹鏌ョ瓑锛?                        鈹?鈹? OnExit 鈫?鍒囨崲鐘舵€?杩斿洖寰呮満                                       鈹?鈹?      鈫?                                                        鈹?鈹? AnimComponent 鐢?Unity Update 鑷姩椹卞姩                          鈹?鈹? 锛堜笉璋冪敤 OnUpdate锛屼笉鍋?Evaluate锛?                              鈹?鈹?                                                                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                    缂栬緫鍣ㄦā寮?(Editor Preview)                  鈹?鈹溾攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                                                                鈹?鈹? SkillRunner.EvaluateAt(time)  鈫?鎷栨嫿鏃堕棿杞?                     鈹?鈹?      鈫?                                                        鈹?鈹? OnEnter 鈫?Play(clip)                                           鈹?鈹? OnUpdate 鈫?Evaluate(time)  鈫?鎵嬪姩閲囨牱鍔ㄧ敾甯?                    鈹?鈹? OnExit 鈫?鍋滄/娓呯悊                                              鈹?鈹?                                                                鈹?鈹? 鐗圭偣锛氭椂闂村彲璺宠穬锛岄渶瑕佹墜鍔ㄩ噰鏍?                                   鈹?鈹?                                                                鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

---

## 浜屻€佷换鍔℃竻鍗?
### 浠诲姟 1: ISkillContext 澧炲姞 IsPreviewMode 灞炴€?
**鏂囦欢**: `Assets/ATEditor/Runtime/System/ISkillContext.cs`

**淇敼鍐呭**:

```csharp
using UnityEngine;

namespace SkillEditor
{
    public interface ISkillContext
    {
        GameObject Owner { get; }
        
        /// <summary>
        /// 鏄惁涓虹紪杈戝櫒棰勮妯″紡銆?        /// true: 缂栬緫鍣ㄩ瑙堬紝闇€瑕佹墜鍔ㄩ噰鏍峰姩鐢诲抚銆?        /// false: 杩愯鏃舵ā寮忥紝鍔ㄧ敾鐢?Unity 鑷姩椹卞姩銆?        /// </summary>
        bool IsPreviewMode { get; }
        
        /// <summary>
        /// 鑾峰彇鐜鐗瑰畾鐨勬湇鍔★紝濡傞煶棰戠鐞嗗櫒銆佺壒鏁堢鐞嗗櫒
        /// </summary>
        T GetService<T>() where T : class;
    }
}
```

---

### 浠诲姟 2: ClipContext 瀹炵幇 IsPreviewMode

**鏂囦欢**: `Assets/ATEditor/Runtime/System/SkillRunner.cs`

**淇敼 ClipContext 绫?*:

```csharp
public class ClipContext : ISkillContext
{
    public GameObject Owner { get; private set; }
    public object CurrentClipData { get; set; }
    
    /// <summary>
    /// 鏄惁涓虹紪杈戝櫒棰勮妯″紡銆?    /// </summary>
    public bool IsPreviewMode { get; set; } = false;
    
    private Dictionary<System.Type, object> _services = new Dictionary<System.Type, object>();

    public ClipContext(GameObject owner)
    {
        Owner = owner;
    }

    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public T GetService<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out object service))
        {
            return service as T;
        }
        Debug.LogError($"Service {typeof(T).Name} not found!");
        return null;
    }
}
```

**淇敼 SkillRunner.EvaluateAt 鏂规硶**:

鍦ㄧ紪杈戝櫒棰勮鍏ュ彛璁剧疆 `IsPreviewMode = true`:

```csharp
/// <summary>
/// 璺宠穬鍒版寚瀹氭椂闂村苟姹傚€?(鐢ㄤ簬缂栬緫鍣ㄦ嫋鎷介瑙?
/// </summary>
public void EvaluateAt(float time)
{
    if (_context == null) _context = new ClipContext(gameObject);
    
    // 璁剧疆棰勮妯″紡
    _context.IsPreviewMode = true;
    
    // ... 鍏朵綑閫昏緫涓嶅彉
}
```

**淇敼 SkillRunner.Tick 鏂规硶**:

鍦ㄨ繍琛屾椂 Tick 涓缃?`IsPreviewMode = false`:

```csharp
/// <summary>
/// 鏍稿績鎺ㄨ繘閫昏緫
/// </summary>
private void Tick(float dt)
{
    if (_context != null)
    {
        // 杩愯鏃舵ā寮?        _context.IsPreviewMode = false;
    }
    
    // ... 鍏朵綑閫昏緫涓嶅彉
}
```

---

### 浠诲姟 3: AnimationClipProcessor.OnUpdate 鍐呴儴鍒ゆ柇妯″紡

**鏂囦欢**: `Assets/ATEditor/Runtime/Logic/Processors/AnimationClipProcessor.cs`

**淇敼鍐呭**:

```csharp
using UnityEngine;

namespace SkillEditor
{
    public class AnimationClipProcessor : BaseClipProcessor
    {
        public override void OnEnter(ISkillContext context)
        {
            var data = context.GetData<AnimationClip>();
            var animService = context.GetService<IAnimationService>();
            
            if (animService != null && data != null && data.animationClip != null)
            {
                // 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕侊細鎾斁鍔ㄧ敾
                animService.Play(data.animationClip, 0.1f);
            }
        }

        public override void OnUpdate(ISkillContext context, float progress)
        {
            // 杩愯鏃剁洿鎺ヨ繑鍥烇紝涓嶅仛閲囨牱
            // 鍔ㄧ敾鐢?Unity Update 鑷姩椹卞姩
            if (!context.IsPreviewMode) return;
            
            // 浠ヤ笅浠呯紪杈戝櫒棰勮妯″紡鎵ц
            var animService = context.GetService<IAnimationService>();
            var data = context.GetData<AnimationClip>();
            
            if (data != null && animService != null)
            {
                // 璁＄畻缁濆鏃堕棿
                float time = data.StartTime + data.Duration * progress;
                
                // 缂栬緫鍣ㄩ瑙堬細鎵嬪姩閲囨牱鍔ㄧ敾甯?                animService.Evaluate(time);
            }
        }

        public override void OnExit(ISkillContext context)
        {
            // 鍔ㄧ敾閫氬父涓嶉渶瑕佹樉寮忓仠姝紝璁╁叾鑷劧鎾斁/铻嶅悎鍒颁笅涓€涓?        }

        public override void OnTick(ISkillContext context, float frameTime, float prevFrameTime)
        {
            // 杩愯鏃堕€昏緫鍒ゅ畾锛堜激瀹冲垽瀹氥€佺姸鎬佹鏌ョ瓑锛?            // 姝ゅ鍙牴鎹渶瑕佹坊鍔犻€昏緫
        }
    }
}
```

---

### 浠诲姟 4: AnimComponent 绉婚櫎 UpdateMode锛屽缁?MonoUpdate 椹卞姩

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimComponent.cs`

**淇敼鍐呭**:

#### 4.1 绉婚櫎 UpdateMode 鏋氫妇鍜岀浉鍏冲瓧娈?
```csharp
// 鍒犻櫎浠ヤ笅浠ｇ爜锛?// public enum UpdateMode { Auto, Manual }
// public UpdateMode updateMode = UpdateMode.Auto;
```

#### 4.2 淇敼 Update 鏂规硶

```csharp
private void Update()
{
    if (!_isGraphCreated) return;
    
    // 濮嬬粓鑷姩鏇存柊锛岀敱 Unity 椹卞姩
    UpdateInternal(Time.deltaTime);
}
```

#### 4.3 淇敼 ManualUpdate 鏂规硶璇箟

```csharp
/// <summary>
/// 璁剧疆鍔ㄧ敾鎾斁閫熷害銆?/// 鐢ㄤ簬甯у悓姝ュ満鏅笅鐨勯€熷害鎺у埗銆?/// </summary>
/// <param name="speedScale">閫熷害缂╂斁鍥犲瓙</param>
public void SetSpeed(float speedScale)
{
    if (!_isGraphCreated) return;
    
    // 璁剧疆 Graph 鐨勬挱鏀鹃€熷害
    // 娉ㄦ剰锛氳繖浼氬奖鍝嶆墍鏈夊姩鐢荤殑鎾斁閫熷害
    foreach (var layer in _layers)
    {
        layer?.SetSpeed(speedScale);
    }
}

/// <summary>
/// [宸插純鐢╙ 璇蜂娇鐢?SetSpeed 鏂规硶銆?/// 淇濈暀姝ゆ柟娉曚粎涓哄悜鍚庡吋瀹广€?/// </summary>
/// <param name="deltaTime">姝ゅ弬鏁板湪鑷姩妯″紡涓嬭蹇界暐</param>
[System.Obsolete("璇蜂娇鐢?SetSpeed(float speedScale) 鏂规硶銆侫nimComponent 濮嬬粓鐢?Unity Update 鑷姩椹卞姩銆?)]
public void ManualUpdate(float deltaTime)
{
    // 鍦ㄦ柊鏋舵瀯涓嬶紝姝ゆ柟娉曚笉鍐嶉渶瑕?    // 鍔ㄧ敾濮嬬粓鐢?Unity Update 鑷姩椹卞姩
}
```

#### 4.4 淇濈暀 Evaluate 鏂规硶锛堢紪杈戝櫒涓撶敤锛?
```csharp
/// <summary>
/// 閲囨牱褰撳墠鍔ㄧ敾鍒版寚瀹氭椂闂淬€?/// 浠呯敤浜庣紪杈戝櫒棰勮鎴栨椂闂磋酱鎷栨嫿銆?/// 杩愯鏃惰鍕胯皟鐢ㄦ鏂规硶銆?/// </summary>
/// <param name="time">鐩爣鏃堕棿锛堢锛?/param>
public void Evaluate(float time)
{
    if (!_isGraphCreated) return;

    var state = GetLayer(0).GetCurrentState();
    if (state != null)
    {
        state.Time = time;
        Graph.Evaluate(0f);
    }
}
```

---

### 浠诲姟 5: AnimLayer 澧炲姞 SetSpeed 鏂规硶

**鏂囦欢**: `Assets/GameClient/MAnimSystem/AnimLayer.cs`

**鏂板鍐呭**:

```csharp
/// <summary>
/// 璁剧疆褰撳墠鍔ㄧ敾鐨勬挱鏀鹃€熷害銆?/// </summary>
/// <param name="speed">閫熷害鍥犲瓙 (1.0 = 姝ｅ父閫熷害)</param>
public void SetSpeed(float speed)
{
    if (_targetState != null)
    {
        _targetState.Speed = speed;
    }
}
```

---

### 浠诲姟 6: IAnimationService 鎺ュ彛璇箟鏄庣‘鍖?
**鏂囦欢**: `Assets/ATEditor/Runtime/Services/IServices.cs`

**淇敼鍐呭**:

```csharp
using UnityEngine;

namespace SkillEditor
{
    public interface IAnimationService
    {
        /// <summary>
        /// 鎾斁鍔ㄧ敾鐗囨銆?        /// 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕併€?        /// </summary>
        /// <param name="clip">鍔ㄧ敾鐗囨</param>
        /// <param name="transitionDuration">杩囨浮鏃堕棿</param>
        void Play(UnityEngine.AnimationClip clip, float transitionDuration);
        
        /// <summary>
        /// 閲囨牱鍒版寚瀹氭椂闂淬€?        /// 浠呯紪杈戝櫒棰勮闇€瑕侊紝杩愯鏃惰鍕胯皟鐢ㄣ€?        /// </summary>
        /// <param name="time">鐩爣鏃堕棿锛堢锛?/param>
        void Evaluate(float time);
        
        /// <summary>
        /// 璁剧疆鍔ㄧ敾鎾斁閫熷害銆?        /// 鐢ㄤ簬甯у悓姝ュ満鏅笅鐨勯€熷害鎺у埗銆?        /// </summary>
        /// <param name="speedScale">閫熷害缂╂斁鍥犲瓙</param>
        void SetSpeed(float speedScale);
    }
    
    // ... 鍏朵粬鎺ュ彛淇濇寔涓嶅彉
}
```

---

### 浠诲姟 7: MAnimAnimationService 閫傞厤鍣ㄩ噸鏂拌璁?
**鏂囦欢**: `Assets/GameClient/MAnimSystem/MAnimAnimationService.cs` (鏂板缓)

**瀹屾暣浠ｇ爜**:

```csharp
using UnityEngine;
using SkillEditor;

namespace Game.MAnimSystem
{
    /// <summary>
    /// MAnimSystem 鐨?SkillEditor 鍔ㄧ敾鏈嶅姟閫傞厤鍣ㄣ€?    /// 瀹炵幇 IAnimationService 鎺ュ彛锛屽皢 SkillEditor 鐨勫姩鐢昏皟鐢ㄨ浆鍙戝埌 AnimComponent銆?    /// 
    /// 璁捐璇存槑锛?    /// - Play: 杩愯鏃跺拰缂栬緫鍣ㄩ兘闇€瑕侊紝瑙﹀彂鍔ㄧ敾鎾斁銆?    /// - Evaluate: 浠呯紪杈戝櫒棰勮闇€瑕侊紝鎵嬪姩閲囨牱鍔ㄧ敾甯с€?    /// - SetSpeed: 鐢ㄤ簬閫熷害鎺у埗锛屼笉褰卞搷鍔ㄧ敾椹卞姩鏂瑰紡銆?    /// 
    /// 鍔ㄧ敾濮嬬粓鐢?AnimComponent 鐨?Unity Update 鑷姩椹卞姩銆?    /// </summary>
    public class MAnimAnimationService : IAnimationService
    {
        /// <summary>
        /// 鍏宠仈鐨?AnimComponent 瀹炰緥銆?        /// </summary>
        private AnimComponent _animComponent;
        
        /// <summary>
        /// 褰撳墠鎾斁鐨勫姩鐢荤墖娈点€?        /// </summary>
        private AnimationClip _currentClip;
        
        /// <summary>
        /// 鏋勯€犲姩鐢绘湇鍔￠€傞厤鍣ㄣ€?        /// </summary>
        /// <param name="animComponent">AnimComponent 瀹炰緥</param>
        public MAnimAnimationService(AnimComponent animComponent)
        {
            _animComponent = animComponent;
        }
        
        /// <summary>
        /// 鎾斁鍔ㄧ敾鐗囨銆?        /// </summary>
        /// <param name="clip">鍔ㄧ敾鐗囨</param>
        /// <param name="transitionDuration">杩囨浮鏃堕棿</param>
        public void Play(AnimationClip clip, float transitionDuration)
        {
            if (_animComponent == null || clip == null) return;
            
            _currentClip = clip;
            _animComponent.Play(clip, transitionDuration);
        }
        
        /// <summary>
        /// 閲囨牱鍒版寚瀹氭椂闂淬€?        /// 浠呯紪杈戝櫒棰勮闇€瑕侊紝杩愯鏃惰鍕胯皟鐢ㄣ€?        /// </summary>
        /// <param name="time">鐩爣鏃堕棿锛堢锛?/param>
        public void Evaluate(float time)
        {
            if (_animComponent == null) return;
            
            // 缂栬緫鍣ㄩ瑙堬細鎵嬪姩閲囨牱鍔ㄧ敾甯?            _animComponent.Evaluate(time);
        }
        
        /// <summary>
        /// 璁剧疆鍔ㄧ敾鎾斁閫熷害銆?        /// </summary>
        /// <param name="speedScale">閫熷害缂╂斁鍥犲瓙</param>
        public void SetSpeed(float speedScale)
        {
            if (_animComponent == null) return;
            
            _animComponent.SetSpeed(speedScale);
        }
        
        /// <summary>
        /// 鑾峰彇褰撳墠鎾斁鐨勫姩鐢荤墖娈点€?        /// </summary>
        /// <returns>褰撳墠鍔ㄧ敾鐗囨</returns>
        public AnimationClip GetCurrentClip()
        {
            return _currentClip;
        }
    }
}
```

---

### 浠诲姟 8: RuntimeAnimationService 鏇存柊

**鏂囦欢**: `Assets/ATEditor/Runtime/Services/RuntimeAnimationService.cs`

**淇敼鍐呭**:

```csharp
using UnityEngine;

namespace SkillEditor
{
    public class RuntimeAnimationService : IAnimationService
    {
        private Animator _animator;
        private float _originalSpeed = 1.0f;

        public RuntimeAnimationService(Animator animator)
        {
            _animator = animator;
            if (_animator != null)
            {
                _originalSpeed = _animator.speed;
            }
        }

        public void Play(UnityEngine.AnimationClip clip, float transitionDuration)
        {
            if (_animator == null || clip == null) return;
            // Animator 鎾斁閫昏緫
            Debug.Log($"[RuntimeAnimation] Playing clip: {clip.name}");
        }

        public void Evaluate(float time)
        {
            // 杩愯鏃朵笉闇€瑕?Evaluate
            // 鍔ㄧ敾鐢?Unity 鑷姩椹卞姩
        }

        public void SetSpeed(float speedScale)
        {
            if (_animator == null) return;
            _animator.speed = _originalSpeed * speedScale;
        }
    }
}
```

---

## 涓夈€佹枃浠跺彉鏇存竻鍗?
| 鏂囦欢 | 鎿嶄綔 | 鍙樻洿鍐呭 |
|------|------|----------|
| `ISkillContext.cs` | 淇敼 | 鏂板 `IsPreviewMode` 灞炴€?|
| `SkillRunner.cs` | 淇敼 | ClipContext 瀹炵幇 IsPreviewMode锛孍valuateAt/Tick 璁剧疆妯″紡 |
| `AnimationClipProcessor.cs` | 淇敼 | OnUpdate 鍐呴儴鍒ゆ柇 IsPreviewMode |
| `AnimComponent.cs` | 淇敼 | 绉婚櫎 UpdateMode锛屽缁?MonoUpdate 椹卞姩锛屾柊澧?SetSpeed |
| `AnimLayer.cs` | 淇敼 | 鏂板 SetSpeed 鏂规硶 |
| `IServices.cs` | 淇敼 | ManualUpdate 鏀逛负 SetSpeed锛屾槑纭涔?|
| `MAnimAnimationService.cs` | 鏂板缓 | IAnimationService 閫傞厤鍣ㄥ疄鐜?|
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
   - 楠岃瘉 Play 鍜?Evaluate 閮借璋冪敤

2. **杩愯鏃跺抚鍚屾娴佺▼**
   - 妯℃嫙缃戠粶鎸囦护锛岃Е鍙戞妧鑳芥挱鏀?   - 楠岃瘉 Play 琚皟鐢紝Evaluate 涓嶈璋冪敤
   - 楠岃瘉鍔ㄧ敾鐢?Unity 鑷姩椹卞姩

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
### 淇敼鍓?
```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                   AnimComponent                            鈹?鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?鈹? 鈹?UpdateMode: Auto / Manual                           鈹?  鈹?鈹? 鈹?                                                    鈹?  鈹?鈹? 鈹?Auto:   Update() 鈫?UpdateInternal(dt)              鈹?  鈹?鈹? 鈹?Manual: ManualUpdate(dt) 鈫?UpdateInternal(dt)      鈹?  鈹?鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

### 淇敼鍚?
```
鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?鈹?                   AnimComponent                            鈹?鈹? 鈹屸攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?鈹? 鈹?濮嬬粓鐢?Unity Update 鑷姩椹卞姩                          鈹?  鈹?鈹? 鈹?                                                    鈹?  鈹?鈹? 鈹?Update() 鈫?UpdateInternal(dt)  鈫?濮嬬粓鎵ц           鈹?  鈹?鈹? 鈹?                                                    鈹?  鈹?鈹? 鈹?鎺у埗鎺ュ彛锛?                                          鈹?  鈹?鈹? 鈹?  Play(clip, fade)     鈫?鎾斁鍔ㄧ敾                   鈹?  鈹?鈹? 鈹?  SetSpeed(scale)      鈫?閫熷害鎺у埗                   鈹?  鈹?鈹? 鈹?  Evaluate(time)       鈫?缂栬緫鍣ㄩ瑙堥噰鏍?            鈹?  鈹?鈹? 鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?  鈹?鈹斺攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹?```

---

**鏂囨。鏃ユ湡**: 2026-02-14
