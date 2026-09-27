using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Game.GamePlay;
using ATEditor;

namespace Game.Editor.ActionTransition
{
    /// <summary>
    /// 动作动画与时间轴信息提取器。
    /// 负责从 ActionConfigAsset 及其关联的 ActionTimeline 中提取底层全部动画片段、默认混合时间以及路由有效窗口。
    /// </summary>
    public static class ActionClipInfoExtractor
    {
        public struct SkillClipItem
        {
            public UnityEngine.AnimationClip AnimationClip;
            public float StartTime;
            public float Duration;
            public float BlendIn;
        }

        public struct ExtractedClipData
        {
            public List<SkillClipItem> Clips;
            public float DefaultBlendIn;
            public float TimelineDuration;
            public bool IsLoop;
            public ActionTimeline TimelineSO;

            public UnityEngine.AnimationClip PrimaryClip => Clips != null && Clips.Count > 0 ? Clips[0].AnimationClip : null;

            /// <summary>
            /// 在动作时间轴给定全局时刻下，采样匹配激活的动画片段和局部时间偏移
            /// </summary>
            public (UnityEngine.AnimationClip clip, float localTime) SampleAt(float time)
            {
                if (Clips == null || Clips.Count == 0)
                {
                    return (null, 0f);
                }

                // 优先寻找包含当前时间戳的片段
                for (int i = 0; i < Clips.Count; i++)
                {
                    var item = Clips[i];
                    if (time >= item.StartTime && time <= item.StartTime + item.Duration)
                    {
                        return (item.AnimationClip, Mathf.Max(0f, time - item.StartTime));
                    }
                }

                // 若超出末尾，停留在最后一个片段末尾
                if (time > Clips[Clips.Count - 1].StartTime + Clips[Clips.Count - 1].Duration)
                {
                    var last = Clips[Clips.Count - 1];
                    return (last.AnimationClip, last.Duration);
                }

                // 若在开始前，停留在第一个片段开头
                var first = Clips[0];
                return (first.AnimationClip, 0f);
            }
        }

        public struct RouteWindowRange
        {
            public bool HasWindow;
            public float StartTime;
            public float EndTime;
            public string Tag;
        }

        /// <summary>
        /// 从 ActionConfigAsset 中提取核心动画数据（支持收集主动画轨道的所有多片段）
        /// </summary>
        public static ExtractedClipData Extract(ActionConfigAsset action)
        {
            var result = new ExtractedClipData
            {
                Clips = new List<SkillClipItem>(),
                DefaultBlendIn = 0.15f,
                TimelineDuration = 1.0f
            };

            if (action == null) return result;

            ActionTimeline timeline = action.actionTimelineSO;
            if (timeline == null && action.TimelineAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(action.TimelineAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string soPath = assetPath.Replace(".json", ".asset");
                    timeline = AssetDatabase.LoadAssetAtPath<ActionTimeline>(soPath);
                }
            }

            result.TimelineSO = timeline;

            if (timeline != null)
            {
                result.TimelineDuration = Mathf.Max(timeline.Duration, 0.1f);
                result.IsLoop = timeline.isLoop;

                // 查找主轨道的动画片段
                ATEditor.AnimationTrack masterAnimTrack = null;
                ATEditor.AnimationTrack anyAnimTrack = null;

                foreach (var track in timeline.AllTracks)
                {
                    if (track is ATEditor.AnimationTrack animTrack)
                    {
                        if (animTrack.isMasterTrack)
                        {
                            masterAnimTrack = animTrack;
                            break;
                        }
                        anyAnimTrack ??= animTrack;
                    }
                }

                var targetTrack = masterAnimTrack ?? anyAnimTrack;
                if (targetTrack != null && targetTrack.clips != null)
                {
                    foreach (var clip in targetTrack.clips)
                    {
                        if (clip is ATEditor.AnimationClip skillClip && skillClip.isEnabled && skillClip.animationClip != null)
                        {
                            float dur = skillClip.Duration > 0.01f ? skillClip.Duration : skillClip.animationClip.length;
                            float blendIn = skillClip.BlendInDuration > 0f ? skillClip.BlendInDuration : 0.15f;

                            result.Clips.Add(new SkillClipItem
                            {
                                AnimationClip = skillClip.animationClip,
                                StartTime = skillClip.StartTime,
                                Duration = dur,
                                BlendIn = blendIn
                            });

                            if (result.Clips.Count == 1)
                            {
                                result.DefaultBlendIn = blendIn;
                            }
                        }
                    }

                    // 按 StartTime 排序片段
                    result.Clips.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                }
            }

            return result;
        }

        /// <summary>
        /// 提取派生路由在源动作时间轴上的有效窗口区间（用于时间轴高亮和模拟按键游标限制）
        /// </summary>
        public static RouteWindowRange ExtractRouteWindow(ActionConfigAsset sourceAction, ActionRoute route)
        {
            var range = new RouteWindowRange
            {
                HasWindow = false,
                StartTime = 0f,
                EndTime = 1f,
                Tag = string.Empty
            };

            if (sourceAction == null || route == null) return range;

            var clipData = Extract(sourceAction);
            range.EndTime = clipData.TimelineDuration;

            string windowTag = null;
            if (route.TriggerStrategy is IntentCommandTrigger intentTrigger && intentTrigger.RequiredWindow != null)
            {
                windowTag = intentTrigger.RequiredWindow.Tag;
            }
            else if (route.TriggerStrategy is DirectAssetTrigger directTrigger && directTrigger.RequiredWindow != null)
            {
                windowTag = directTrigger.RequiredWindow.Tag;
            }

            range.Tag = windowTag;
            if (string.IsNullOrEmpty(windowTag) || clipData.TimelineSO == null)
            {
                return range;
            }

            // 遍历时间轴轨道寻找匹配 Tag 的 RouteWindowClip
            foreach (var track in clipData.TimelineSO.AllTracks)
            {
                if (track is RouteWindowTrack rwTrack && rwTrack.clips != null)
                {
                    foreach (var clip in rwTrack.clips)
                    {
                        if (clip is RouteWindowClip rwClip && rwClip.isEnabled && rwClip.routewindow != null)
                        {
                            if (string.Equals(rwClip.routewindow.Tag, windowTag, StringComparison.OrdinalIgnoreCase))
                            {
                                range.HasWindow = true;
                                range.StartTime = rwClip.StartTime;
                                range.EndTime = rwClip.StartTime + rwClip.Duration;
                                return range;
                            }
                        }
                    }
                }
            }

            return range;
        }
    }
}
