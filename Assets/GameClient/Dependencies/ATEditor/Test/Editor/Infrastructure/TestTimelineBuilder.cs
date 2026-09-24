using System;
using System.Reflection;
using UnityEngine;

namespace ATEditor.Test
{
    /// <summary>
    /// 测试专用时间轴纯代码动态构建器
    /// 100% 在内存中组装，无需任何外部 .asset 或 .json 资源文件
    /// </summary>
    public static class TestTimelineBuilder
    {
        /// <summary>
        /// 创建一个干净的 ActionTimeline 内存实例
        /// </summary>
        public static ActionTimeline Create(float duration = 1.0f, bool isLoop = false, int skillId = 1001)
        {
            var timeline = ScriptableObject.CreateInstance<ActionTimeline>();
            timeline.Id = skillId;
            timeline.isLoop = isLoop;
            timeline.AddGroup("DefaultTestGroup");
            SetDuration(timeline, duration);
            return timeline;
        }

        /// <summary>
        /// 向时间轴添加一个 MockClip，并自动挂载在 MockTrack 上
        /// </summary>
        public static MockClip AddMockClip(this ActionTimeline timeline, float startTime, float duration, string clipName = "MockClip")
        {
            if (timeline.Groups == null || timeline.Groups.Count == 0)
            {
                timeline.AddGroup("DefaultTestGroup");
            }

            var group = timeline.Groups[0];
            MockTrack track = null;
            if (group.tracks != null)
            {
                foreach (var t in group.tracks)
                {
                    if (t is MockTrack mt)
                    {
                        track = mt;
                        break;
                    }
                }
            }

            if (track == null)
            {
                track = new MockTrack();
                if (group.tracks == null)
                {
                    group.tracks = new System.Collections.Generic.List<TrackBase>();
                }
                group.tracks.Add(track);
            }

            var clip = new MockClip(startTime, duration, clipName);
            track.clips.Add(clip);

            // 若添加片段后时间超过当前 duration，自动扩容
            float clipEnd = startTime + duration;
            if (clipEnd > timeline.Duration)
            {
                SetDuration(timeline, clipEnd);
            }

            return clip;
        }

        /// <summary>
        /// 链式添加 MockClip 辅助扩展
        /// </summary>
        public static ActionTimeline WithMockClip(this ActionTimeline timeline, float startTime, float duration, out MockClip clip, string clipName = "MockClip")
        {
            clip = AddMockClip(timeline, startTime, duration, clipName);
            return timeline;
        }

        /// <summary>
        /// 链式添加 MockClip 辅助扩展（无需 out 参数）
        /// </summary>
        public static ActionTimeline WithMockClip(this ActionTimeline timeline, float startTime, float duration, string clipName = "MockClip")
        {
            AddMockClip(timeline, startTime, duration, clipName);
            return timeline;
        }

        /// <summary>
        /// 反射设定私有 duration 字段
        /// </summary>
        public static void SetDuration(ActionTimeline timeline, float duration)
        {
            if (timeline == null) return;
            var field = typeof(ActionTimeline).GetField("duration", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(timeline, duration);
        }
    }
}
