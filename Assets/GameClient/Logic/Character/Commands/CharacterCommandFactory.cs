using Game.Input;
using Game.Logic.Action.Combo;
using UnityEngine;

namespace Game.Logic.Character
{
    public static class CharacterCommandFactory
    {
        public static CharacterCommand Create(CommandType commandType, CommandPhase phase, IInputProvider provider)
        {
            Vector2 direction = provider?.GetMovementDirection() ?? Vector2.zero;

            return new CharacterCommand
            {
                Type = commandType,
                Phase = phase,
                Payload = new CommandPayload
                {
                    DirectionSnapshot = direction,
                    HasMovementInput = provider != null && provider.HasMovementInput()
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }
    }
}
