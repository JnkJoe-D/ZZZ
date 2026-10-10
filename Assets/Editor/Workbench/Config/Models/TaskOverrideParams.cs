using System;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 单次任务临时覆盖参数 (纯内存状态，不写入持久化配置)
    /// 位于四级生效优先级的最高层级：Task Overrides > Character Overrides > Workspace Config > System Defaults
    /// </summary>
    public class TaskOverrideParams
    {
        public string ActionDirectoryOverride { get; set; }
        public string TimelineJsonDirectoryOverride { get; set; }
        public string TimelineAssetDirectoryOverride { get; set; }
        public string NamingTemplateOverride { get; set; }
        public ConflictPolicy? ConflictPolicyOverride { get; set; }
        public string SkillTablePathOverride { get; set; }

        public bool HasAnyOverride =>
            !string.IsNullOrEmpty(ActionDirectoryOverride) ||
            !string.IsNullOrEmpty(TimelineJsonDirectoryOverride) ||
            !string.IsNullOrEmpty(TimelineAssetDirectoryOverride) ||
            !string.IsNullOrEmpty(NamingTemplateOverride) ||
            !string.IsNullOrEmpty(SkillTablePathOverride) ||
            ConflictPolicyOverride.HasValue;
    }
}
