# 独立动作播放（动画、视听、运动与检测）

Unity 菜单 **Tools → ATEditor → 创建独立动画示例** 会生成一个独立文件夹，含动画、时间轴资产与 Prefab。将 `IndependentAction.prefab` 拖入空场景并进入 Play Mode，方块自动移动；在 ActionDirector Inspector 可暂停、继续、停止和重播。样例不使用 ZZZ 角色、配表或 GameRoot。

菜单 **Tools → ATEditor → 创建独立视听示例** 额外生成粒子 Prefab、材质和 WAV 音频，并加入特效/音频轨道。将 Prefab 拖入带 Camera 与 AudioListener 的默认新场景即可试用。两种菜单都创建唯一目录，不覆盖已有内容。

菜单 **Tools → ATEditor → 创建独立运动示例** 在视听示例上添加连续移动片段，移动的是宿主根节点。运动参数与输入方向由运动端或外部角色控制器（如 MovementComponent / ITransformHandler）提供，ActionDirector 不侵入角色输入或相机状态。

已有角色可直接添加 `ActionDirector`，指定时间轴；宿主或子节点上需要 Animator，动画轨道勾选“主轨道”。代码调用 `director.Play(timeline)`。默认后端独占宿主上的 MAnim 图；现有角色已由其他动画控制器持有图时，请覆盖动画服务。

```csharp
director.ServiceProvider = (type, owner) =>
    type == typeof(IAnimationHandler) ? myAnimationHandler : null;
director.Play(timeline);
```

返回 null 的服务使用默认实现；当前默认提供动画、骨骼挂点、ParticleSystem 特效、AudioSource 音频、位移与根运动。外部服务的更新与释放由调用方负责，Director 不接管其所有权。默认 MAnim 图由 Director 手动推进，在停止、自然结束、禁用时释放。默认后端的中断切换会重建图，目前不提供跨动作淡出。

默认运动模块支持 `ITransformHandler` 与 `IMotionWindowHandler`。连续移动、瞬移和朝向片段可直接使用；默认运动端优先调用宿主上启用的 CharacterController，否则直接修改 Transform。不会添加角色胶囊、重力、寻路或 Rigidbody 控制器；这些运动权威由项目注入实现承担。

勾选 Director 的 `Apply Root Motion` 时，默认 MAnim 的图采样通过 [OnAnimatorMove](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnAnimatorMove.html) 将真实动画增量提交到根运动服务，Animator 可位于宿主子节点。增量已经包含动画采样倍率，消费端不能再次乘 dt 或 Speed。默认模块支持局部 X/Z 过滤、角色碰撞与可选障碍预检测、根旋转轴向/方向/累计角限制及退出对齐。窗口后注册者优先，清理句柄只影响自身；正常退出可以对齐，中断与 Seek 不触发退出吸附。退出混合使用动作时间；自然播放结束时结算尚未完成的对齐目标，停止时丢弃它。

ActionDirector 在片段边界分段采样，先应用当前区间内的动画增量，再处理边界的退出/进入；零时长刷新和 Seek 不提交根运动。已经发生的根位移在停止后保留。原 ActionRunner 的其他调用入口尚未统一采用此分段策略，不能据此宣布所有调度问题已解决。

默认动画后端独占 Animator 的图与根运动回调，不可与现有项目的另一个 OnAnimatorMove 消费者同时驱动同一对象。可注入 `ITransformHandler` 保留默认过滤并接入自定义运动端，或注入 `IMotionWindowHandler` 替换整条运动窗口消费链。外部动画服务需自行提交根运动/驱动时钟，Director 不假定能接管其图。ZZZ 旧 MovementComponent 与启动入口本批未迁移。视觉挤压、视觉反向旋转、视觉偏移恢复等已在 IMotionWindowHandler 形成统一契约。

特效与音源使用 Director 私有对象池，每种资源最多缓存 8 个空闲实例；停止或自然结束会归还本会话的全部对象，重播复用缓存，禁用/销毁会清空缓存。特效已经登记的延迟归还绑定本次借用，旧计时不会回收已复用的对象；片段尾迹按真实粒子存活状态回收，不以单粒子寿命估计整个发射器时长。默认服务直接使用片段中的资源引用，无异步资源加载或协程；YooAsset 等加载策略由外部服务承担。

