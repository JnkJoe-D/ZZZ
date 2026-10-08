using System;
using UnityEngine;
using UnityEditor;

namespace Game.Editor.ActionTransition
{
    public enum TimelineFpsMode
    {
        Fps60 = 60,
        Fps30 = 30,
        Fps15 = 15
    }

    public enum TimelineRulerUnit
    {
        Frames,   // 帧模式 (默认)
        Seconds   // 秒模式
    }

    /// <summary>
    /// 双轨联动动作过渡时间轴绘制与交互组件。
    /// 包含按帧绘制的刻度标尺（默认以帧模式绘制，支持 60/30/15 FPS 与秒模式切换）、上轨（源动作）、下轨（目标动作）、
    /// Crossfade 渐变混合梯形、右边缘锁定联动拖拽、ExitTime 模拟按键游标与帧吸附辅助线。
    /// </summary>
    public sealed class ActionTransitionTimelineDrawer
    {
        private const float RulerHeight = 22f;
        private const float TrackHeight = 28f;
        private const float TrackGap = 12f;
        private const float HandleWidth = 8f;

        private float _timeToPixelScale = 240f;
        private float _scrollOffsetX = 0f;
        private bool _userZoomed = false;

        // 标尺单位（默认以帧模式绘制而非秒模式）与帧率模式
        public TimelineRulerUnit RulerUnit { get; set; } = TimelineRulerUnit.Frames;
        public TimelineFpsMode FpsMode { get; set; } = TimelineFpsMode.Fps60;
        public int CurrentFps => (int)FpsMode;

        private float _lockedRightTime = 0f;
        private bool _isSnapping = false;
        private float _snappedTime = 0f;
        private int _snappedFrame = 0;

        private enum DraggingMode
        {
            None,
            Scrubber,
            ExitTimeHandle,
            CrossfadeHandle,
            PanView
        }

        private DraggingMode _currentDrag = DraggingMode.None;

        public Action<float> OnCrossfadeChanged;
        public Action<float> OnExitTimeChanged;
        public Action<float> OnScrubTimeChanged;

