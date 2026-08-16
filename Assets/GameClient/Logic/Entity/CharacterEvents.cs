using System.Collections.Generic;
using Game.Framework;
using ATEditor;

namespace Game.Logic
{
    /// <summary>
    /// 动作路由执行事件。
    /// 当 ActionController 的路由执行目标为 Event 时广播此事件，
    /// 由各子系统（如 SwitchExecutor）订阅并响应。
    /// </summary>
    public struct ActionRouteExecuteEvent : IGameEvent
    {
        /// <summary> 触发此事件的角色实体（即当前操控角色）。 </summary>
        public RoleEntity SourceEntity;

        /// <summary> 路由配置的执行事件类型。 </summary>
        public ExecuteEvent Event;

        /// <summary>
        /// 可选的目标插槽索引提示。-1 表示使用默认轮转规则。
        /// 为未来定向切人（如支援技指定角色）预留扩展。
        /// </summary>
        public int TargetSlotHint;
    }

    /// <summary>
    /// Timeline 动画事件。
    /// 由 CharacterEntity.OnSkillEvent 发布，CharacterManager 订阅后委托给各子系统处理。
    /// </summary>
    public struct CharacterTimelineEvent : IGameEvent
    {
        public RoleEntity SourceEntity;
        public string EventName;
        public List<ATEventParam> Parameters;
    }

    /// <summary>
    /// 当玩家当前控制（激活）的角色发生改变时广播
    /// </summary>
    public struct ActiveCharacterChangedEvent : IGameEvent
    {
        public int OldSlotIndex;
        public int NewSlotIndex;
        public RoleEntity NewEntity;
    }
}
