using System;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 资产生成与批处理时的冲突处理策略
    /// </summary>
    public enum ConflictPolicy
    {
        /// <summary>
        /// 跳过已存在的资产，保留原样
        /// </summary>
        Skip = 0,

        /// <summary>
        /// 覆盖已有资产（需经二次确认或仅覆盖受管字段）
        /// </summary>
        Overwrite = 1,

        /// <summary>
        /// 自动重命名并生成新资产（例如加后缀 _Copy）
        /// </summary>
        AutoRename = 2
    }
}
