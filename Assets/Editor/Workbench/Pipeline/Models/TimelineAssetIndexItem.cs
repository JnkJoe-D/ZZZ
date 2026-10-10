using System;
using System.IO;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Timeline 时间轴资产索引条目
    /// 支持统一表达磁盘上的 Timeline JSON 文件与 Timeline SO 资产。
    /// </summary>
    public class TimelineAssetIndexItem
    {
        public string Guid { get; }
        public string AssetPath { get; }
        public string TimelineName { get; }
        public bool IsJson { get; }
        public bool IsSo => !IsJson;

        public TimelineAssetIndexItem(string guid, string assetPath, bool isJson)
        {
            Guid = guid ?? string.Empty;
            AssetPath = (assetPath ?? string.Empty).Replace('\\', '/');
            IsJson = isJson;
            TimelineName = Path.GetFileNameWithoutExtension(AssetPath);
        }

        public override string ToString()
        {
            return $"[TimelineItem: {TimelineName}] Kind={(IsJson ? "JSON" : "SO")}, Path={AssetPath}";
        }
    }
}
