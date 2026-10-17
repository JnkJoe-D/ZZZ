using System;
using UnityEngine;

namespace Game.GamePlay
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
                    HasMovementInput = provider != null && provider.HasMoveInput()
                },
                Timestamp = Time.time,
                IsConsumed = false
            };
        }

        public static CharacterCommand CreateDirectAssetCommand(CharacterActionPlayInfo playInfo)
        {
            if (playInfo == null) return null;

            DirectAssetPayload payload;
            if (playInfo is DirectAssetPayload directPayload)
            {
                payload = directPayload;
            }
            else
            {
                payload = new DirectAssetPayload
                {
                    ActionConfig = playInfo.ActionConfig,
                    TransitionInfo = playInfo.TransitionInfo,
                    EventInfo = playInfo.EventInfo,
                    CommandId = playInfo.CommandId,
                    RouteTag = playInfo.RouteTag,
                    RouteSource = playInfo.RouteSource,
                    SourcePayload = playInfo.SourcePayload
                };
            }

            return new CharacterCommand
            {
                Id = ++_idCounter,
                Payload = payload,
                Timestamp = Time.time,
                IsConsumed = false
            };
        }

        public static CharacterCommand CreateDirectAssetCommand(ActionPlayInfo playInfo)
        {
            if (playInfo == null) return null;
            if (playInfo is CharacterActionPlayInfo charPlayInfo)
                return CreateDirectAssetCommand(charPlayInfo);

            return CreateDirectAssetCommand(new CharacterActionPlayInfo
            {
                ActionConfig = playInfo.ActionConfig,
                TransitionInfo = playInfo.TransitionInfo,
                EventInfo = playInfo.EventInfo
            });
        }

        public static CharacterCommand CreateDirectAssetCommand(
            ActionConfigAsset actionAsset,
            float crossfadeOverride = -1f,
            float startTime = 0f,
            Action onComplete = null,
            Action onInterrupt = null,
            Action onStart = null)
        {
            var payload = new DirectAssetPayload(actionAsset, crossfadeOverride, startTime, onComplete, onInterrupt, onStart);

            return new CharacterCommand
            {
                Id = ++_idCounter,
                Payload = payload,
                Timestamp = Time.time,
                IsConsumed = false
            };
        }

        // 按事件类型缓存轻量系统事件指令实例，避免重复堆分配
        private static readonly System.Collections.Generic.Dictionary<RouteEventType, CharacterCommand> _cachedSystemEventCommands = new();

        public static CharacterCommand CreateSystemEventCommand(RouteEventType eventType, float startTime = 0f)
        {
            if (!_cachedSystemEventCommands.TryGetValue(eventType, out var cmd))
            {
                cmd = new CharacterCommand
                {
                    Id = ++_idCounter,
                    Payload = new SystemEventPayload { EventType = eventType, StartTime = startTime },
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
                if (cmd.Payload is SystemEventPayload payload)
                {
                    payload.StartTime = startTime;
                }
                else
                {
                    cmd.Payload = new SystemEventPayload { EventType = eventType, StartTime = startTime };
                }
            }
            return cmd;
        }
    }
}
