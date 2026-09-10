using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 全局事件中心
    /// 
    /// 设计原则：
    ///   - 以 C# Type 作为 Key，完全类型安全，无字符串魔法值
    ///   - 事件结构体（struct）零堆分配，不产生 GC
    ///   - 支持同步发布和延迟发布（下一帧执行）
    ///   - 支持优先级订阅（高优先级先执行）
    ///   - 遍历时安全处理 Subscribe/Unsubscribe（拷贝后迭代）
    /// </summary>
    public static class EventCenter
    {
        // ── 内部处理器容器（非泛型基类，用于存入字典）
        private abstract class HandlerList
        {
            public abstract void Clear();
        }

        // ── 泛型处理器容器
        private class HandlerList<T> : HandlerList where T : IGameEvent
        {
            // 按优先级降序排列：数字大的先执行
            public readonly List<(int priority, Action<T> handler)> Handlers
                = new List<(int, Action<T>)>();

            public override void Clear() => Handlers.Clear();
        }

        // ── 注册表：Type → HandlerList
        private static readonly Dictionary<Type, HandlerList> _registry
            = new Dictionary<Type, HandlerList>();

        // ── 延迟发布与修改队列
        private static readonly Queue<Action> _pendingEvents = new Queue<Action>();
        private static readonly Queue<Action> _pendingModifications = new Queue<Action>();
        private static int _publishDepth = 0;

        // ────────────────────────────────────────
        // 订阅 / 取消订阅
        // ────────────────────────────────────────

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型（必须实现 IGameEvent）</typeparam>
        /// <param name="handler">处理函数</param>
        /// <param name="priority">优先级，数值越大越先执行，默认 0</param>
        public static void Subscribe<T>(Action<T> handler, int priority = 0) where T : IGameEvent
        {
            if (handler == null) return;

            if (_publishDepth > 0)
            {
                _pendingModifications.Enqueue(() => Subscribe(handler, priority));
                return;
            }

            var type = typeof(T);
            if (!_registry.TryGetValue(type, out var list))
            {
                list = new HandlerList<T>();
                _registry[type] = list;
            }

            var typedList = (HandlerList<T>)list;

            // 重复订阅检测
            for (int i = 0; i < typedList.Handlers.Count; i++)
            {
                if (typedList.Handlers[i].handler == handler)
                {
                    Debug.LogWarning($"[EventCenter] 重复订阅事件 {typeof(T).Name}，已忽略");
                    return;
                }
            }

            typedList.Handlers.Add((priority, handler));

            // 按优先级降序排序
            typedList.Handlers.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            if (_publishDepth > 0)
            {
                _pendingModifications.Enqueue(() => Unsubscribe(handler));
                return;
            }

            var type = typeof(T);
            if (!_registry.TryGetValue(type, out var list)) return;

            var typedList = (HandlerList<T>)list;
            typedList.Handlers.RemoveAll(pair => pair.handler == handler);
        }

        // ────────────────────────────────────────
        // 发布
        // ────────────────────────────────────────

        /// <summary>
        /// 同步发布事件（立即调用所有订阅者，完全零堆分配）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件数据</param>
        public static void Publish<T>(T evt) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_registry.TryGetValue(type, out var list)) return;

            var typedList = (HandlerList<T>)list;
            int count = typedList.Handlers.Count;
            if (count == 0) return;

            _publishDepth++;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        typedList.Handlers[i].handler?.Invoke(evt);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventCenter] 事件处理异常 [{typeof(T).Name}]: {e}");
                    }
                }
            }
            finally
            {
                _publishDepth--;
                if (_publishDepth == 0)
                {
                    while (_pendingModifications.Count > 0)
                    {
                        _pendingModifications.Dequeue()?.Invoke();
                    }

                    // 处理发布期间积压的延迟事件
                    FlushPending();
                }
            }
        }

        /// <summary>
        /// 延迟发布：将事件推入队列，在下一次 FlushPending 时（通常下一帧）执行
        /// 适用于在事件回调内部再次发出事件，避免重入问题
        /// </summary>
        public static void PublishDeferred<T>(T evt) where T : IGameEvent
        {
            _pendingEvents.Enqueue(() => Publish(evt));
        }

        /// <summary>
        /// 刷新延迟事件队列（由 GameRoot 在每帧 Update 中调用）
        /// </summary>
        public static void FlushPending()
        {
            if (_publishDepth > 0) return;

            while (_pendingEvents.Count > 0)
            {
                var action = _pendingEvents.Dequeue();
                action?.Invoke();
            }
        }

        // ────────────────────────────────────────
        // 生命周期
        // ────────────────────────────────────────

        /// <summary>
        /// 清除指定类型的所有订阅
        /// </summary>
        public static void ClearEvent<T>() where T : IGameEvent
        {
            var type = typeof(T);
            if (_registry.TryGetValue(type, out var list))
            {
                list.Clear();
            }
        }

        /// <summary>
        /// 清除所有订阅和待处理事件（通常在场景切换时调用）
        /// </summary>
        public static void ClearAll()
        {
            foreach (var list in _registry.Values)
            {
                list.Clear();
            }
            _registry.Clear();
            _pendingEvents.Clear();
            _pendingModifications.Clear();
            _publishDepth = 0;
        }

        // ────────────────────────────────────────
        // 调试
        // ────────────────────────────────────────

        /// <summary>
        /// 获取当前所有事件类型及订阅数量（调试用）
        /// </summary>
        public static string GetStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== EventCenter Statistics ===");
            foreach (var kvp in _registry)
            {
                // 通过反射获取 Handlers.Count —— 仅用于调试，不影响运行时
                var handlersField = kvp.Value.GetType().GetField("Handlers");
                if (handlersField != null)
                {
                    var handlers = handlersField.GetValue(kvp.Value) as System.Collections.IList;
                    sb.AppendLine($"  [{kvp.Key.Name}] {handlers?.Count ?? 0} subscriber(s)");
                }
            }
            sb.AppendLine($"Pending Events: {_pendingEvents.Count}");
            return sb.ToString();
        }
    }
}
