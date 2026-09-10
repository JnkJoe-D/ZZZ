using System.Collections.Generic;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public enum CommandRouteSource
    {
        None = 0,
        ActionRoute = 10,
        ActionComplete = 20
    }

    public class CharacterCommand
    {
        public long Id;
        public ICommandPayload Payload;
        public float Timestamp;
        public long BufferOrder;
        public bool IsConsumed;
    }

    public enum BufferMode
    {
        Queue,
        SingleOverride
    }

    /// <summary>
    /// 指令缓冲区：纯缓冲池，仅存储有时效性的瞬时指令。
    /// Held 状态跟踪已移至输入层（IInputProvider），此处不再维护。
    /// </summary>
    public class CommandBuffer
    {
        private readonly BufferMode _mode;
        private readonly List<CharacterCommand> _commands = new();
        private long _nextBufferOrder;
        private const float ExpirationTime = 0.3f;

        public CommandBuffer(BufferMode mode = BufferMode.Queue)
        {
            _mode = mode;
        }

        private float CurrentLogicTime => TimeManager.Instance != null ? TimeManager.Instance.GameplayTime : Time.time;

        public void Push(CharacterCommand command)
        {
            if (command == null) return;

            if (_mode == BufferMode.SingleOverride)
            {
                _commands.Clear();
            }

            if (command.Timestamp <= 0f)
                command.Timestamp = CurrentLogicTime;

            command.BufferOrder = ++_nextBufferOrder;
            _commands.Add(command);
        }

        public void Tick()
        {
            float currentTime = CurrentLogicTime;
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                CharacterCommand cmd = _commands[i];
                if (currentTime - cmd.Timestamp > ExpirationTime || cmd.IsConsumed)
                {
                    _commands.RemoveAt(i);
                }
            }
        }

        private readonly List<CharacterCommand> _unconsumedCommandsCache = new();

        public List<CharacterCommand> GetUnconsumedCommands()
        {
            _unconsumedCommandsCache.Clear();
            for (int i = 0; i < _commands.Count; i++)
            {
                if (!_commands[i].IsConsumed)
                {
                    _unconsumedCommandsCache.Add(_commands[i]);
                }
            }
            return _unconsumedCommandsCache;
        }

        public void Clear()
        {
            _commands.Clear();
            _unconsumedCommandsCache.Clear();
        }
    }
}