        /// <summary>
        /// 绘制时间轴并处理鼠标交互
        /// </summary>
        public void DrawTimeline(
            Rect totalRect,
            string sourceActionName,
            float sourceDuration,
            ActionClipInfoExtractor.RouteWindowRange routeWindow,
            string targetActionName,
            float targetDuration,
            float exitTime,
            float crossfadeDuration,
            float currentTime,
            bool isLoopFocus,
            float focusStartTime,
            float focusEndTime,
            bool hasStartTime = false,
            float startTime = 0f)
        {
            if (totalRect.width <= 10f || totalRect.height <= 10f) return;

            Event evt = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 背景底色
            EditorGUI.DrawRect(totalRect, new Color(0.13f, 0.13f, 0.15f, 1f));

            // 使用 GUI.BeginClip 将时间轴所有图元严格限制在自身区域内，坐标转换为局部 (0, 0) 起始，彻底杜绝越界穿透左侧列表
            GUI.BeginClip(totalRect);
            try
            {
                Rect localRect = new Rect(0, 0, totalRect.width, totalRect.height);
                float contentStartX = 8f - _scrollOffsetX;
                float availableWidth = totalRect.width - 16f;

                // 动态自适应缩放比（若用户未手动缩放）
                float effectiveStartTime = hasStartTime ? startTime : 0f;
                float effectiveTargetDuration = Mathf.Max(0.1f, targetDuration - effectiveStartTime);
                float maxTime = Mathf.Max(sourceDuration, exitTime + effectiveTargetDuration, exitTime + crossfadeDuration + 0.4f, 1.5f);
                if (!_userZoomed && maxTime * _timeToPixelScale < availableWidth)
                {
                    _timeToPixelScale = availableWidth / maxTime;
                }

                // 轨道局部几何布局
                Rect rulerRect = new Rect(0, 0, totalRect.width, RulerHeight);
                Rect track1Rect = new Rect(0, rulerRect.yMax + 4f, totalRect.width, TrackHeight);
                Rect track2Rect = new Rect(0, track1Rect.yMax + TrackGap, totalRect.width, TrackHeight);

                // 1. 绘制刻度尺（铺满整个时间轴视口区域，支持超高放大倍率与防重叠动态步长）
                DrawRuler(rulerRect, contentStartX, totalRect.width);

                // 2. 局部循环（Loop Focus）范围高亮
                if (isLoopFocus)
                {
                    float focusX1 = contentStartX + focusStartTime * _timeToPixelScale;
                    float focusX2 = contentStartX + focusEndTime * _timeToPixelScale;
                    Rect focusRect = new Rect(focusX1, rulerRect.yMax, Mathf.Max(4f, focusX2 - focusX1), track2Rect.yMax - rulerRect.yMax);
                    EditorGUI.DrawRect(focusRect, new Color(0.15f, 0.75f, 0.35f, 0.08f));
                }

                // 3. 绘制 Track 1: 源动作 (深靛蓝)
                DrawSourceTrack(track1Rect, contentStartX, sourceActionName, sourceDuration, routeWindow);

                // 4. 绘制 Track 2: 目标动作 (翡翠绿)
                DrawTargetTrack(track2Rect, contentStartX, targetActionName, targetDuration, exitTime, hasStartTime, startTime);

                // 5. 绘制 Crossfade 混合梯形与右边缘手柄（居中连接两轨，风格与左侧对称）
                DrawCrossfadeTrapezoid(track1Rect, track2Rect, contentStartX, exitTime, crossfadeDuration);

                // 6. 绘制 ExitTime 游标
                DrawExitTimeCursor(track1Rect, track2Rect, contentStartX, exitTime);

                // 7. 绘制 Scrubber 播放指针红线
                DrawScrubber(rulerRect, track2Rect, contentStartX, currentTime);

                // 8. 绘制帧吸附高亮辅助线与悬浮帧数提示
                if (_isSnapping)
                {
                    DrawSnappingGuide(rulerRect, track2Rect, contentStartX);
                }

                // 9. 鼠标事件处理（局部坐标，支持右边界锁定与整帧吸附）
                HandleMouseInput(evt, localRect, contentStartX, exitTime, crossfadeDuration, maxTime, controlID);
            }
            finally
            {
                GUI.EndClip();
            }
        }

