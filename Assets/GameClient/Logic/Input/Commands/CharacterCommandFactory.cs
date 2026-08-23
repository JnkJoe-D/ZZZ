using Game.Input;
using Game.Logic;
using UnityEngine;

namespace Game.Logic
{
    public static class CharacterCommandFactory
    {
        private static long _idCounter = 0;

        public static CharacterCommand Create(HardwareInputType commandType, CommandPhase phase, IInputProvider provider)
        {
            Vector2 direction = provider?.GetMovementDirection() ?? Vector2.zero;

            return new CharacterCommand
            {
                Id = ++_idCounter,
                Payload = new InputPayload
                {
                    InputType = commandType,
                    Phase = phase,
                    DirectionSnapshot = direction,
                    HasMovementInput = provider != null && provider.HasMovementInput()
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }

        public static CharacterCommand CreateDirectAssetCommand(ActionConfigAsset actionAsset)
        {
            return new CharacterCommand
            {
                Id = ++_idCounter,
                Payload = new DirectAssetPayload
                {
                    TargetAsset = actionAsset
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }

        public static CharacterCommand CreateSystemEventCommand(RouteEventType eventType)
        {
            return new CharacterCommand
            {
                Id = ++_idCounter,
                Payload = new SystemEventPayload
                {
                    EventType = eventType
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }
    }
}
