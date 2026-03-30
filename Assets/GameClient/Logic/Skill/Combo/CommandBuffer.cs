using System.Collections.Generic;
using Game.Logic.Character;
using UnityEngine;

namespace Game.Logic.Action.Combo
{
    public enum CommandRouteSource
    {
        None = 0,
        LocalRoute = 10,
        ContextRoute = 20,
        StateAction = 30
    }

    public class CharacterCommand
    {
        public CommandType Type;
        public CommandPhase Phase;
        public CommandPayload Payload;
        public float Timestamp;
        public long BufferOrder;
        public bool IsConsumed;
    }

    public class CommandBuffer
    {
        private readonly List<CharacterCommand> _commands = new();
        private long _nextBufferOrder;
        private const float ExpirationTime = 0.3f;

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
    }
}