        private void DrawRuler(Rect rulerRect, float contentStartX, float viewportWidth)
        {
            EditorGUI.DrawRect(rulerRect, new Color(0.17f, 0.17f, 0.19f, 1f));
            Handles.color = new Color(0.35f, 0.35f, 0.4f, 0.6f);
            Handles.DrawLine(new Vector2(rulerRect.x, rulerRect.yMax), new Vector2(rulerRect.xMax, rulerRect.yMax));

            // 计算当前屏幕视口覆盖的可见时间区间（铺满整个时间轴视口）
            float viewStartTime = Mathf.Max(0f, (-40f - contentStartX) / _timeToPixelScale);
            float viewEndTime = Mathf.Max(0f, (viewportWidth + 40f - contentStartX) / _timeToPixelScale);

            int fps = CurrentFps;
            float frameDt = 1f / fps;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.72f, 0.75f, 0.82f) },
                fontSize = 9
            };

            if (RulerUnit == TimelineRulerUnit.Frames)
            {
                // 【帧模式】根据单帧像素宽度 pixelsPerFrame 动态计算步长
                float pixelsPerFrame = _timeToPixelScale * frameDt;

                // 候选主刻度步长（确保相邻标尺示数间距永远保持在 45px~120px 之间，彻底避免重叠）
                int[] candidateSteps = new int[] { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1200, 1800, 3600, 7200 };
                float idealStep = 45f / Mathf.Max(0.001f, pixelsPerFrame);
                int majorFrameStep = candidateSteps[candidateSteps.Length - 1];
                for (int i = 0; i < candidateSteps.Length; i++)
                {
                    if (candidateSteps[i] >= idealStep)
                    {
                        majorFrameStep = candidateSteps[i];
                        break;
                    }
                }

                // 次级刻度（小短竖线）步进自适应
                int minorFrameStep;
                if (majorFrameStep == 1)
                {
                    minorFrameStep = 1; // 放大到图 1 的程度：每 1 帧均为主刻度竖线 + 帧号示数
                }
                else if (pixelsPerFrame >= 10f)
                {
                    minorFrameStep = 1;
                }
                else if (pixelsPerFrame >= 3.5f)
                {
                    minorFrameStep = (majorFrameStep % 5 == 0) ? 5 : 2;
                }
                else
                {
                    minorFrameStep = majorFrameStep >= 10 ? majorFrameStep / 5 : majorFrameStep;
                }
                if (minorFrameStep < 1) minorFrameStep = 1;

                // 铺满视口全宽：从对齐步长的 startFrame 循环至 endFrame
                int startFrame = Mathf.Max(0, Mathf.FloorToInt(viewStartTime * fps) - 2);
                startFrame = (startFrame / minorFrameStep) * minorFrameStep;
                int endFrame = Mathf.Max(0, Mathf.CeilToInt(viewEndTime * fps) + 5);

                for (int f = startFrame; f <= endFrame; f += minorFrameStep)
                {
                    float t = f * frameDt;
                    float x = contentStartX + t * _timeToPixelScale;
                    if (x < rulerRect.x - 20f || x > rulerRect.xMax + 20f) continue;

                    bool isMajor = (f % majorFrameStep == 0);
                    float lineH = isMajor ? 11f : 5f;

                    Handles.color = isMajor ? new Color(0.85f, 0.88f, 0.95f, 0.9f) : new Color(0.42f, 0.45f, 0.52f, 0.45f);
                    Handles.DrawLine(new Vector2(x, rulerRect.yMax - lineH), new Vector2(x, rulerRect.yMax));

                    if (isMajor)
                    {
                        // 图 1 风格：文字紧随刻度线右侧 |1593   |1594
                        GUI.Label(new Rect(x + 2f, rulerRect.y + 1f, 48f, 15f), $"{f}", labelStyle);
                    }
                }

                // 右上角标尺帧率标签
                GUIStyle fpsBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    normal = { textColor = new Color(0.4f, 0.85f, 0.6f, 0.85f) },
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 9
                };
                GUI.Label(new Rect(rulerRect.xMax - 95f, rulerRect.y + 2f, 90f, 14f), $"{fps} FPS [帧模式]", fpsBadgeStyle);
            }
            else
            {
                // 【秒模式】铺满全宽自适应防重叠
                float[] candidateSecSteps = new float[] { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.5f, 1.0f, 2.0f, 5.0f, 10.0f, 30.0f, 60.0f, 120.0f };
                float idealSecStep = 55f / Mathf.Max(0.001f, _timeToPixelScale);
                float majorSecStep = candidateSecSteps[candidateSecSteps.Length - 1];
                for (int i = 0; i < candidateSecSteps.Length; i++)
                {
                    if (candidateSecSteps[i] >= idealSecStep)
                    {
                        majorSecStep = candidateSecSteps[i];
                        break;
                    }
                }

                float minorSecStep = majorSecStep >= 0.1f ? majorSecStep / 5f : majorSecStep;
                int startSecIndex = Mathf.Max(0, Mathf.FloorToInt(viewStartTime / minorSecStep) - 1);
                int endSecIndex = Mathf.Max(0, Mathf.CeilToInt(viewEndTime / minorSecStep) + 2);

                for (int s = startSecIndex; s <= endSecIndex; s++)
                {
                    float t = s * minorSecStep;
                    float x = contentStartX + t * _timeToPixelScale;
                    if (x < rulerRect.x - 20f || x > rulerRect.xMax + 20f) continue;

                    bool isMajor = Mathf.Abs((t / majorSecStep) - Mathf.Round(t / majorSecStep)) < 0.001f;
                    float lineH = isMajor ? 11f : 5f;

                    Handles.color = isMajor ? new Color(0.85f, 0.88f, 0.95f, 0.9f) : new Color(0.42f, 0.45f, 0.52f, 0.45f);
                    Handles.DrawLine(new Vector2(x, rulerRect.yMax - lineH), new Vector2(x, rulerRect.yMax));

                    if (isMajor)
                    {
                        string secText = majorSecStep < 0.1f ? $"{t:0.00}s" : (majorSecStep < 1f ? $"{t:0.0}s" : $"{t:0}s");
                        GUI.Label(new Rect(x + 2f, rulerRect.y + 1f, 48f, 15f), secText, labelStyle);
                    }
                }

                GUIStyle secBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    normal = { textColor = new Color(0.85f, 0.75f, 0.4f, 0.85f) },
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 9
                };
                GUI.Label(new Rect(rulerRect.xMax - 75f, rulerRect.y + 2f, 70f, 14f), "[秒模式]", secBadgeStyle);
            }
        }

        private void DrawSnappingGuide(Rect rulerRect, Rect track2, float contentStartX)
        {
            float snapX = contentStartX + _snappedTime * _timeToPixelScale;

            // 金黄色穿透垂直吸附参考线
            Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            Handles.DrawLine(new Vector2(snapX, rulerRect.y), new Vector2(snapX, track2.yMax + 4f));

            // 吸附帧数与时间提示气泡
            GUIStyle tipStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(1f, 0.95f, 0.4f) },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };
            Rect badgeRect = new Rect(snapX - 34f, rulerRect.yMax + 2f, 68f, 15f);
            EditorGUI.DrawRect(badgeRect, new Color(0.12f, 0.12f, 0.14f, 0.92f));
            Handles.color = new Color(1f, 0.85f, 0.2f, 0.85f);
            Handles.DrawWireCube(badgeRect.center, badgeRect.size);
            GUI.Label(badgeRect, $"{_snappedFrame}F ({_snappedTime:0.00}s)", tipStyle);
        }

        public float SnapTimeToFrame(float time, out int frame)
        {
            int fps = CurrentFps;
            frame = Mathf.Max(0, Mathf.RoundToInt(time * fps));
            return (float)frame / fps;
        }

        private void DrawSourceTrack(Rect rect, float startX, string actionName, float duration, ActionClipInfoExtractor.RouteWindowRange routeWindow)
        {
            float blockWidth = Mathf.Max(12f, duration * _timeToPixelScale);
            Rect blockRect = new Rect(startX, rect.y, blockWidth, rect.height);

            // 深靛蓝底色与高亮紫蓝边框
            EditorGUI.DrawRect(blockRect, new Color(0.14f, 0.22f, 0.32f, 0.95f));

            Handles.color = new Color(0.25f, 0.5f, 0.85f, 0.75f);
            Handles.DrawWireCube(new Vector3(blockRect.center.x, blockRect.center.y, 0f), new Vector3(blockRect.width, blockRect.height, 0f));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.85f, 0.92f, 1f) },
                fontSize = 10
            };

            int srcFrames = Mathf.RoundToInt(duration * CurrentFps);
            string durText = RulerUnit == TimelineRulerUnit.Frames ? $"{srcFrames}F ({duration:0.00}s)" : $"{duration:0.00}s ({srcFrames}F)";
            GUI.Label(new Rect(blockRect.x + 6f, blockRect.y + 6f, blockRect.width - 12f, 16f), $"源动作: {actionName} ({durText})", labelStyle);
        }

        private void DrawTargetTrack(Rect rect, float startX, string actionName, float duration, float exitTime, bool hasStartTime, float startTime)
        {
            float effectiveStartTime = hasStartTime ? startTime : 0f;
            float effectiveDuration = Mathf.Max(0.05f, duration - effectiveStartTime);

            float blockX = startX + exitTime * _timeToPixelScale;
            float blockWidth = Mathf.Max(12f, effectiveDuration * _timeToPixelScale);
            Rect blockRect = new Rect(blockX, rect.y, blockWidth, rect.height);

            // 深翡翠绿底色与高亮碧绿边框
            EditorGUI.DrawRect(blockRect, new Color(0.12f, 0.25f, 0.20f, 0.95f));

            Handles.color = new Color(0.2f, 0.75f, 0.45f, 0.75f);
            Handles.DrawWireCube(new Vector3(blockRect.center.x, blockRect.center.y, 0f), new Vector3(blockRect.width, blockRect.height, 0f));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.85f, 1f, 0.92f) },
                fontSize = 10
            };

            int tgtFrames = Mathf.RoundToInt(effectiveDuration * CurrentFps);
            string durText = RulerUnit == TimelineRulerUnit.Frames ? $"{tgtFrames}F ({effectiveDuration:0.00}s)" : $"{effectiveDuration:0.00}s ({tgtFrames}F)";
            string startInfo = hasStartTime && startTime > 0.001f ? $" [起切: {startTime:0.00}s]" : "";
            string title = !string.IsNullOrEmpty(actionName) ? $"目标动作: {actionName}{startInfo} ({durText})" : $"目标动作: (未配置) ({durText})";
            GUI.Label(new Rect(blockRect.x + 6f, blockRect.y + 6f, blockRect.width - 12f, 16f), title, labelStyle);
        }

        private void DrawCrossfadeTrapezoid(Rect track1, Rect track2, float startX, float exitTime, float crossfadeDuration)
        {
            if (crossfadeDuration <= 0.001f) return;

            float fadeStartX = startX + exitTime * _timeToPixelScale;
            float fadeWidth = crossfadeDuration * _timeToPixelScale;

            // 梯形区域：精准连接上轨顶部到下轨底部，垂直居中覆盖中间间距区
            Rect fadeRect = new Rect(fadeStartX, track1.y, fadeWidth, track2.yMax - track1.y);

            // 半透明紫青色填充
            EditorGUI.DrawRect(fadeRect, new Color(0.42f, 0.25f, 0.85f, 0.32f));

            // 对角交叉渐变线
            Handles.color = new Color(0.75f, 0.6f, 1f, 0.85f);
            Handles.DrawLine(new Vector2(fadeStartX, track1.y), new Vector2(fadeStartX + fadeWidth, track2.yMax)); // A 渐出
            Handles.color = new Color(0.2f, 0.9f, 0.85f, 0.85f);
            Handles.DrawLine(new Vector2(fadeStartX, track2.yMax), new Vector2(fadeStartX + fadeWidth, track1.y)); // B 渐入

            // 右边缘拖拽手柄：与左侧 ExitTime 游标风格对称一致（细长主竖线 + 顶部/底部精致三角指示）
            float rightX = fadeStartX + fadeWidth;
            Handles.color = new Color(0.25f, 0.85f, 1f, 0.95f);
            Handles.DrawLine(new Vector2(rightX, track1.y - 4f), new Vector2(rightX, track2.yMax + 2f));

            // 顶部倒三角手柄
            Vector3[] rightTriangleTop = new Vector3[]
            {
                new Vector3(rightX - 5f, track1.y - 7f, 0f),
                new Vector3(rightX + 5f, track1.y - 7f, 0f),
                new Vector3(rightX, track1.y - 1f, 0f)
            };
            Handles.DrawAAConvexPolygon(rightTriangleTop);

            // 底部正三角手柄
            Vector3[] rightTriangleBottom = new Vector3[]
            {
                new Vector3(rightX - 5f, track2.yMax + 5f, 0f),
                new Vector3(rightX + 5f, track2.yMax + 5f, 0f),
                new Vector3(rightX, track2.yMax - 1f, 0f)
            };
            Handles.DrawAAConvexPolygon(rightTriangleBottom);

            Rect handleHitRect = new Rect(rightX - 6f, track1.y - 8f, 12f, track2.yMax - track1.y + 16f);
            EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.ResizeHorizontal);

            // 中间提示文字（居中在轨道间距区域）
            GUIStyle tipStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(0.4f, 1f, 1f) },
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter
            };
            Rect textRect = new Rect(fadeStartX, track1.yMax - 1f, fadeWidth, TrackGap + 2f);
            GUI.Label(textRect, $"{crossfadeDuration:0.00}s", tipStyle);
        }

        private void DrawExitTimeCursor(Rect track1, Rect track2, float startX, float exitTime)
        {
            float cursorX = startX + exitTime * _timeToPixelScale;

            // 青绿垂直线
            Handles.color = new Color(0.3f, 1f, 0.65f, 0.95f);
            Handles.DrawLine(new Vector2(cursorX, track1.y - 4f), new Vector2(cursorX, track2.yMax + 2f));

            // 顶部倒三角手柄
            Vector3[] triangle = new Vector3[]
            {
                new Vector3(cursorX - 5f, track1.y - 7f, 0f),
                new Vector3(cursorX + 5f, track1.y - 7f, 0f),
                new Vector3(cursorX, track1.y - 1f, 0f)
            };
            Handles.DrawAAConvexPolygon(triangle);

            Rect handleHitRect = new Rect(cursorX - 6f, track1.y - 8f, 12f, track2.yMax - track1.y + 10f);
            EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.SlideArrow);
        }

        private void DrawScrubber(Rect rulerRect, Rect track2, float startX, float currentTime)
        {
            float scrubX = startX + currentTime * _timeToPixelScale;

            // 红色垂直线
            Handles.color = new Color(1f, 0.22f, 0.22f, 0.95f);
            Handles.DrawLine(new Vector2(scrubX, rulerRect.y), new Vector2(scrubX, track2.yMax + 2f));

            // 刻度尺顶部的倒三角红色手柄
            Vector3[] head = new Vector3[]
            {
                new Vector3(scrubX - 5f, rulerRect.y, 0f),
                new Vector3(scrubX + 5f, rulerRect.y, 0f),
                new Vector3(scrubX, rulerRect.y + 7f, 0f)
            };
            Handles.DrawAAConvexPolygon(head);
        }

        private void HandleMouseInput(Event evt, Rect totalRect, float startX, float exitTime, float crossfadeDuration, float maxTime, int controlID)
        {
            if (!totalRect.Contains(evt.mousePosition) && _currentDrag == DraggingMode.None) return;

            float mouseTime = Mathf.Max(0f, (evt.mousePosition.x - startX) / _timeToPixelScale);

            switch (evt.type)
            {
                case EventType.ScrollWheel:
                    // 统一滚轮为平滑时间轴缩放（以鼠标当前指向的时间点为中心锚点）
                    float mouseTimeInScale = Mathf.Max(0f, (evt.mousePosition.x - startX) / _timeToPixelScale);
                    float zoomMultiplier = 1.0f - evt.delta.y * 0.12f;
                    float newScale = Mathf.Clamp(_timeToPixelScale * zoomMultiplier, 20f, 15000f);

                    // 维持鼠标所指向的虚拟时间点屏幕像素位置不变（基于局部坐标 8f）
                    float newStartX = evt.mousePosition.x - mouseTimeInScale * newScale;
                    _scrollOffsetX = Mathf.Max(0f, 8f - newStartX);
                    _timeToPixelScale = newScale;
                    _userZoomed = true;
                    evt.Use();
                    break;

                case EventType.MouseDown:
                    if (evt.button == 2 || (evt.button == 0 && evt.alt))
                    {
                        // 中键或 Alt+左键：平移时间轴
                        _currentDrag = DraggingMode.PanView;
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    else if (evt.button == 0)
                    {
                        float exitX = startX + exitTime * _timeToPixelScale;
                        float fadeRightX = startX + (exitTime + crossfadeDuration) * _timeToPixelScale;

                        if (Mathf.Abs(evt.mousePosition.x - fadeRightX) <= 6f)
                        {
                            _currentDrag = DraggingMode.CrossfadeHandle;
                            GUIUtility.hotControl = controlID;
                            _isSnapping = true;
                            _snappedTime = SnapTimeToFrame(exitTime + crossfadeDuration, out _snappedFrame);
                            evt.Use();
                        }
                        else if (Mathf.Abs(evt.mousePosition.x - exitX) <= 6f)
                        {
                            _currentDrag = DraggingMode.ExitTimeHandle;
                            GUIUtility.hotControl = controlID;
                            // 关键锁定：按下左手柄时，固化右边界绝对世界时间戳！
                            _lockedRightTime = exitTime + crossfadeDuration;
                            _isSnapping = true;
                            _snappedTime = SnapTimeToFrame(exitTime, out _snappedFrame);
                            evt.Use();
                        }
                        else
                        {
                            _currentDrag = DraggingMode.Scrubber;
                            GUIUtility.hotControl = controlID;
                            _isSnapping = true;
                            float scrubTime = SnapTimeToFrame(mouseTime, out _snappedFrame);
                            _snappedTime = scrubTime;
                            OnScrubTimeChanged?.Invoke(scrubTime);
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (_currentDrag == DraggingMode.PanView)
                        {
                            _scrollOffsetX = Mathf.Max(0f, _scrollOffsetX - evt.delta.x);
                            evt.Use();
                        }
                        else if (_currentDrag == DraggingMode.CrossfadeHandle)
                        {
                            // 拖动右边界：右边界帧吸附
                            float snappedRightTime = SnapTimeToFrame(mouseTime, out _snappedFrame);
                            snappedRightTime = Mathf.Max(exitTime, snappedRightTime);
                            _snappedTime = snappedRightTime;

                            float newFade = snappedRightTime - exitTime;
                            newFade = SnapTimeToFrame(newFade, out _);
                            OnCrossfadeChanged?.Invoke(newFade);
                            evt.Use();
                        }
                        else if (_currentDrag == DraggingMode.ExitTimeHandle)
                        {
                            // 核心：拖动左边界时，右边界绝对位置锁定不动！
                            float snappedExit = SnapTimeToFrame(mouseTime, out _snappedFrame);
                            // 限制不能小于 0，且不能超过锁定的右边界
                            snappedExit = Mathf.Clamp(snappedExit, 0f, _lockedRightTime);
                            _snappedTime = snappedExit;

                            // 右边界锁定不变，反求混合时长并吸附
                            float snappedFade = Mathf.Max(0f, _lockedRightTime - snappedExit);
                            snappedFade = SnapTimeToFrame(snappedFade, out _);

                            OnExitTimeChanged?.Invoke(snappedExit);
                            OnCrossfadeChanged?.Invoke(snappedFade);
                            evt.Use();
                        }
                        else if (_currentDrag == DraggingMode.Scrubber)
                        {
                            float scrubTime = SnapTimeToFrame(mouseTime, out _snappedFrame);
                            _snappedTime = scrubTime;
                            OnScrubTimeChanged?.Invoke(scrubTime);
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        _currentDrag = DraggingMode.None;
                        _isSnapping = false;
                        evt.Use();
                    }
                    break;
            }
        }
    }
}
