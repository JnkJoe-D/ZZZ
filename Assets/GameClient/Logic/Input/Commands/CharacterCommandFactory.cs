using Game.Input;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public static class CharacterCommandFactory
    {
        private static long _idCounter = 0;

        public static CharacterCommand Create(InputCommand commandType, CommandPhase phase, IInputProvider provider)
        {
            Vector2 direction = provider?.GetMovementDirection() ?? Vector2.zero;

            return new CharacterCommand
            {
                Id = ++_idCounter,
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
        public static CharacterCommand Create(InputCommand commandType, CommandPhase phase, ActionConfigAsset actionAsset)
        {
            return new CharacterCommand
            {
                Id = ++_idCounter,
                Type = commandType,
                Phase = phase,
                Payload = new CommandPayload
                {
                    AIActionAsset = actionAsset
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }
    }
}
