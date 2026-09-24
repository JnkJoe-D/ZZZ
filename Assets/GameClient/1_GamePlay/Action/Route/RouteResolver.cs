using System;
using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由评估候选项数据模型
    /// </summary>
    public struct RouteCandidate
    {
        public CharacterCommand Command;
        public ActionConfigAsset NextAction;
        public ExecuteEvent RouteExecuteEvent;
        public ExecuteTarget ExecuteType;
        public int Priority;
        public ATEditor.RouteWindow RouteWindow;
        public string RouteTag;
        public ActionRoute SourceRoute;
    }

    /// <summary>
    /// 纯领域路由仲裁算子：无状态、无 Unity 引擎副作用，专职执行路由候选人过滤与最高优先级决选。
    /// </summary>
    public static class RouteResolver
    {
        /// <summary>
        /// 在给定的路由集合中，评估并挑选满足输入、窗口、修饰条件且优先级最高的候选人。
        /// </summary>
        public static bool TryResolve(
            IReadOnlyList<ActionRoute> routes,
            CharacterCommand command,
            ATEditor.RouteWindow activeWindow,
            CharacterEntity actor,
            ISkillCostHandler skillHandler,
            RouteSingleModifierCheckTiming timing,
            out RouteCandidate best)
        {
            best = default;
            if (routes == null || routes.Count == 0) return false;

            bool found = false;

            for (int i = 0; i < routes.Count; i++)
            {
                ActionRoute route = routes[i];
                if (route == null) continue;
                if (!route.IsValid()) continue;

                if (!route.Evaluate(command, activeWindow, actor, skillHandler, timing))
                    continue;

                var candidate = new RouteCandidate
                {
                    Command = command,
                    NextAction = route.ExecuteAction,
                    RouteExecuteEvent = route.RouteExecuteEvent,
                    ExecuteType = route.ExecuteType,
                    Priority = route.Priority,
                    RouteWindow = activeWindow,
                    RouteTag = activeWindow?.Tag,
                    SourceRoute = route
                };

                if (!found || IsHigherPriority(candidate, best))
                {
                    best = candidate;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// 比对两名候选人的优先级：数值越大优先级越高；同优先级时遵循缓冲区时间序（BufferOrder 越大越新）。
        /// </summary>
        public static bool IsHigherPriority(RouteCandidate a, RouteCandidate b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            long orderA = a.Command?.BufferOrder ?? 0L;
            long orderB = b.Command?.BufferOrder ?? 0L;
            return orderA > orderB;
        }
    }
}
