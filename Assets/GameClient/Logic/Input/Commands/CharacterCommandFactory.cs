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

        // 按事件类型缓存轻量系统事件指令实例，避免重复堆分配
        private static readonly System.Collections.Generic.Dictionary<RouteEventType, CharacterCommand> _cachedSystemEventCommands = new();

        public static CharacterCommand CreateSystemEventCommand(RouteEventType eventType)
        {
            if (!_cachedSystemEventCommands.TryGetValue(eventType, out var cmd))
            {
                cmd = new CharacterCommand
                {
                    Id = ++_idCounter,
                    Payload = new SystemEventPayload { EventType = eventType },
                    Timestamp = Time.time,
                    IsConsumed = false
                };
                _cachedSystemEventCommands[eventType] = cmd;
            }
            else
            {
                cmd.Id = ++_idCounter;
                cmd.Timestamp = Time.time;
                cmd.IsConsumed = false;
            }
            return cmd;
        }
    }
}
