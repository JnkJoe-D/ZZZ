using System.Collections.Generic;
using Game.Framework;
using ATEditor;

namespace Game.GamePlay
{
    public struct ATGlobalEvent : IGameEvent
    {
        public string EventName;
        public IReadOnlyList<ATEventParam> Parameters;
    }

    /// <summary>
    /// 动作时间轴事件专职适配器。
    /// 负责捕获 ATEditor 时间轴上的打点事件并向游戏事件总线发布标准领域事件。
    /// </summary>
    public class ATEventHandler : IEventHandler
    {
        private readonly CharacterEntity _entity;

        public ATEventHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void OnActionTimelineEvent(string eventName, List<ATEventParam> parameters)
        {
            // 1. 广播携带宿主实体上下文的标准领域事件（供编队系统、换人逻辑等消费）
            EventCenter.Publish(new CharacterTimelineEvent
            {
                SourceEntity = _entity as RoleEntity,
                EventName = eventName,
                Parameters = parameters
            });

            // 2. 广播通用全局时间轴事件（保持向后兼容）
            EventCenter.Publish(new ATGlobalEvent
            {
                EventName = eventName,
                Parameters = parameters
            });
        }
    }
}
