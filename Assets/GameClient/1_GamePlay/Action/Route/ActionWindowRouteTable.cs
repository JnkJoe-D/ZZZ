using System;
using System.Collections.Generic;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由窗口复合查找键：唯一标识 (窗口类型 + 窗口标签 Tag)。
    /// 内存仅占 16 字节，纯结构体零 GC 分配。
    /// </summary>
    public readonly struct RouteWindowKey : IEquatable<RouteWindowKey>
    {
        public readonly Type WindowType;
        public readonly string Tag;

        public RouteWindowKey(RouteWindow window)
        {
            if (window == null)
            {
                WindowType = null;
                Tag = string.Empty;
            }
            else
            {
                WindowType = window.GetType();
                Tag = window.Tag ?? string.Empty;
            }
        }

        public RouteWindowKey(Type windowType, string tag)
        {
            WindowType = windowType;
            Tag = tag ?? string.Empty;
        }

        /// <summary> 全局/无窗口键：表示未挂载任何特定窗口的全局事件/直接切换路由 </summary>
        public static readonly RouteWindowKey Global = new(null, string.Empty);

        public bool IsGlobal => WindowType == null;

        public bool Equals(RouteWindowKey other)
        {
            return WindowType == other.WindowType && string.Equals(Tag, other.Tag, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is RouteWindowKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((WindowType != null ? WindowType.GetHashCode() : 0) * 397)
                    ^ (Tag != null ? StringComparer.Ordinal.GetHashCode(Tag) : 0);
            }
        }

        public override string ToString() => IsGlobal ? "[Global]" : $"[{WindowType?.Name}] {Tag}";
    }

    /// <summary>
    /// 单个动作的窗口路由分桶快照表。
    /// 预加载阶段一次性构建固化，只读安全，零 GC 分配。
    /// </summary>
    public sealed class ActionWindowRouteTable
    {
        private static readonly ActionRoute[] EmptyRoutes = Array.Empty<ActionRoute>();

        private readonly Dictionary<RouteWindowKey, ActionRoute[]> _windowBuckets = new();
        private ActionRoute[] _unmappedRoutes = EmptyRoutes;
        private ActionRoute[] _allRoutes = EmptyRoutes;

        public ActionRoute[] UnmappedRoutes => _unmappedRoutes;
        public ActionRoute[] GlobalRoutes => _unmappedRoutes;
        public ActionRoute[] AllRoutes => _allRoutes;

        public void SetBucket(RouteWindowKey key, ActionRoute[] routes)
        {
            if (key.IsGlobal)
            {
                _unmappedRoutes = routes ?? EmptyRoutes;
            }
            else
            {
                _windowBuckets[key] = routes ?? EmptyRoutes;
            }
        }

        public void SetAllRoutes(ActionRoute[] routes)
        {
            _allRoutes = routes ?? EmptyRoutes;
        }

        /// <summary>
        /// O(1) 获取指定窗口绑定的专属路由切片（严格遵守 100% 窗口驱动：若窗口为 null 或无效则返回空数组）
        /// </summary>
        public IReadOnlyList<ActionRoute> GetRoutesForWindow(RouteWindow window)
        {
            if (window == null || !window.IsValid) return EmptyRoutes;
            var key = new RouteWindowKey(window);
            return _windowBuckets.TryGetValue(key, out var list) ? list : EmptyRoutes;
        }

        public IReadOnlyList<ActionRoute> GetRoutesForWindowKey(in RouteWindowKey key)
        {
            if (key.IsGlobal) return EmptyRoutes;
            return _windowBuckets.TryGetValue(key, out var list) ? list : EmptyRoutes;
        }
    }

    /// <summary>
    /// 触发器窗口提取扩展工具
    /// </summary>
    public static class RouteTriggerExtensions
    {
        public static RouteWindow GetRequiredWindow(this IRouteTrigger trigger)
        {
            return trigger switch
            {
                IntentCommandTrigger intent => intent.RequiredWindow,
                AutoTransitionTrigger auto => auto.RequiredWindow,
                ConditionOnlyTrigger cond => cond.RequiredWindow,
                DirectAssetTrigger direct => direct.RequiredWindow,
                SystemEventTrigger sys => sys.RequiredWindow,
                _ => null
            };
        }
    }

    /// <summary>
    /// 动作路由分桶构建器
    /// </summary>
    public static class ActionRouteTableBuilder
    {
        public static ActionWindowRouteTable BuildTable(ActionConfigAsset action, CharacterEntity actor = null)
        {
            var table = new ActionWindowRouteTable();
            if (action == null) return table;

            var effectiveRoutes = new List<ActionRoute>();
            action.CollectEffectiveRoutes(effectiveRoutes, actor);

            var bucketMap = new Dictionary<RouteWindowKey, List<ActionRoute>>();
            var globalList = new List<ActionRoute>();
            var allList = new List<ActionRoute>();

            for (int i = 0; i < effectiveRoutes.Count; i++)
            {
                ActionRoute route = effectiveRoutes[i];
                if (route == null || !route.IsValid()) continue;

                allList.Add(route);
                RouteWindow reqWindow = route.TriggerStrategy?.GetRequiredWindow();

                if (reqWindow == null || !reqWindow.IsValid)
                {
                    globalList.Add(route);
                }
                else
                {
                    var key = new RouteWindowKey(reqWindow);
                    if (!bucketMap.TryGetValue(key, out var list))
                    {
                        list = new List<ActionRoute>();
                        bucketMap[key] = list;
                    }
                    list.Add(route);
                }
            }

            table.SetBucket(RouteWindowKey.Global, globalList.ToArray());
            table.SetAllRoutes(allList.ToArray());
            foreach (var kvp in bucketMap)
            {
                table.SetBucket(kvp.Key, kvp.Value.ToArray());
            }

            return table;
        }
    }

    /// <summary>
    /// 运行时动态窗口路由解析算子：融合预加载静态窗口分桶与动态 InheritMode
    /// </summary>
    public static class ActionRouteRuntimeResolver
    {
        public static void ResolveEffectiveWindowRoutes(
            ActionConfigAsset currentAction,
            RouteWindow activeWindow,
            CharacterEntity actor,
            List<ActionRoute> outResults)
        {
            if (outResults == null) return;
            outResults.Clear();
            if (currentAction == null) return;

            var currentTable = currentAction.GetWindowRouteTable(actor);
            var selfWindowRoutes = currentTable.GetRoutesForWindow(activeWindow);

            // 检查动态继承
            if (currentAction is RoleActionConfigAsset roleAction &&
                roleAction.InheritMode != RouteInheritMode.None &&
                actor?.ActionController != null)
            {
                var history = actor.ActionController.ExecutionHistory;
                if (history != null && history.Count >= 2 && history[1].Asset is ActionConfigAsset prevAction)
                {
                    var prevTable = prevAction.GetWindowRouteTable(actor);
                    var prevWindowRoutes = prevTable.GetRoutesForWindow(activeWindow);

                    bool inheritedFirst = roleAction.InheritMode == RouteInheritMode.InheritPrioritizeInherited;
                    bool overrideSelf = roleAction.InheritMode == RouteInheritMode.InheritAndOverrideSelf;

                    if (inheritedFirst)
                    {
                        AppendInheritedRoutes(prevWindowRoutes, currentAction.ID, outResults);
                        if (!overrideSelf) AppendRoutes(selfWindowRoutes, outResults);
                    }
                    else
                    {
                        if (!overrideSelf) AppendRoutes(selfWindowRoutes, outResults);
                        AppendInheritedRoutes(prevWindowRoutes, currentAction.ID, outResults);
                    }
                    return;
                }
            }

            AppendRoutes(selfWindowRoutes, outResults);
        }

        private static void AppendRoutes(IReadOnlyList<ActionRoute> source, List<ActionRoute> target)
        {
            if (source == null || target == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) target.Add(source[i]);
            }
        }

        private static void AppendInheritedRoutes(IReadOnlyList<ActionRoute> source, int currentActionId, List<ActionRoute> target)
        {
            if (source == null || target == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                var r = source[i];
                if (r != null && !(r.ExecuteType == ExecuteTarget.Action && r.ExecuteAction != null && r.ExecuteAction.ID == currentActionId))
                {
                    target.Add(r);
                }
            }
        }
    }
}
