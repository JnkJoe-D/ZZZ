using System.Collections.Generic;
using Game.Framework;
using SkillEditor;

namespace Game.Adapters
{
    /// <summary>
    /// 全局事件数据载体
    /// 发射到 Game.Framework.EventCenter 的包装类
    /// </summary>
    public struct SkillGlobalEvent : IGameEvent
    {
        public string EventName;
        public IReadOnlyList<SkillEventParam> Parameters;
    }

    /// <summary>
    /// 技能系统与当前全局事件中心 (EventCenter) 之间的适配器
    /// </summary>
    public class SkillEventHandler : ISkillEventHandler
    {
        public void OnSkillEvent(string eventName, List<SkillEventParam> parameters)
        {
            // 通过 EventCenter 广播由技能编辑器触发过来的通用逻辑事件
            var e = new SkillGlobalEvent
            {
                EventName = eventName,
                Parameters = parameters
            };
            
            EventCenter.Publish(e);
        }
    }
}
