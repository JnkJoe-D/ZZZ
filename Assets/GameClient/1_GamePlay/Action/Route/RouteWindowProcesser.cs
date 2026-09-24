using System.Collections.Generic;
using ATEditor;
namespace Game.GamePlay
{
    /// <summary>
    /// 路由窗口处理器绑定特性。
    /// 标注在 IRouteWindowProcesser 实现类上，声明其负责处理的 RouteWindow 数据模型类型。
    /// 供 RouteWindowProcessorFactory 自动扫描与预编译委托，杜绝 if/switch 硬编码。
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RouteProcessorBindingAttribute : System.Attribute
    {
        public System.Type WindowType { get; }

        public RouteProcessorBindingAttribute(System.Type windowType)
        {
            if (!typeof(RouteWindow).IsAssignableFrom(windowType))
            {
                throw new System.ArgumentException($"类型 {windowType} 必须继承自 ATEditor.RouteWindow", nameof(windowType));
            }
            WindowType = windowType;
        }
    }

    /// <summary>
    /// 路由窗口处理器工厂。
    /// 
    /// 【设计原则】
    /// 1. 杜绝任何 if/switch 分支分发，满足开闭原则 (OCP)。
    /// 2. 在首次调用时通过反射扫描 [RouteProcessorBinding] 特性，利用 Expression 树预编译强类型构造委托。
    /// 3. 运行期间为 O(1) 字典查找 + 委托直接调用，零反射性能损耗，零 GC 装箱。
    /// </summary>
    public static class RouteWindowProcessorFactory
    {
        private static readonly Dictionary<System.Type, System.Func<RouteWindow, IRouteWindowProcesser>> _creators = new();
        private static bool _isInitialized;

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            var currentAssembly = typeof(RouteWindowProcessorFactory).Assembly;
            var types = currentAssembly.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                System.Type type = types[i];
                if (type.IsAbstract || !typeof(IRouteWindowProcesser).IsAssignableFrom(type))
                    continue;

                var bindingAttr = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<RouteProcessorBindingAttribute>(type);
                if (bindingAttr == null) continue;

                System.Type windowType = bindingAttr.WindowType;
                var ctor = type.GetConstructor(new[] { windowType });
                if (ctor == null)
                {
                    Game.Framework.GLog.Error(Game.Framework.LogTags.Action, $"[RouteWindowProcessorFactory] 处理器 {type.Name} 必须具有包含单个参数 ({windowType.Name}) 的公共构造函数！");
                    continue;
                }

                var paramExp = System.Linq.Expressions.Expression.Parameter(typeof(RouteWindow), "w");
                var castExp = System.Linq.Expressions.Expression.Convert(paramExp, windowType);
                var newExp = System.Linq.Expressions.Expression.New(ctor, castExp);
                var lambdaExp = System.Linq.Expressions.Expression.Lambda<System.Func<RouteWindow, IRouteWindowProcesser>>(newExp, paramExp);