粒子在每次显示更新中手动模拟，使用真实帧步长及显式表现倍率，逻辑 Tick 不推进粒子；该实现采用 Unity ParticleSystem.Simulate 的模拟后暂停语义，避免 Unity 再推进一次。音频由 Unity DSP 播放，勾选片段“速度同步”时按显式表现倍率调整音调，音调受 AudioSource 的 0–3 范围限制。未勾选时保持原音调。Director 暂停会暂停全部默认音源，并分别记录暂停状态，避免把 Pause 后的 isPlaying=false 当作播放完毕。普通 GameObject 特效可生成和归还，Animator、自定义脚本、VFX Graph 等的内部时间尚不由默认后端接管。

片段结束与会话结束分别处理：`lifeControlByClip=false` 的声音可以越过片段结束，但不会越过 Director 会话结束；粒子尾迹同样以会话为最终所有权边界。因此停止、切换或完成动作会收回尚未结束的声音/尾迹。跨动作尾迹与音频持有策略留待 ActionRuntime 接入，不把资源转交给隐式全局管理器。

`UpdateAutomatically = false` 后由调用方传入 `Tick(dt)`：dt 已属于调用者选定的逻辑时间域，Director 只额外应用动作自身的 `Speed`，不自行累计或生成固定逻辑帧。Process 的 OnUpdate 仍是逻辑回调，显示更新不会执行检测、动作流转或权威运动。

`PresentationTimeScale` 单独声明该逻辑时间域相对真实秒的速率。例如角色时钟已经把 0.5 倍慢动作应用到传入 dt，仍需设置此属性为 0.5，让没有逻辑 Tick 的渲染帧和原生音频也能响应；该属性不会再次乘入逻辑 dt。最终表现速率为 `PresentationTimeScale * Speed`。自动逻辑模式每帧从 Unity 的 timeScale（或未缩放模式的 1）同步此值；ZZZ 新角色入口从实体时钟的倍率变更事件同步，含全局暂停和角色顿帧。

Unity Update 始终以 Time.unscaledDeltaTime 更新默认表现，与是否自动逻辑驱动无关。`UpdatePresentation(realDt)` 是手动显示采样入口，测试或外部宿主使用它时应避免再让同一对象的 Unity Update 推进一次。倍率为 0 时受倍率影响的表现冻结；Pause 同时暂停全部默认音源，包括未勾选速度同步的声音。

默认动画使用同一张 MAnim 图：逻辑求值前恢复权威时间和混合权重，采样本次逻辑姿态并只在此阶段捕获根运动；显示阶段按连续显示时钟求值，临时预测混合权重，不推进逻辑状态清理/事件，不提交根运动。下一次逻辑求值恢复图状态，避免检测读取上一显示帧的骨骼姿态。额外图求值有 CPU 成本，本批没有性能达标结论；显示在逻辑停发但未暂停时会继续，不提供网络预测、回滚或等待策略。

外部覆盖可额外实现 `IPresentationClock`，接收有效倍率和暂停状态；动画 speed、音频 pitch 保持片段局部值，由后端应用倍率一次。未实现此可选接口的旧 Handler 继续收到含表现倍率的动画 speed / 音频 pitch；外部后端仍自行更新和释放，Director 不接管其图或原生循环。

支持 Seek，Seek 不提交根运动和历史命中。`Completed` 仅表示自然完成，`Interrupted` 表示停止或替换。两类通知完成旧会话清理后发出，可以发起下一次播放。片段执行/清理回调中嵌套 Play 返回 false；片段内 Stop 延后到本轮分发结束执行。

目录职责：`Runtime` 保留时间轴核心与服务契约；`Extensions/Unity/Runtime/DefaultHandlers` 集中维护默认能力适配器；`Extensions/Unity/Runtime` 组合默认能力并提供纯粹的播放宿主 ActionDirector。现有 Gameplay 入口继续复用相同的动画与视听后端。

## 通用攻击检测

