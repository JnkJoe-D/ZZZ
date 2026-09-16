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
        /// 依次排查历史记录、缓冲区与活跃窗口。
        /// </summary>
        public CommandFate CheckFate(long commandId, CommandBuffer buffer, IReadOnlyList<ActionController.RouteWindowData> activeWindows)
        {
            if (commandId <= 0) return CommandFate.Dropped;

            // 1. 检查是否已被执行
            for (int i = 0; i < _history.Count; i++)
            {
                if (_history[i].CommandId == commandId) return CommandFate.Executed;
            }

            // 2. 检查是否仍在 CommandBuffer 中挂起
            if (buffer != null)
            {
                List<CharacterCommand> unconsumed = buffer.GetUnconsumedCommands();
                for (int i = 0; i < unconsumed.Count; i++)
                {
                    CharacterCommand cmd = unconsumed[i];
                    if (cmd.Id == commandId && !cmd.IsConsumed) return CommandFate.Pending;
                }
            }

            // 3. 检查是否已被某个活跃窗口捕获等待退出裁决
            if (activeWindows != null)
            {
                for (int i = 0; i < activeWindows.Count; i++)
                {
                    List<CharacterCommand> captured = activeWindows[i].CapturedCommands;
                    if (captured == null) continue;

                    for (int j = 0; j < captured.Count; j++)
                    {
                        CharacterCommand cmd = captured[j];
                        if (cmd.Id == commandId && !cmd.IsConsumed) return CommandFate.Pending;
                    }
                }
            }

            // 4. 其余情况均视为已丢弃
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
