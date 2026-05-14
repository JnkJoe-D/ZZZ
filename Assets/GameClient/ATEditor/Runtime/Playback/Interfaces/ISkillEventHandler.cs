using System.Collections.Generic;

namespace ATEditor
{
    /// <summary>
    /// 技能事件处理器接口
    /// 战斗系统/状态机需实现此接口，用于接收 Timeline 传出的通用逻辑指令
    /// </summary>
    public interface ISkillEventHandler
    {
        /// <summary>
        /// 技能触发了通用逻辑事件
        /// </summary>
        /// <param name="eventName">事件名称（如 "AddBuff", "SetCamera"）</param>
        /// <param name="parameters">事件附带的键值对参数列表</param>
        void OnSkillEvent(string eventName, List<SkillEventParam> parameters);
    }
}
