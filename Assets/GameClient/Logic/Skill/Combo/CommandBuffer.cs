using System.Collections.Generic;
using Game.Logic.Character;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    public enum CommandRouteSource
    {
        None = 0,
        ActionRoute = 10
    }

    public class CharacterCommand
    {
        public InputCommand Type;
        public CommandPhase Phase;
        public CommandPayload Payload;
        public float Timestamp;
        public long BufferOrder;
        public bool IsConsumed;
        public bool IsSynthetic; // 是否为被动注入的合成指令（如窗口切入时检测到的持续按键）
    }

    public class CommandBuffer
    {
        private readonly List<CharacterCommand> _commands = new();
        private readonly Dictionary<InputCommand, CommandPayload> _heldInputs = new();
        private long _nextBufferOrder;
        private const float ExpirationTime = 1f;

        public void Push(CharacterCommand command)
        {
            if (command == null)
            {
                return;
            }

            if (command.Timestamp <= 0f)
            {
                command.Timestamp = Time.time;
            }

            command.BufferOrder = ++_nextBufferOrder;
            _commands.Add(command);

            // 被动更新物理按键的持有状态（不受 Clear 影响）
            if (!command.IsSynthetic)
            {
                if (command.Phase == CommandPhase.Held)
                {
                    _heldInputs[command.Type] = command.Payload;
                }
                else if (command.Phase == CommandPhase.Canceled)
                {
                    _heldInputs.Remove(command.Type);
                }
            }
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
                {
                    yield return command;
                }
            }
        }

        public void Clear()
        {
            _commands.Clear();
        }

        public bool HasUnconsumedCommand()
        {
            foreach (CharacterCommand command in _commands)
            {
                if (!command.IsConsumed)
                {
                    return true;
                }
            }

            return false;
        }

        public void InjectHeldStateSnapshot()
        {
            foreach (var kvp in _heldInputs)
            {
                var cmd = new CharacterCommand
                {
                    Type = kvp.Key,
                    Phase = CommandPhase.Held,
                    Payload = kvp.Value,
                    Timestamp = Time.time,
                    IsConsumed = false,
                    IsSynthetic = true,
                    BufferOrder = ++_nextBufferOrder
                };
                _commands.Add(cmd);
            }
        }

        public void ClearSyntheticCommands()
        {
            _commands.RemoveAll(cmd => cmd.IsSynthetic);
        }
    }
}
