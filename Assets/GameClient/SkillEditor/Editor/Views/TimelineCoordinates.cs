using UnityEngine;
using System.Collections.Generic;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 时间轴坐标转换、吸附和刻度计算工具类
    /// 无状态辅助类，通过 SkillEditorState 读取缩放/偏移等参数
    /// </summary>
    public class TimelineCoordinates
    {
        private SkillEditorState state;

        // 常量
        public const float TIMELINE_START_OFFSET = 10f; // 时间轴左侧偏移量（防止 0 刻度遮挡）

        public TimelineCoordinates(SkillEditorState state)
        {
            this.state = state;
        }

        #region 坐标转换

        /// <summary>
        /// 时间转换为像素位置
        /// </summary>
        public float TimeToPixel(float time)
        {
            return time * state.zoom;
        }

        /// <summary>
        /// 像素位置转换为时间
        /// </summary>
        public float PixelToTime(float pixel)
        {
            return pixel / state.zoom;
        }

        /// <summary>
        /// 时间转换为物理像素 X（包含滚动偏移和起始边距）
        /// </summary>
        public float TimeToPhysX(float time)
        {
            return time * state.zoom - state.scrollOffset + TIMELINE_START_OFFSET;
        }

        /// <summary>
        /// 物理像素 X 转换为时间（包含滚动偏移和起始边距）
        /// </summary>
        public float PhysXToTime(float physX)
        {
            return (physX - TIMELINE_START_OFFSET + state.scrollOffset) / state.zoom;
        }

        #endregion

        #region 缩放

        /// <summary>
        /// 获取当前缩放级别
        /// </summary>
        public float GetZoom()
        {
            return state.zoom;
        }

        /// <summary>
        /// 计算主刻度间隔（根据缩放级别自适应）
        /// </summary>
        public float CalculateMajorInterval()
        {
            float currentZoom = state.zoom;
            if (currentZoom >= 150f) return 0.5f;
            if (currentZoom >= 80f) return 1.0f;
            if (currentZoom >= 40f) return 2.0f;
            if (currentZoom >= 20f) return 5.0f;
            return 10.0f;
        }

        #endregion

        #region 吸附

        /// <summary>
        /// 计算吸附后的时间（简化版，不返回吸附状态）
        /// </summary>
        public float SnapTime(float time)
        {
            bool snapped;
            float dist;
            return SnapTime(time, null, out snapped, out dist);
        }

        /// <summary>
        /// 判断 value 是否为 step 的倍数（带误差容忍）
        /// </summary>
        public bool IsMultiple(float value, float step)
        {
            if (step <= 0) return false;
            float remainder = Mathf.Abs(value % step);
            float epsilon = Mathf.Min(0.001f, step * 0.01f);
            return remainder < epsilon || Mathf.Abs(remainder - step) < epsilon;
        }

        /// <summary>
        /// 计算吸附后的时间（完整版，返回吸附状态和距离）
        /// </summary>
        /// <param name="time">原始时间</param>
        /// <param name="excludeClip">排除的片段（正在拖拽的片段）</param>
        /// <param name="snapped">是否吸附到某个目标</param>
        /// <param name="minPixelDist">最小像素距离</param>
        public float SnapTime(float time, ClipBase excludeClip, out bool snapped, out float minPixelDist)
        {
            snapped = false;
            minPixelDist = state.snapThreshold;

            // 综合判断：全局开关开启 且 未按住Alt键
            bool isSnappingActive = state.snapEnabled && !Event.current.alt;

            if (!isSnappingActive)
            {
                // 如果 FrameSnap 开启，依然返回量化时间
                if (state.useFrameSnap && state.frameRate > 0)
                {
                    float interval = 1f / state.frameRate;
                    return Mathf.Round(time / interval) * interval;
                }
                return time;
            }

            float bestSnapTime = -1f;

            // 1. 吸附到时间轴指针
            float distToIndicator = Mathf.Abs(time - state.timeIndicator);
            float pixelDistToIndicator = TimeToPixel(distToIndicator);
            if (pixelDistToIndicator <= minPixelDist)
            {
                minPixelDist = pixelDistToIndicator;
                bestSnapTime = state.timeIndicator;
            }

            // 2. 吸附到其他片段的边沿（跨轨道全量吸附）
            SkillTimeline timeline = state.currentTimeline;
            if (timeline != null)
            {
                foreach (var track in timeline.AllTracks)
                {
                    for (int j = 0; j < track.clips.Count; j++)
                    {
                        var clip = track.clips[j];
                        if (clip == excludeClip) continue;

                        float[] targets = { clip.StartTime, clip.StartTime + clip.Duration };
                        for (int k = 0; k < targets.Length; k++)
                        {
                            float target = targets[k];
                            float dist = Mathf.Abs(time - target);
                            float pixelDist = TimeToPixel(dist);
                            if (pixelDist <= minPixelDist)
                            {
                                minPixelDist = pixelDist;
                                bestSnapTime = target;
                            }
                        }
                    }
                }
            }

            // 3. 吸附到网格（优先级最低）
            {
                float snapInterval = GetSnapInterval();
                float snappedGridTime = Mathf.Round(time / snapInterval) * snapInterval;
                float pixelDistToGrid = Mathf.Abs(TimeToPixel(time) - TimeToPixel(snappedGridTime));
                if (pixelDistToGrid <= minPixelDist)
                {
                    minPixelDist = pixelDistToGrid;
                    bestSnapTime = snappedGridTime;
                }
            }

            // 如果找到了高优先级吸附点 (Magnet)
            if (bestSnapTime >= 0)
            {
                snapped = true;
                return bestSnapTime;
            }

            // 4. Fallback: 帧锁定量化
            bool frameLockActive = state.useFrameSnap && state.frameRate > 0;
            if (frameLockActive)
            {
                float interval = 1f / state.frameRate;
                return Mathf.Round(time / interval) * interval;
            }

            return time;
        }

        /// <summary>
        /// 根据缩放级别获取吸附间隔
        /// </summary>
        public float GetSnapInterval()
        {
            CalculateRulerLevels(out _, out _, out float gridStep, out bool useFrameIndex);

            if (useFrameIndex)
            {
                return gridStep * (1f / state.frameRate);
            }
            else
            {
                return gridStep;
            }
        }

        #endregion

        #region 刻度计算

        /// <summary>
        /// 计算标尺的分级刻度 (LOD系统)
        /// </summary>
        public void CalculateRulerLevels(out float majorStep, out float subStep, out float gridStep, out bool useFrameIndex)
        {
            float pixelsPerUnit;
            List<float> validSteps = new List<float>();

            useFrameIndex = (state.useFrameSnap && state.frameRate > 0);

            if (useFrameIndex)
            {
                // 固定帧模式：单位为帧 (Frame Index)
                pixelsPerUnit = (1f / state.frameRate) * state.zoom;
                int fps = state.frameRate;

                // 分钟级
                validSteps.Add(fps * 600);
                validSteps.Add(fps * 60);

                // 秒级
                validSteps.Add(fps * 30);

                // 针对常见的 30/60 fps 优化
                if (fps % 60 == 0 || 60 % fps == 0)
                {
                    validSteps.Add(fps * 10);
                    validSteps.Add(fps * 5);
                    validSteps.Add(fps * 2);
                    validSteps.Add(fps);

                    if (fps >= 60) validSteps.Add(fps / 2);

                    validSteps.Add(60);
                    validSteps.Add(30);
                    validSteps.Add(10);
                    validSteps.Add(5);
                    validSteps.Add(1);
                }
                else
                {
                    validSteps.Add(fps * 10);
                    validSteps.Add(fps * 5);
                    validSteps.Add(fps * 2);
                    validSteps.Add(fps);
                    validSteps.Add(fps / 2);
                    validSteps.Add(10);
                    validSteps.Add(5);
                    validSteps.Add(1);
                }
            }
            else
            {
                // 自由模式：单位为秒
                pixelsPerUnit = state.zoom;
                validSteps.AddRange(new float[] {
                    600, 300, 60,
                    30, 10, 5, 2, 1,
                    0.5f, 0.1f,
                    0.05f, 0.01f
                });
            }

            // 1. Find Grid（最小可视 > 6px）
            gridStep = validSteps[validSteps.Count - 1];
            float gridPixelWidth = gridStep * pixelsPerUnit;

            for (int i = validSteps.Count - 1; i >= 0; i--)
            {
                if (validSteps[i] * pixelsPerUnit >= 6f)
                {
                    gridStep = validSteps[i];
                    gridPixelWidth = gridStep * pixelsPerUnit;
                    break;
                }
            }

            // 2. 如果 Grid 本身就很宽 (>45px)，则直接作为 Major
            if (gridPixelWidth >= 45f)
            {
                subStep = gridStep;
                majorStep = gridStep;
                return;
            }

            // 3. Find Sub（必须是 Grid 的倍数）
            subStep = gridStep;
            if (gridPixelWidth < 20f)
            {
                for (int i = validSteps.IndexOf(gridStep) - 1; i >= 0; i--)
                {
                    float step = validSteps[i];
                    if (Mathf.Abs(step % gridStep) > 0.001f) continue;

                    if (step * pixelsPerUnit >= 20f)
                    {
                        subStep = step;
                        break;
                    }
                }
            }

            // 4. Find Major（必须是 Sub 的倍数）
            majorStep = subStep;
            float subPixelWidth = subStep * pixelsPerUnit;

            if (subPixelWidth < 45f)
            {
                int subIndex = validSteps.IndexOf(subStep);
                if (subIndex >= 0)
                {
                    for (int i = subIndex - 1; i >= 0; i--)
                    {
                        float step = validSteps[i];
                        if (Mathf.Abs(step % subStep) > 0.001f) continue;

                        if (step * pixelsPerUnit >= 45f)
                        {
                            majorStep = step;
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region 重叠检测与位置查找

        /// <summary>
        /// 检测片段在指定位置是否与轨道上其他片段重叠
        /// </summary>
        public bool HasOverlap(TrackBase track, float startTime, float duration, ClipBase excludeClip = null)
        {
            float endTime = startTime + duration;

            foreach (var clip in track.clips)
            {
                if (clip == excludeClip) continue;

                if (!(endTime <= clip.StartTime || startTime >= clip.EndTime))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 查找从指定时间开始的下一个可用位置（不重叠）
        /// </summary>
        public float FindNextAvailableTime(TrackBase track, float preferredTime, float duration)
        {
            float testTime = SnapTime(preferredTime);
            const int maxAttempts = 100;
            int attempts = 0;

            while (HasOverlap(track, testTime, duration) && attempts < maxAttempts)
            {
                ClipBase blockingClip = null;
                for (int i = 0; i < track.clips.Count; i++)
                {
                    var clip = track.clips[i];
                    if (testTime < (clip.StartTime + clip.Duration) && testTime + duration > clip.StartTime)
                    {
                        blockingClip = clip;
                        break;
                    }
                }

                if (blockingClip != null)
                {
                    testTime = SnapTime(blockingClip.StartTime + blockingClip.Duration);
                }
                else
                {
                    break;
                }

                attempts++;
            }

            return testTime;
        }

        /// <summary>
        /// 检查轨道类型是否允许片段重叠
        /// </summary>
        public bool AllowsOverlap(TrackBase track)
        {
            return track.CanOverlap;
        }

        /// <summary>
        /// 自动处理重合部分的融合时长（Cross-fade）
        /// </summary>
        public void AutoResolveBlending(TrackBase track, ClipBase modifiedClip)
        {
            if (track == null || !AllowsOverlap(track)) return;

            foreach (var clip in track.clips)
            {
                if (clip == modifiedClip) continue;

                // Case 1: modifiedClip 头部 overlap 了 clip 的尾部
                if (modifiedClip.StartTime < clip.EndTime && modifiedClip.StartTime > clip.StartTime)
                {
                    float overlap = clip.EndTime - modifiedClip.StartTime;
                    clip.BlendOutDuration = overlap;
                    modifiedClip.BlendInDuration = overlap;
                }
                // Case 2: modifiedClip 尾部 overlap 了 clip 的头部
                else if (modifiedClip.EndTime > clip.StartTime && modifiedClip.EndTime < clip.EndTime)
                {
                    float overlap = modifiedClip.EndTime - clip.StartTime;
                    modifiedClip.BlendOutDuration = overlap;
                    clip.BlendInDuration = overlap;
                }
            }
        }

        #endregion

        #region 几何辅助

        /// <summary>
        /// 从两个点创建矩形（用于框选）
        /// </summary>
        public Rect GetRectFromPoints(Vector2 start, Vector2 end)
        {
            float x = Mathf.Min(start.x, end.x);
            float y = Mathf.Min(start.y, end.y);
            float width = Mathf.Abs(end.x - start.x);
            float height = Mathf.Abs(end.y - start.y);
            return new Rect(x, y, width, height);
        }

        #endregion
    }
}