                _creators[windowType] = lambdaExp.Compile();
            }
        }

        public static IRouteWindowProcesser Create(RouteWindow window)
        {
            if (window == null) return null;

            EnsureInitialized();

            System.Type windowType = window.GetType();
            if (_creators.TryGetValue(windowType, out var creator))
            {
                return creator(window);
            }

            Game.Framework.GLog.Error(Game.Framework.LogTags.Action, $"[RouteWindowProcessorFactory] 未找到与窗口数据类型 {windowType.Name} 绑定的 IRouteWindowProcesser！");
            return null;
        }
    }

    public abstract class RouteWindowProcesser<TWindow> :  IRouteWindowProcesser where TWindow : RouteWindow
    {
        protected TWindow _window;
        public RouteWindowProcesser(TWindow window)
        {
            _window = window;
        }
        /// <summary> 窗口打开时调用（由 ActionController.OnComboWindowEnter 驱动）</summary>
        public abstract void OnEnter(WindowProcessContext ctx);

        /// <summary>
        /// 每帧调用（由 ActionController.LogicTick 驱动）。
        /// 默认空实现，子类按需重写（如 AutoRouteWindow 的 EveryFrameInWindow 条件评估）。
        /// </summary>
        public virtual void OnFrameProcess(WindowProcessContext ctx) { }

        /// <summary>
        /// 收到玩家/AI 指令时调用（由 ActionController.OnInput 广播而来）。
        /// 【职责边界】只处理本窗口关心的部分，不越界处理属于其他窗口的指令。
        /// </summary>
        public abstract void OnCommandReceived(CharacterCommand cmd, WindowProcessContext ctx);

        /// <summary>
        /// 窗口关闭时调用（在 ActionController finally 移除窗口之前）。
        /// 各子类在此推入 L2 候选，帧末由 ActionController 统一仲裁。
        /// </summary>
        public abstract void OnExit(WindowProcessContext ctx);

        /// <summary>
        /// 窗口被打断注销时调用（如受击、强切技能打断）。
        /// 【红线约束】必须清理内部捕获状态，绝对不可向 L2 候选池提交任何数据。
        /// </summary>
        public virtual void OnDisable(WindowProcessContext ctx) { }
    }

    // ───────────────────────────────────────────────────────────────────────────
    //  BufferRouteWindowProcesser
    //  执行模式：OnWindowExit（隐式，不可更改）
    //  行为：收到指令时先评估是否满足本窗口路由，通过的作为合法候选存入捕获列表；
    //        窗口关闭时对候选排优先级，将最高优先级候选推入 L2，随后安全清空捕获列表。
    // ───────────────────────────────────────────────────────────────────────────
    [RouteProcessorBinding(typeof(BufferRouteWindow))]
    public class BufferRouteWindowProcesser : RouteWindowProcesser<BufferRouteWindow>
    {
        public BufferRouteWindowProcesser(BufferRouteWindow window) : base(window){}

        // 本地捕获列表：存储通过预评估的有效候选。
        // 【职责边界】仅当指令与本窗口绑定的路由评估通过时才暂存，在窗口关闭时排优推入 L2。
        private readonly List<RouteCandidate> _capture = new();

        public override void OnEnter(WindowProcessContext ctx)
        {
            _capture.Clear();   // 窗口开启：重置捕获列表，清除旧代际遗留数据
        }

        public override void OnCommandReceived(CharacterCommand cmd, WindowProcessContext ctx)
        {
            if (cmd == null) return;

            // 收到指令时先执行评估：只有能匹配本窗口路由的有效指令，才进入捕获列表
            if (ctx.TryResolve(cmd, _window, RouteSingleModifierCheckTiming.OnWindowExit, out var candidate))
            {
                _capture.Add(candidate);
            }
        }

        public override void OnFrameProcess(WindowProcessContext ctx) { }

        public override void OnExit(WindowProcessContext ctx)
        {
            // 窗口关闭时：对所有已通过评估的候选排优先级，选出最高优先级的提交至仲裁器
            if (_capture.Count > 0)
            {
                RouteCandidate best = default;
                bool found = false;

                for (int i = 0; i < _capture.Count; i++)
                {
                    RouteCandidate c = _capture[i];
                    if (!found || RouteResolver.IsHigherPriority(c, best))
                    {
                        best = c;
                        found = true;
                    }
                }

                if (found)
                {
                    ctx.Arbitrator?.Submit(best);
                }

                _capture.Clear();
            }
        }

        public override void OnDisable(WindowProcessContext ctx)
        {
            // 打断时只清空捕获引用，绝不提交至仲裁器
            _capture.Clear();
        }
    }
    // ───────────────────────────────────────────────────────────────────────────
    //  ExecuteRouteWindowProcesser
    //  执行模式：EveryFrameInWindow（Instant，隐式，不可更改）
    //  行为：收到指令立即评估，匹配则提交仲裁器，不匹配直接丢弃；不缓存任何指令。
    // ───────────────────────────────────────────────────────────────────────────
    [RouteProcessorBinding(typeof(ExecuteRouteWindow))]
    public class ExecuteRouteWindowProcesser : RouteWindowProcesser<ExecuteRouteWindow>
    {
        public ExecuteRouteWindowProcesser(ExecuteRouteWindow window) : base(window){}

        public override void OnEnter(WindowProcessContext ctx) { }
        public override void OnFrameProcess(WindowProcessContext ctx) { }

        public override void OnCommandReceived(CharacterCommand cmd, WindowProcessContext ctx)
        {
            if (ctx.TryResolve(cmd, _window, RouteSingleModifierCheckTiming.EveryFrameInWindow, out var candidate))
            {
                ctx.Arbitrator?.Submit(candidate);
            }
        }

        public override void OnExit(WindowProcessContext ctx) { }
    }
    // ───────────────────────────────────────────────────────────────────────────
    //  AutoRouteWindowProcesser
    //  行为：不响应任何指令；在各个生命周期节点（Enter / FrameProcess / Exit）分别以对应的
    //        RouteSingleModifierCheckTiming 对无指令条件路由（AutoTransitionTrigger / ConditionOnlyTrigger）做评估并提交仲裁器。
    // ───────────────────────────────────────────────────────────────────────────
    [RouteProcessorBinding(typeof(AutoRouteWindow))]
    public class AutoRouteWindowProcesser : RouteWindowProcesser<AutoRouteWindow>
    {
        public AutoRouteWindowProcesser(AutoRouteWindow window) : base(window){}

        public override void OnEnter(WindowProcessContext ctx)
        {
            EvalAndPush(ctx, RouteSingleModifierCheckTiming.OnWindowEnter);
        }

        public override void OnFrameProcess(WindowProcessContext ctx)
        {
            EvalAndPush(ctx, RouteSingleModifierCheckTiming.EveryFrameInWindow);
        }

        // Auto 窗口不响应任何 CharacterCommand
        public override void OnCommandReceived(CharacterCommand cmd, WindowProcessContext ctx) { }

        public override void OnExit(WindowProcessContext ctx)
        {
            EvalAndPush(ctx, RouteSingleModifierCheckTiming.OnWindowExit);
        }

        private void EvalAndPush(WindowProcessContext ctx, RouteSingleModifierCheckTiming timing)
        {
            // AutoTransitionTrigger / ConditionOnlyTrigger 要求 command == null
            if (ctx.TryResolve(null, _window, timing, out var candidate))
            {
                ctx.Arbitrator?.Submit(candidate);
            }
        }
    }
}