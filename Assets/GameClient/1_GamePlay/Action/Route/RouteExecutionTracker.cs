using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由执行配额与生命周期追踪器
    /// 宿主于实体的动作控制器 每实体独占私有实例 彻底与静态配置资产解耦
    /// 负责在当前动作生命周期内管理路由及其目标的执行次数 提供耗尽判定与短路防护
    /// </summary>
    public sealed class RouteExecutionTracker
    {
        // 记录每个目标在当前动作周期内已执行的次数
        private readonly Dictionary<RouteExecutionTarget, int> _targetExecutedCounts = new();

        // 记录整条已彻底耗尽配额的路由 供评估阶段极速短路
        private readonly HashSet<ActionRoute> _consumedRoutes = new();

        /// <summary>
        /// 查询指定路由在当前动作周期内是否已彻底耗尽配额
        /// 若已耗尽 评估阶段应直接短路返回假 杜绝无谓的条件判定开销
        /// </summary>
        public bool IsRouteConsumed(ActionRoute route)
        {
            if (route == null) return true;
            if (_consumedRoutes.Contains(route)) return true;

            var targets = route.Targets;
            if (targets == null || targets.Count == 0) return false;

            int validTargetCount = 0;
            bool allExhausted = true;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || !target.IsValid()) continue;

                validTargetCount++;
                if (CanExecuteTarget(target))
                {
                    allExhausted = false;
                    break;
                }
            }

            if (validTargetCount > 0 && allExhausted)
            {
                _consumedRoutes.Add(route);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 查询指定的执行目标在当前动作周期内是否还有剩余配额
        /// </summary>
        public bool CanExecuteTarget(RouteExecutionTarget target)
        {
            if (target == null || !target.IsValid()) return false;
            if (target.MaxExecuteCount <= 0) return true; // 小于等于零代表无限制执行

            _targetExecutedCounts.TryGetValue(target, out int executedCount);
            return executedCount < target.MaxExecuteCount;
        }

        /// <summary>
        /// 记录指定目标执行了一次
        /// </summary>
        public void RecordExecuted(ActionRoute route, RouteExecutionTarget target)
        {
            if (target == null) return;

            _targetExecutedCounts.TryGetValue(target, out int currentCount);
            currentCount++;
            _targetExecutedCounts[target] = currentCount;

            // 检查整条路由是否因此耗尽
            if (route != null && IsRouteConsumed(route))
            {
                _consumedRoutes.Add(route);
            }
        }

        /// <summary>
        /// 显式标记整条路由已耗尽
        /// </summary>
        public void MarkRouteConsumed(ActionRoute route)
        {
            if (route != null)
            {
                _consumedRoutes.Add(route);
            }
        }

        /// <summary>
        /// 获取指定目标的已执行次数
        /// </summary>
        public int GetExecutedCount(RouteExecutionTarget target)
        {
            if (target == null) return 0;
            _targetExecutedCounts.TryGetValue(target, out int count);
            return count;
        }

        /// <summary>
        /// 当切换或重播动作时调用 清空当前动作周期的所有配额记录
        /// </summary>
        public void ResetForNewAction()
        {
            _targetExecutedCounts.Clear();
            _consumedRoutes.Clear();
        }
    }
}
