using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由仲裁器（纯领域无状态候选池）。
    /// 
    /// 【职责契约】
    /// 1. 窗口候选汇集：各 RouteWindowProcesser 在其生命周期内（OnCommandReceived / OnExit / OnFrameProcess）
    ///    将评估通过的有效路由候选通过 Submit 提交至此。
    /// 2. 帧末全局仲裁：帧末由 ActionController.LogicTick 调用 TryResolveBest 选出全局唯一最高优先级的候选人执行。
    /// 3. 生命周期安全：动作切换或受击打断时调用 Clear，彻底阻断旧动作残留。
    /// </summary>
    public class RouteArbitrator
    {
        private readonly List<RouteCandidate> _candidates = new();

        /// <summary>
        /// 窗口将评估通过的候选提交至仲裁池。
        /// </summary>
        public void Submit(RouteCandidate candidate)
        {
            _candidates.Add(candidate);
        }

        /// <summary>
        /// 帧末统一仲裁：取优先级最高的一项并清空池子。
        /// 优先级相同时，后按键提交的候选优先（依 BufferOrder 决选）。
        /// </summary>
        public bool TryResolveBest(out RouteCandidate best)
        {
            best = default;
            if (_candidates.Count == 0) return false;

            best = _candidates[0];
            for (int i = 1; i < _candidates.Count; i++)
            {
                if (RouteResolver.IsHigherPriority(_candidates[i], best))
                    best = _candidates[i];
            }
            _candidates.Clear();
            return true;
        }

        /// <summary>
        /// 动作切换或重置时清空仲裁池中的所有候选。
        /// </summary>
        public void Clear()
        {
            _candidates.Clear();
        }
    }
}
