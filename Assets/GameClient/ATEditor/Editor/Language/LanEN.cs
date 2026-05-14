namespace ATEditor.Editor
{
    [Name("English")]
    public class LanEN : ILanguages
    {
        public static string ImportFromJson = "Import JSON";
        public static string ExportToJson = "Export JSON";
        public static string Save = "Save";
        public static string Settings = "Settings";
        
        public static string ImportPanelTitle = "Import Skill Config";
        public static string ExportPanelTitle = "Export Skill Config";
        
        public static string SettingsPanelTitle = "Settings";
        public static string SettingsWarning = "No valid Skill Editor state found";
        
        public static string SettingsPrecisionLabel = "Precision & Frame Control";
        public static string SettingsFrameRateLabel = "Logic Frame Rate";
        public static string SettingsTimeStepModeLabel = "Time Step Mode";
        public static string SettingsFrameSnapLabel = "Force Frame Snap (Auto)";
        public static string SettingsSnapEnabledLabel = "Enable Magnetic Snap";
        public static string SettingsSnapIntervalLabel = "Current Snap Interval: ";
        public static string SettingsDynamicStep = "Dynamic";
        public static string SettingsDefaultPreviewTargetLabel = "Default Preview Target";
        public static string PreviewSpeedMultiplier = "Preview Speed Multiplier";

        public static string AddTrackMenuItem = "AddTrack";

        public static string LanguageLabel = "Language";

        // Toolbar
        public static string Play = "Play";
        public static string Pause = "Pause";
        public static string StopTooltip = "Stop and Reset";
        public static string Zoom = "Zoom";
        public static string EditorTitle = "Skill Editor";
        public static string PreviewTarget = "Preview Target";
        public static string CreateDefaultCharacter = "Create Default";
        // Warning
        public static string PreviewTargetWarning = "[ATEditor] Please set a preview target to enable timeline seeking.";
    }
}
