using System;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 生效配置项的来源溯源标记，用于在 UI 和报告中展示该项是继承还是显式覆盖
    /// </summary>
    public enum ConfigSource
    {
        /// <summary>
        /// 系统内置默认基准值
        /// </summary>
        SystemDefault = 0,

        /// <summary>
        /// 继承自工作区全局配置 (WorkspaceConfig)
        /// </summary>
        WorkspaceInherited = 1,

        /// <summary>
        /// 角色差异化覆盖 (CharacterOverride)
        /// </summary>
        CharacterOverride = 2,

        /// <summary>
        /// 单次任务临时参数覆盖 (TaskOverride)
        /// </summary>
        TaskOverride = 3
    }
}
