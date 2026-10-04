using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 动画片段元数据统一描述，解耦外部对具体 AnimationClip 类型的直接强依赖
    /// </summary>
    public struct ATAnimationClipInfo
    {
        public UnityEngine.AnimationClip Clip;
        public float StartTime;
        public float Duration;
        public float BlendInDuration;
        public bool IsEnabled;
    }

    /// <summary>
    /// ActionTimeline 外部查询与解耦访问扩展类。
    /// 位于 ATEditor/Runtime/Utils 非核心工具层，提供动画片段提取、路由窗口检索等通用只读契约。
    /// </summary>
    public static class ActionTimelineExtensions
    {
        /// <summary>
        /// 从时间轴的主动画轨道（或首个有效动画轨道）提取所有启用的动画片段信息列表
        /// </summary>
        public static List<ATAnimationClipInfo> GetAnimationClips(this ActionTimeline timeline)
        {
            var result = new List<ATAnimationClipInfo>();
            if (timeline == null) return result;

            AnimationTrack targetTrack = null;
            foreach (var track in timeline.AllTracks)
            {
                if (track is AnimationTrack animTrack && animTrack.isEnabled)
                {
                    if (animTrack.isMasterTrack)
                    {
                        targetTrack = animTrack;
                        break;
                    }
                    targetTrack ??= animTrack;
                }
            }

            if (targetTrack?.clips != null)
            {
                foreach (var clip in targetTrack.clips)
                {
                    if (clip is AnimationClip animClip && animClip.isEnabled && animClip.animationClip != null)
                    {
                        float dur = animClip.Duration > 0.01f ? animClip.Duration : animClip.animationClip.length;
                        float blendIn = animClip.BlendInDuration > 0f ? animClip.BlendInDuration : 0.15f;

                        result.Add(new ATAnimationClipInfo
                        {
                            Clip = animClip.animationClip,
                            StartTime = animClip.StartTime,
                            Duration = dur,
                            BlendInDuration = blendIn,
                            IsEnabled = animClip.isEnabled
                        });
                    }
                }

                result.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            }

            return result;
        }

        /// <summary>
        /// 获取时间轴首个动画片段的默认渐入时长（Crossfade / BlendIn）
        /// </summary>
        public static float GetDefaultBlendInDuration(this ActionTimeline timeline, float fallback = 0.15f)
        {
            var clips = timeline.GetAnimationClips();
            if (clips.Count > 0 && clips[0].BlendInDuration > 0f)
            {
                return clips[0].BlendInDuration;
            }
            return fallback;
        }

        /// <summary>
        /// 检索时间轴中所有符合条件的 RouteWindowClip（支持按 Tag 过滤）
        /// </summary>
        public static List<RouteWindowClip> FindRouteWindowClips(this ActionTimeline timeline, string tag = null)
        {
            var result = new List<RouteWindowClip>();
            if (timeline == null) return result;

            foreach (var track in timeline.AllTracks)
            {
                if (track is RouteWindowTrack rwTrack && rwTrack.clips != null)
                {
                    foreach (var clip in rwTrack.clips)
                    {
                        if (clip is RouteWindowClip rwClip && rwClip.isEnabled && rwClip.routewindow != null)
                        {
                            if (string.IsNullOrEmpty(tag) || string.Equals(rwClip.routewindow.Tag, tag, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(rwClip);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检索时间轴中所有窗口类型为 AutoRouteWindow 的 Clip
        /// </summary>
        public static List<RouteWindowClip> FindAutoRouteWindowClips(this ActionTimeline timeline, string tag = null)
        {
            var all = timeline.FindRouteWindowClips(tag);
            var result = new List<RouteWindowClip>();
            foreach (var clip in all)
            {
                if (clip.routewindow is AutoRouteWindow)
                {
                    result.Add(clip);
                }
            }
            return result;
        }

        /// <summary>
        /// 查找指定 Clip 所属的 Track
        /// </summary>
        public static TrackBase FindClipTrack(this ActionTimeline timeline, ClipBase targetClip)
        {
            if (timeline == null || targetClip == null) return null;

            foreach (var track in timeline.AllTracks)
            {
                if (track.clips != null && track.clips.Contains(targetClip))
                {
                    return track;
                }
            }

            return null;
        }
    }
}
