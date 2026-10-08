using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 目标动作的转移类型枚举
    /// </summary>
    public enum TransitionTargetKind
    {
        /// <summary> 普通分支路由（通过自身或指令集的按键/事件派生） </summary>
        RouteBranch = 10,

        /// <summary> 自循环（动作自身为 Loop） </summary>
        SelfLoop = 20,

        /// <summary> 自然顺承动作（CompleteMode 为 TransitToAction 时配置的后续动作） </summary>
        CompleteAction = 30
    }

    /// <summary>
    /// 提取出的目标动作元数据描述
    /// </summary>
    public class TransitionTargetInfo
    {
        /// <summary> 目标动作资产引用 </summary>
        public ActionConfigAsset TargetAction { get; set; }

        /// <summary> 转移类型 </summary>
        public TransitionTargetKind Kind { get; set; }

        /// <summary> 对应路由中的最高优先级（数值越大优先级越高） </summary>
        public int MaxPriority { get; set; }

        /// <summary> 来源通道说明（用于 UI 提示，例如“私有路由 [P:100]”、“指令集 [通用闪避]”） </summary>
        public string SourceDescription { get; set; }

        /// <summary> 关联的匹配路由（若为普通分支路由，可用于获取 RequiredWindowTag） </summary>
        public ActionRoute MatchedRoute { get; set; }

        /// <summary> 当前是否已在源动作的 TransitionTable 中个性化配置过 </summary>
        public bool IsConfiguredInTable { get; set; }

        /// <summary> 已配置的混合时间（未配置则为 -1） </summary>
        public float ConfiguredCrossfade { get; set; } = -1f;

        /// <summary> 是否配置了自定义退出时间点 </summary>
        public bool HasCustomExitTime { get; set; }

        /// <summary> 已配置的自定义退出时间点 </summary>
        public float CustomExitTime { get; set; }

        /// <summary> 是否配置了自定义切入起始时间点 </summary>
        public bool HasStartTime { get; set; }

        /// <summary> 已配置的切入起始时间点 </summary>
        public float StartTime { get; set; }

        public bool HasEndTime { get => HasCustomExitTime; set => HasCustomExitTime = value; }
        public float EndTime { get => CustomExitTime; set => CustomExitTime = value; }
        public float BlendDuration { get => ConfiguredCrossfade; set => ConfiguredCrossfade = value; }
    }

    /// <summary>
    /// 动作过渡拓扑关系扫描与目标提取服务
    /// </summary>
    public static class ActionTransitionTopologyService
    {
        /// <summary>
        /// 获取动作资产的安全显示名称（优先使用 Name 字段，若为空回退到 Unity 资产名）
        /// </summary>
        public static string GetActionDisplayName(ActionConfigAsset action)
        {
            if (action == null) return "(None)";
            if (!string.IsNullOrEmpty(action.Name)) return action.Name;
            return !string.IsNullOrEmpty(action.name) ? action.name : "(None)";
        }

        /// <summary>
        /// 扫描并整理源动作的所有合法转移目标白名单。
        /// 排序契约：
        ///   1. 普通分支路由（按目标动作名称字母序排列，相同前缀动作自然聚类）；
        ///   2. Loop 自循环（若当前动作为循环动作，排倒数第二）；
        ///   3. CompleteAction 自然顺承目标（排最后）。
        /// </summary>
        public static List<TransitionTargetInfo> DiscoverTargets(ActionConfigAsset sourceAction)
        {
            var result = new List<TransitionTargetInfo>();
            if (sourceAction == null) return result;

            // 用于去重与聚合最高优先级：Key = 目标 Action
            var branchTargets = new Dictionary<ActionConfigAsset, (int maxPriority, List<string> sources, ActionRoute bestRoute)>();

            // 1. 扫描自身私有路由
            if (sourceAction.Routes != null)
            {
                foreach (var route in sourceAction.Routes)
                {
                    if (route != null && route.ExecuteType == ExecuteTarget.Action && route.ExecuteAction != null)
                    {
                        var target = route.ExecuteAction;
                        int priority = route.Priority;
                        string desc = $"私有路由 [P:{priority}]";

                        if (!branchTargets.TryGetValue(target, out var entry))
                        {
                            branchTargets[target] = (priority, new List<string> { desc }, route);
                        }
                        else
                        {
                            ActionRoute best = entry.bestRoute;
                            int newMax = entry.maxPriority;
                            if (priority > entry.maxPriority)
                            {
                                newMax = priority;
                                best = route;
                            }
                            entry.sources.Add(desc);
                            branchTargets[target] = (newMax, entry.sources, best);
                        }
                    }
                }
            }

            // 2. 扫描引用的通用指令集 (RouteSets)
            if (sourceAction.RouteSets != null)
            {
                var tempRoutes = new List<ActionRoute>();
                foreach (var set in sourceAction.RouteSets)
                {
                    if (set == null) continue;
                    tempRoutes.Clear();
                    set.AppendRoutes(tempRoutes);

                    string setName = !string.IsNullOrEmpty(set.name) ? set.name : "RouteSet";
                    foreach (var route in tempRoutes)
                    {
                        if (route != null && route.ExecuteType == ExecuteTarget.Action && route.ExecuteAction != null)
                        {
                            var target = route.ExecuteAction;
                            int priority = route.Priority;
                            string desc = $"指令集 [{setName}] [P:{priority}]";

                            if (!branchTargets.TryGetValue(target, out var entry))
                            {
                                branchTargets[target] = (priority, new List<string> { desc }, route);
                            }
                            else
                            {
                                ActionRoute best = entry.bestRoute;
                                int newMax = entry.maxPriority;
                                if (priority > entry.maxPriority)
                                {
                                    newMax = priority;
                                    best = route;
                                }
                                entry.sources.Add(desc);
                                branchTargets[target] = (newMax, entry.sources, best);
                            }
                        }
                    }
                }
            }

            // 3. 构建普通路由分支列表，并按 MaxPriority 降序排序
            var branchList = new List<TransitionTargetInfo>();
            foreach (var kvp in branchTargets)
            {
                var target = kvp.Key;
                // 排除指向自身或 CompleteAction 的重复项（它们有专属位置）
                if (target == sourceAction) continue;
                if (sourceAction.CompleteMode == ActionCompleteMode.TransitToAction && target == sourceAction.CompleteAction) continue;

                var existingTransition = sourceAction.GetTransition(target);
                branchList.Add(new TransitionTargetInfo
                {
                    TargetAction = target,
                    Kind = TransitionTargetKind.RouteBranch,
                    MaxPriority = kvp.Value.maxPriority,
                    MatchedRoute = kvp.Value.bestRoute,
                    SourceDescription = string.Join(", ", kvp.Value.sources),
                    IsConfiguredInTable = existingTransition != null,
                    ConfiguredCrossfade = existingTransition?.BlendDuration ?? -1f,
                    HasCustomExitTime = existingTransition?.HasEndTime ?? false,
                    CustomExitTime = existingTransition?.EndTime ?? 0f,
                    HasStartTime = existingTransition?.HasStartTime ?? false,
                    StartTime = existingTransition?.StartTime ?? 0f
                });
            }

            // 分支路由按目标动作名称字母序排序，相同前缀动作（如 Attack、Evade）自然聚类
            branchList.Sort((a, b) =>
            {
                string nameA = GetActionDisplayName(a.TargetAction);
                string nameB = GetActionDisplayName(b.TargetAction);
                return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
            });

            result.AddRange(branchList);

            // 4. 检查是否为循环动作 (Loop 自循环排倒数第二)
            bool isLoopAction = CheckIsLoopAction(sourceAction);
            if (isLoopAction)
            {
                var existingTransition = sourceAction.GetTransition(sourceAction);
                result.Add(new TransitionTargetInfo
                {
                    TargetAction = sourceAction,
                    Kind = TransitionTargetKind.SelfLoop,
                    MaxPriority = int.MinValue + 1, // 象征性保底低优先级
                    SourceDescription = "动作自循环 (Loop Transition)",
                    IsConfiguredInTable = existingTransition != null,
                    ConfiguredCrossfade = existingTransition?.BlendDuration ?? -1f,
                    HasCustomExitTime = existingTransition?.HasEndTime ?? false,
                    CustomExitTime = existingTransition?.EndTime ?? 0f,
                    HasStartTime = existingTransition?.HasStartTime ?? false,
                    StartTime = existingTransition?.StartTime ?? 0f
                });
            }

            // 5. 检查 CompleteAction 自然顺承目标 (排最后)
            if (sourceAction.CompleteMode == ActionCompleteMode.TransitToAction && sourceAction.CompleteAction != null)
            {
                var completeTarget = sourceAction.CompleteAction;
                var existingTransition = sourceAction.GetTransition(completeTarget);
                result.Add(new TransitionTargetInfo
                {
                    TargetAction = completeTarget,
                    Kind = TransitionTargetKind.CompleteAction,
                    MaxPriority = int.MinValue, // 排在最末尾
                    SourceDescription = "自然完成顺承 (Complete Transition)",
                    IsConfiguredInTable = existingTransition != null,
                    ConfiguredCrossfade = existingTransition?.BlendDuration ?? -1f,
                    HasCustomExitTime = existingTransition?.HasEndTime ?? false,
                    CustomExitTime = existingTransition?.EndTime ?? 0f,
                    HasStartTime = existingTransition?.HasStartTime ?? false,
                    StartTime = existingTransition?.StartTime ?? 0f
                });
            }

            return result;
        }

        private static bool CheckIsLoopAction(ActionConfigAsset action)
        {
            if (action == null) return false;
            if (action.actionTimelineSO != null && action.actionTimelineSO.isLoop)
            {
                return true;
            }
            if (action.CompleteMode == ActionCompleteMode.Stay)
            {
                return true;
            }
            return false;
        }
    }
}
