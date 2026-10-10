using System;
using UnityEditor;
using UnityEngine;
using Game.GamePlay;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - Action 动作资产索引条目
    /// 统一记录 ActionConfigAsset 的 GUID、路径、归属工作区及双轨时间轴引用状态。
    /// </summary>
    public class ActionAssetIndexItem
    {
        public string Guid { get; }
        public string AssetPath { get; }
        public string ActionName { get; }
        public ActionConfigAsset AssetObject { get; }

        public string TimelineJsonPath { get; }
        public string TimelineSoPath { get; }

        public bool HasTimelineJson => !string.IsNullOrEmpty(TimelineJsonPath);
        public bool HasTimelineSo => !string.IsNullOrEmpty(TimelineSoPath);
        public bool IsFullyLinked => HasTimelineJson && HasTimelineSo;

        public ActionAssetIndexItem(
            string guid,
            string assetPath,
            ActionConfigAsset assetObject)
        {
            Guid = guid ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            AssetObject = assetObject;

            if (assetObject != null)
            {
                ActionName = !string.IsNullOrEmpty(assetObject.Name)
                    ? assetObject.Name
                    : assetObject.name;

                if (assetObject.TimelineAsset != null)
                {
                    TimelineJsonPath = AssetDatabase.GetAssetPath(assetObject.TimelineAsset);
                }

                if (assetObject.actionTimelineSO != null)
                {
                    TimelineSoPath = AssetDatabase.GetAssetPath(assetObject.actionTimelineSO);
                }
            }
            else
            {
                ActionName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
        }

        public override string ToString()
        {
            return $"[ActionItem: {ActionName}] Path={AssetPath}, HasJson={HasTimelineJson}, HasSO={HasTimelineSo}";
        }
    }
}
