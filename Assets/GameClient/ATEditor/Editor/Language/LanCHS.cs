namespace ATEditor.Editor
{
    [Name("简体中文")]
    public class LanCHS : ILanguages
    {
        public static string ImportFromJson = "导入 JSON";
        public static string ExportToJson = "导出 JSON";
        public static string Save = "保存";
        public static string Settings = "设置";
        
        public static string ImportPanelTitle = "导入技能配置";
        public static string ExportPanelTitle = "导出技能配置";
        
        public static string SettingsPanelTitle = "设置";
        public static string SettingsWarning = "未找到有效的技能编辑器状态 (No Active State)";
        
        public static string SettingsPrecisionLabel = "精度与帧控制";
        public static string SettingsFrameRateLabel = "逻辑帧率";
        public static string SettingsTimeStepModeLabel = "时间步进模式";
        public static string SettingsFrameSnapLabel = "强制帧吸附 (自动)";
        public static string SettingsSnapEnabledLabel = "开启磁性吸附";
        public static string SettingsSnapIntervalLabel = "当前吸附间隔: ";
        public static string SettingsDynamicStep = "动态自适应";
        public static string SettingsDefaultPreviewTargetLabel = "默认预览角色预制体";
        public static string PreviewSpeedMultiplier = "预览速率";

        public static string AddTrackMenuItem = "添加轨道";

        public static string LanguageLabel = "语言 (Language)";

        // Toolbar
        public static string Play = "播放";
        public static string Pause = "暂停";
        public static string StopTooltip = "停止并重置";
        public static string Zoom = "缩放";
        public static string EditorTitle = "技能编辑器";
        public static string PreviewTarget = "预览角色";
        public static string CreateDefaultCharacter = "创建默认角色";
        //warning
        public static string PreviewTargetWarning = "[技能编辑器] 请设置预览角色以启用时间轴定位功能。";
    }
}
