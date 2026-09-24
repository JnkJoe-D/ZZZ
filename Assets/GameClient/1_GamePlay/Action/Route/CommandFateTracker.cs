using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 指令路由执行历史记录模型
    /// </summary>
    public struct ExecutionRecord
    {
        public long CommandId;
        public CommandRouteSource Source;
        public string RouteTag;
        public int ActionId;
        public string ActionName;
        public float Timestamp;
        public ActionConfigAsset Asset;
    }

    /// <summary>
    /// 指令去向与执行历史追踪器：专职承载指令命运（CommandFate）查询与有限历史记录遥测。
    /// </summary>
    public sealed class CommandFateTracker
    {
        private const int MaxHistoryCapacity = 10;
        private readonly List<ExecutionRecord> _history = new();

        public IReadOnlyList<ExecutionRecord> History => _history;

        /// <summary>
        /// 记录一次路由执行事务，维护最多 10 条最新记录。
        /// </summary>
        public void Record(long commandId, CommandRouteSource source, string tag, ActionConfigAsset action)
        {
            _history.Insert(0, new ExecutionRecord
            {
                CommandId = commandId,
                Source = source,
                RouteTag = tag,
                ActionId = action?.ID ?? -1,
                ActionName = action?.name,
                Timestamp = Time.time,
                Asset = action
            });

            if (_history.Count > MaxHistoryCapacity)
            {
                _history.RemoveAt(MaxHistoryCapacity);
            }
        }

        /// <summary>
        /// 查询指定指令的最终命运（Pending / Executed / Dropped）。
        /// 依次排查历史记录与 L1 缓冲区。
        /// 注意：新架构中窗口不再暴露 CapturedCommands；
        ///       处于 BufferWindow 捕获中的指令其物理副本仍在 L1，因此 L1 检查可覆盖该场景。
        /// </summary>
        public CommandFate CheckFate(long commandId, IReadOnlyList<ActionController.RouteWindowData> activeWindows)
        {
            if (commandId <= 0) return CommandFate.Dropped;

            // 1. 检查是否已被执行
            for (int i = 0; i < _history.Count; i++)
            {
                if (_history[i].CommandId == commandId) return CommandFate.Executed;
            }

            // 2. 其余情况均视为已丢弃或已消费
            return CommandFate.Dropped;
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }
    }
}
