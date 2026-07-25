using System;
using Game.Framework;

namespace Game.UI 
{
    public class HotUpdateModel : UIModel
    {
        public float DownloadProgress { get; set; } = 0f;
        public string StatusText      { get; set; } = "正在检查更新...";
        public string VersionText     { get; set; } = "v1.0.0";
        public string SpeedText       { get; set; } = "0 KB/s";
    }
}