菜单 **Tools → ATEditor → 创建独立攻击检测示例** 生成独立时间轴与 `IndependentAttack.prefab`。拖入带 Camera 的空场景运行，方块经过两个目标；左上角显示观察计数，可重播。每个目标包含两个碰撞体，但只占一个目标名额；不需要 ZZZ 配表或角色。

在战斗轨道添加 **攻击检测（通用）/AttackClip**，通过 `director.AttackObserved` 消费观察。默认解析器根据 Unity 原生 `attachedRigidbody` 或碰撞体所在根 GameObject 归拢合并受击体，受击对象无需挂载任何额外组件。自身只排除宿主子层级，共用场景父节点的对象仍可被检测。可替换 `IAttackTargetResolver`，返回 null 在目标排序/预算之前过滤；阵营、免疫等政策由项目决定。

```csharp
director.AttackObserved += observation =>
{
    // 几何观察不等于伤害确认；在这里交给项目的战斗系统。
    Debug.Log($"{observation.SourceClipId}: {observation.Target}");
};
director.Play(timeline);
```

`sampleCount=N` 在片段开始及后续 `Duration/N` 间隔采样，共 N 次，不额外在右端点检测。同目标支持窗口内一次、每轮一次和按动作时间冷却；`maxTargets` 是窗口内最多**不同目标**数，已选目标的重复观察仍受重复策略约束。结果携带播放身份、窗口身份、动作 ID、片段 ID、采样轮次、时刻、逻辑目标和代表碰撞体。ContactPoint 使用 Collider.ClosestPoint；查询中心在碰撞体内时可等于中心，Direction 此时为零，不伪造接触法线。

Director 同时在片段与检测采样边界分段，先更新运动/动画姿态，再做物理查询；每轮同步 Transform，查询缓存满载会扩容重试，不固定截断为 64 个碰撞体。暂停与零 dt 不重复采样，Seek 不产生观察，也不补打过去轮次；同一活动窗口内后退 Seek 不重置去重/预算，离开再进入会创建新窗口。循环拥有新窗口身份，重新 Play 拥有新播放身份。回调中 Stop 会阻止本轮剩余通知。直接驱动旧 Runner 的宿主自行控制步长，跳过的历史物理姿态不会被虚假回填。

Sphere/Box/Capsule 使用 Unity 重叠查询；Sector/Ring 使用保守包围体筛选，边缘可有误报，不是网格精确相交或高速武器连续扫掠。范围参数为世界单位，不自动乘宿主缩放。Trigger 默认显式 Collide，也可设 Ignore/UseGlobal。快照分配及每轮物理同步尚未做负载性能验收。

可独立替换 `IAttackDetectionService`、`IAttackTargetResolver`、`IAttackObservationHandler`。外部观察 Handler 替代默认 Director 事件输出；需要同时观察时由外部显式转发。Process 持有检测窗口并在退出、停止、禁用时 Dispose；外部注入服务本身仍归调用方管理。编辑器拖动只显示范围，不产生攻击。

ZZZ 接入示例（放在项目层）：

```csharp
var combat = new Game.GamePlay.ATAttackObservationHandler(observation =>
    ResolveProjectEffect(observation.ActionId, observation.SourceClipId, observation.SampleIndex));
director.ServiceProvider = (type, owner) =>
    type == typeof(IAttackObservationHandler) ? combat :
    type == typeof(IAttackTargetResolver) ? new Game.GamePlay.ATAttackTargetResolver() : null;
```

`ResolveProjectEffect` 是调用项目提供的绑定函数，返回 DetectConfig；无绑定或 hitEffectId<=0 时不执行结算。桥接默认使用 AttackerToTarget 方向，交给既有 ATHitHandler/HitPipeline 处理配表、招架及反馈。新 AttackClip 不存 hitEffectId。旧 HitClip 的字段、GUID、资产与原结算入口保留；其旧物理查询实现尚未迁入此后端，因此不能把新片段的去重/扩容等改进宣称已经覆盖所有旧角色。

后续接入语义输入、动作配置与流转；包含项目专用轨道的旧 ZZZ 时间轴仍需项目服务，目前尚非完整动作系统。
