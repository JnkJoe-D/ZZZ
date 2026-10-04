using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 覆盖目标属性：StartTime 还是 EndTime
    /// </summary>
    public enum WindowTimingTarget
    {
        StartTime,
        EndTime
    }

    /// <summary>
    /// ActionTimeline 窗口安全调整与反向同步操作类。
    /// 位于 ATEditor/Editor/Utils 非核心工具层，提供时间钳制、面积重叠检测与同组自愈迁轨等操作。
    /// </summary>
    public static class ActionTimelineWindowOperations
    {
        /// <summary>
        /// 判定两个时间区间是否存在正面积重叠（端点边界接触视为合法，不判定为重叠）。
        /// 例如：A.end == B.start 时返回 false（合法重叠点）。
        /// </summary>
        public static bool HasAreaOverlap(float startA, float endA, float startB, float endB)
        {
            float overlapStart = Mathf.Max(startA, startB);
            float overlapEnd = Mathf.Min(endA, endB);
            return overlapStart < overlapEnd - 0.0001f;
        }

        /// <summary>
        /// 调整 AutoRouteWindowClip 的起止时间，并安全处理时间边界限制与同组重叠迁移
        /// </summary>
        /// <param name="timeline">时间轴数据根节点</param>
        /// <param name="clip">待调整的 RouteWindowClip</param>
        /// <param name="targetProperty">覆盖到 StartTime 还是 EndTime</param>
        /// <param name="targetTime">目标时刻（例如工作台中的 ExitTime）</param>
        /// <returns>操作结果描述（成功或提示信息）</returns>
        public static bool AdjustRouteWindowTiming(
            ActionTimeline timeline,
            RouteWindowClip clip,
            WindowTimingTarget targetProperty,
            float targetTime,
            out string statusMessage)
        {
            statusMessage = string.Empty;
            if (timeline == null || clip == null)
            {
                statusMessage = "时间轴或 Clip 为空";
                return false;
            }

            float timelineDuration = Mathf.Max(timeline.Duration, 0.1f);
            float oldDuration = Mathf.Max(clip.Duration, 0.05f);

            float newStartTime;
            float newDuration;

            if (targetProperty == WindowTimingTarget.StartTime)
            {
                // 覆盖到 StartTime：将进入时刻设为 targetTime
                newStartTime = Mathf.Clamp(targetTime, 0f, timelineDuration - 0.01f);
                // 保持原窗口时长，但不能超出时间轴总长度
                newDuration = Mathf.Min(oldDuration, timelineDuration - newStartTime);
                newDuration = Mathf.Max(newDuration, 0.01f);
            }
            else
            {
                // 覆盖到 EndTime：将离开时刻设为 targetTime
                float newEndTime = Mathf.Clamp(targetTime, 0.01f, timelineDuration);
                newStartTime = Mathf.Max(0f, newEndTime - oldDuration);
                newDuration = newEndTime - newStartTime;
                newDuration = Mathf.Max(newDuration, 0.01f);
            }

            float newEndTimeFinal = newStartTime + newDuration;

            // 查找 Clip 所在轨道
            TrackBase currentTrack = timeline.FindClipTrack(clip);
            if (currentTrack == null)
            {
                statusMessage = "未找到 Clip 所在的轨道";
                return false;
            }

            // 查找所在 Group
            Group group = timeline.FindGroupContainingTrack(currentTrack);
            if (group == null)
            {
                statusMessage = "未找到轨道所属的 Group";
                return false;
            }

            // 检查在当前轨道上是否与同轨道的其它 Clip 发生面积重叠
            bool hasOverlapOnCurrentTrack = false;
            if (currentTrack.clips != null)
            {
                foreach (var other in currentTrack.clips)
                {
                    if (other == clip || other == null) continue;
                    if (HasAreaOverlap(newStartTime, newEndTimeFinal, other.StartTime, other.EndTime))
                    {
                        hasOverlapOnCurrentTrack = true;
                        break;
                    }
                }
            }

            Undo.RecordObject(timeline, "Adjust Route Window Timing");

            TrackBase destinationTrack = currentTrack;

            // 若发生面积重叠，在同一分组（Group）内寻找空闲的 RouteWindowTrack 或新建一条
            if (hasOverlapOnCurrentTrack)
            {
                RouteWindowTrack suitableTrack = null;

                foreach (var track in group.tracks)
                {
                    if (track is RouteWindowTrack rwTrack && track != currentTrack)
                    {
                        bool trackHasConflict = false;
                        if (rwTrack.clips != null)
                        {
                            foreach (var other in rwTrack.clips)
                            {
                                if (other == null) continue;
                                if (HasAreaOverlap(newStartTime, newEndTimeFinal, other.StartTime, other.EndTime))
                                {
                                    trackHasConflict = true;
                                    break;
                                }
                            }
                        }

                        if (!trackHasConflict)
                        {
                            suitableTrack = rwTrack;
                            break;
                        }
                    }
                }

                // 若同一分组内没有无冲突的现有轨道，则新建一条同组 RouteWindowTrack
                if (suitableTrack == null)
                {
                    suitableTrack = group.AddTrack<RouteWindowTrack>();
                    suitableTrack.trackName = $"{currentTrack.trackName} (自动拆分)";
                }

                // 从原轨道移除并迁移到新轨道
                currentTrack.RemoveClip(clip);
                if (suitableTrack.clips == null) suitableTrack.clips = new List<ClipBase>();
                suitableTrack.clips.Add(clip);
                destinationTrack = suitableTrack;
            }

            // 应用新时间
            clip.StartTime = newStartTime;
            clip.Duration = newDuration;

            EditorUtility.SetDirty(timeline);

            string targetName = targetProperty == WindowTimingTarget.StartTime ? "StartTime" : "EndTime";
            statusMessage = hasOverlapOnCurrentTrack
                ? $"已在同组新建/迁移轨道，并将 [{clip.clipName}] 的 {targetName} 同步为 {targetTime:0.00}s ({newStartTime:0.00}s ~ {newEndTimeFinal:0.00}s)"
                : $"已将 [{clip.clipName}] 的 {targetName} 同步为 {targetTime:0.00}s ({newStartTime:0.00}s ~ {newEndTimeFinal:0.00}s)";

            return true;
        }

        /// <summary>
        /// 批量/统一覆盖时间轴主动画片段的默认渐入时长（BlendInDuration）
        /// </summary>
        public static void SetMasterAnimationBlendIn(ActionTimeline timeline, float blendInDuration)
        {
            if (timeline == null) return;

            Undo.RecordObject(timeline, "Modify Animation BlendIn Duration");

            foreach (var track in timeline.AllTracks)
            {
                if (track is AnimationTrack animTrack && animTrack.isMasterTrack)
                {
                    if (animTrack.clips != null)
                    {
                        foreach (var c in animTrack.clips)
                        {
                            if (c is AnimationClip animClip && animClip.animationClip != null)
                            {
                                animClip.BlendInDuration = Mathf.Max(0f, blendInDuration);
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(timeline);
        }

        /// <summary>
        /// 对指定 ActionTimeline 执行与 ATEditor 标准一致的双轨保存（同时更新 SO 和 JSON）
        /// </summary>
        /// <param name="timeline">时间轴数据根节点</param>
        /// <param name="knownAssetOrJsonPath">已知的 SO 或 JSON 资产路径（例如来自 ActionConfigAsset）</param>
        /// <param name="workspace">所属角色工作区（可选，用于路径缺失时的目录推导）</param>
        /// <param name="savedJsonPath">导出的 JSON 相对路径</param>
        /// <param name="savedAssetPath">导出的 SO 相对路径</param>
        /// <returns>保存是否成功</returns>
        public static bool SaveTimelineDual(
            ActionTimeline timeline,
            string knownAssetOrJsonPath,
            ATWorkspaceDefinition workspace,
            out string savedJsonPath,
            out string savedAssetPath)
        {
            savedJsonPath = string.Empty;
            savedAssetPath = string.Empty;

            if (timeline == null) return false;

            string defaultJsonRoot = EditorPrefs.GetString("SkillEditor_DefaultJsonDir", "Assets/Resources/Serializations/JSON/ActionTimelines").Replace('\\', '/');
            string defaultAssetRoot = EditorPrefs.GetString("SkillEditor_DefaultAssetDir", "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines").Replace('\\', '/');

            string fileName = !string.IsNullOrEmpty(timeline.name) ? timeline.name : "NewTimeline";
            string jsonDir = defaultJsonRoot;
            string assetDir = defaultAssetRoot;

            if (!string.IsNullOrEmpty(knownAssetOrJsonPath))
            {
                string normalizedKnown = knownAssetOrJsonPath.Replace('\\', '/');
                fileName = Path.GetFileNameWithoutExtension(normalizedKnown);
                string currentDir = Path.GetDirectoryName(normalizedKnown)?.Replace('\\', '/');
                string ext = Path.GetExtension(normalizedKnown);

                // 尝试推导工作区
                if (workspace == null)
                {
                    workspace = ATEditorWorkspaceDatabase.Instance?.GetWorkspaceByAssetPath(normalizedKnown);
                }

                if (string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonDir = currentDir;
                    // 推导 assetDir：若路径包含 /JSON/ActionTimelines/ 则替换为 /ScriptableObjects/ActionTimelines/
                    if (normalizedKnown.IndexOf("/JSON/ActionTimelines/", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        assetDir = currentDir.Replace("/JSON/ActionTimelines", "/ScriptableObjects/ActionTimelines");
                    }
                    else if (workspace != null && !string.IsNullOrEmpty(workspace.FolderName))
                    {
                        assetDir = Path.Combine(defaultAssetRoot, workspace.FolderName).Replace('\\', '/');
                    }
                }
                else if (string.Equals(ext, ".asset", StringComparison.OrdinalIgnoreCase))
                {
                    assetDir = currentDir;
                    // 推导 jsonDir：若路径包含 /ScriptableObjects/ActionTimelines/ 则替换为 /JSON/ActionTimelines/
                    if (normalizedKnown.IndexOf("/ScriptableObjects/ActionTimelines/", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        jsonDir = currentDir.Replace("/ScriptableObjects/ActionTimelines", "/JSON/ActionTimelines");
                    }
                    else if (workspace != null && !string.IsNullOrEmpty(workspace.FolderName))
                    {
                        jsonDir = Path.Combine(defaultJsonRoot, workspace.FolderName).Replace('\\', '/');
                    }
                }
            }
            else if (workspace != null && !string.IsNullOrEmpty(workspace.FolderName))
            {
                jsonDir = Path.Combine(defaultJsonRoot, workspace.FolderName).Replace('\\', '/');
                assetDir = Path.Combine(defaultAssetRoot, workspace.FolderName).Replace('\\', '/');
            }

            // 调用 ATEditor 统一的 SerializationUtility.SaveDual
            SerializationUtility.SaveDual(timeline, jsonDir, assetDir, fileName);

            savedJsonPath = Path.Combine(jsonDir, fileName + ".json").Replace('\\', '/');
            savedAssetPath = Path.Combine(assetDir, fileName + ".asset").Replace('\\', '/');

            AssetDatabase.Refresh();
            return true;
        }
    }
}
