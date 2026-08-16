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
        public InputCommand Type;
        public CommandPhase Phase;
        public CommandPayload Payload;
        public float Timestamp;
        public long BufferOrder;
        public bool IsConsumed;
    }

    /// <summary>
    /// 指令缓冲区：纯缓冲池，仅存储有时效性的瞬时指令。
    /// Held 状态跟踪已移至输入层（IInputProvider），此处不再维护。
    /// </summary>
    public class CommandBuffer
    {
        private readonly List<CharacterCommand> _commands = new();
        private long _nextBufferOrder;
        private const float ExpirationTime = 0.3f;

        public void Push(CharacterCommand command)
        {
            if (command == null) return;

            if (command.Timestamp <= 0f)
                command.Timestamp = Time.time;

            command.BufferOrder = ++_nextBufferOrder;
            _commands.Add(command);
        }

        public void Tick()
        {
            float currentTime = Time.time;
            _commands.RemoveAll(cmd => currentTime - cmd.Timestamp > ExpirationTime || cmd.IsConsumed);
        }

        public IEnumerable<CharacterCommand> GetUnconsumedCommands()
        {
            foreach (CharacterCommand command in _commands)
            {
                if (!command.IsConsumed)
                    yield return command;
            }
        }

        public void Clear()
        {
            _commands.Clear();
        }
    }
}
