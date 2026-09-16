using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// 消息分发器
    ///
    /// 职责：
    ///   1. 注册 MsgId 与 Handler 的映射（泛型自动反序列化 Protobuf）
    ///   2. 主线程调用 Dispatch 时根据 MsgId 查找并执行对应回调
    ///   3. 支持注册/取消注册，支持多个 Handler
    ///
    /// 使用示例：
    ///   dispatcher.Register<S2C_Login>(MsgId.Login, OnLoginResponse);
    ///   dispatcher.Dispatch(msgId, payload);
    /// </summary>
    public class MessageDispatcher
    {
        /// <summary>非泛型基类，用于统一存储</summary>
        private abstract class HandlerEntry
        {
            public abstract void Invoke(byte[] payload);
        }

        /// <summary>泛型实现，自动 Protobuf 反序列化</summary>
        private class HandlerEntry<T> : HandlerEntry where T : IMessage<T>, new()
        {
            private static readonly MessageParser<T> Parser = new(() => new T());
            private readonly Action<T> _callback;

            public HandlerEntry(Action<T> callback)
            {
                _callback = callback;
            }

            public override void Invoke(byte[] payload)
            {
                var msg = Parser.ParseFrom(payload);
                _callback?.Invoke(msg);
            }

            public bool MatchCallback(Action<T> callback) => _callback == callback;
        }

        /// <summary>原始字节回调（不做 Protobuf 解析）</summary>
        private class RawHandlerEntry : HandlerEntry
        {
            private readonly Action<byte[]> _callback;
            public RawHandlerEntry(Action<byte[]> callback) { _callback = callback; }
            public override void Invoke(byte[] payload) { _callback?.Invoke(payload); }
            public bool MatchCallback(Action<byte[]> callback) => _callback == callback;
        }

        // ── MsgId → Handler 列表 ────────────────
        private readonly Dictionary<ushort, List<HandlerEntry>> _handlers = new();

        // ────────────────────────────────────────
        // 注册
        // ────────────────────────────────────────

        /// <summary>
        /// 注册消息处理器（自动 Protobuf 反序列化）
        /// </summary>
        public void Register<T>(ushort msgId, Action<T> callback) where T : IMessage<T>, new()
        {
            if (!_handlers.TryGetValue(msgId, out var list))
            {
                list = new List<HandlerEntry>();
                _handlers[msgId] = list;
            }
            list.Add(new HandlerEntry<T>(callback));
        }

        /// <summary>
        /// 注册原始字节处理器（不做 Protobuf 解析）
        /// </summary>
        public void RegisterRaw(ushort msgId, Action<byte[]> callback)
        {
            if (!_handlers.TryGetValue(msgId, out var list))
            {
                list = new List<HandlerEntry>();
                _handlers[msgId] = list;
            }
            list.Add(new RawHandlerEntry(callback));
        }

        /// <summary>取消注册</summary>
        public void Unregister<T>(ushort msgId, Action<T> callback) where T : IMessage<T>, new()
        {
            if (!_handlers.TryGetValue(msgId, out var list)) return;
            list.RemoveAll(e => e is HandlerEntry<T> typed && typed.MatchCallback(callback));
            if (list.Count == 0) _handlers.Remove(msgId);
        }

        /// <summary>取消注册（原始字节版）</summary>
        public void UnregisterRaw(ushort msgId, Action<byte[]> callback)
        {
            if (!_handlers.TryGetValue(msgId, out var list)) return;
            list.RemoveAll(e => e is RawHandlerEntry raw && raw.MatchCallback(callback));
            if (list.Count == 0) _handlers.Remove(msgId);
        }

        // ────────────────────────────────────────
        // 分发
        // ────────────────────────────────────────

        /// <summary>
        /// 分发一条消息给所有注册的 Handler（主线程调用）
        /// </summary>
        public void Dispatch(ushort msgId, byte[] payload)
        {
            if (!_handlers.TryGetValue(msgId, out var list))
            {
                GLog.Warning(LogTags.Network, $"未注册处理器: 0x{msgId:X4}");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i].Invoke(payload);
                }
                catch (Exception e)
                {
                    GLog.Error(LogTags.Network, $"处理消息 0x{msgId:X4} 异常: {e}");
                }
            }
        }

        /// <summary>清空所有注册</summary>
        public void ClearAll()
        {
            _handlers.Clear();
        }
    }
}
