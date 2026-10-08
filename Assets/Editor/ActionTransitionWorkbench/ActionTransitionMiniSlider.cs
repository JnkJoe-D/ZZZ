using System;
using UnityEngine;
using UnityEditor;

namespace Game.Editor.ActionTransition
{
    public enum MiniSliderUnit
    {
        Seconds = 0,
        Frames = 1,
        Normalized = 2
    }

    public enum MiniSliderTheme
    {
        Source, // 源动作（深靛蓝底色，从 0 到 pointer 填充）
        Target  // 目标动作（深翡翠绿底色，从 pointer 到末尾填充）
    }

    /// <summary>
    /// 紧凑型长条时间轴滑块控件 (Mini Track Slider)。
    /// 包含不同动作语义底色、半透明片段覆盖层、整帧吸附指针与悬浮数值气泡。
    /// </summary>
    public sealed class ActionTransitionMiniSlider
    {
        public MiniSliderUnit Unit { get; set; } = MiniSliderUnit.Seconds;
        public MiniSliderTheme Theme { get; set; } = MiniSliderTheme.Source;
        public float Duration { get; set; } = 1.0f;
        public int Fps { get; set; } = 60;
        public float CurrentTime { get; set; } = 0f;
        public bool IsEnabled { get; set; } = true;

        public Action<float> OnValueChanged;

        private bool _isDragging = false;
        private int _controlID = 0;

        public void Draw(Rect rect)
        {
            if (rect.width <= 10f || rect.height <= 4f) return;

            Event evt = Event.current;
            _controlID = GUIUtility.GetControlID(FocusType.Passive);

            float safeDuration = Mathf.Max(0.01f, Duration);
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(safeDuration * Fps));
            bool isHovering = rect.Contains(evt.mousePosition) && IsEnabled;

            // 1. 处理鼠标事件（点击与拖拽）
            if (IsEnabled)
            {
                switch (evt.type)
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && rect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = _controlID;
                            _isDragging = true;
                            UpdateTimeFromMouse(evt.mousePosition.x, rect, safeDuration, totalFrames);
                            evt.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == _controlID)
                        {
                            UpdateTimeFromMouse(evt.mousePosition.x, rect, safeDuration, totalFrames);
                            evt.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == _controlID && evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            _isDragging = false;
                            evt.Use();
                        }
                        break;
                }
            }
            else
            {
                if (GUIUtility.hotControl == _controlID)
                {
                    GUIUtility.hotControl = 0;
                    _isDragging = false;
                }
            }

            // 2. 颜色方案
            Color bgColor;
            Color borderColor;
            Color fillColor;
            Color pointerColor;

            if (Theme == MiniSliderTheme.Source)
            {
                bgColor = IsEnabled ? new Color(0.12f, 0.17f, 0.25f, 1f) : new Color(0.15f, 0.15f, 0.17f, 0.6f);
                borderColor = IsEnabled ? new Color(0.25f, 0.42f, 0.68f, 0.85f) : new Color(0.28f, 0.28f, 0.32f, 0.5f);
                fillColor = IsEnabled ? new Color(0.22f, 0.55f, 0.95f, 0.38f) : new Color(0.35f, 0.35f, 0.38f, 0.2f);
                pointerColor = IsEnabled ? (_isDragging ? new Color(0.4f, 0.9f, 1f, 1f) : new Color(0.3f, 0.75f, 1f, 0.95f)) : new Color(0.5f, 0.5f, 0.55f, 0.5f);
            }
            else
            {
                bgColor = IsEnabled ? new Color(0.10f, 0.20f, 0.16f, 1f) : new Color(0.15f, 0.15f, 0.17f, 0.6f);
                borderColor = IsEnabled ? new Color(0.20f, 0.55f, 0.38f, 0.85f) : new Color(0.28f, 0.28f, 0.32f, 0.5f);
                fillColor = IsEnabled ? new Color(0.18f, 0.85f, 0.52f, 0.38f) : new Color(0.35f, 0.35f, 0.38f, 0.2f);
                pointerColor = IsEnabled ? (_isDragging ? new Color(0.5f, 1f, 0.75f, 1f) : new Color(0.3f, 0.9f, 0.6f, 0.95f)) : new Color(0.5f, 0.5f, 0.55f, 0.5f);
            }

            // 3. 绘制底板与外边框
            EditorGUI.DrawRect(rect, bgColor);
            Handles.color = borderColor;
            Handles.DrawWireCube(new Vector3(rect.center.x, rect.center.y, 0f), new Vector3(rect.width, rect.height, 0f));

            // 4. 计算当前指针 X 坐标
            float clampedTime = Mathf.Clamp(CurrentTime, 0f, safeDuration);
            float norm = clampedTime / safeDuration;
            float pointerX = Mathf.Clamp(rect.x + norm * rect.width, rect.x, rect.xMax);

            // 5. 绘制有效片段高亮覆盖层
            if (IsEnabled)
            {
                if (Theme == MiniSliderTheme.Source)
                {
                    // EndTime：源动作从 0 播放到 EndTime
                    float fillWidth = Mathf.Max(0f, pointerX - rect.x);
                    if (fillWidth > 0.5f)
                    {
                        Rect fillRect = new Rect(rect.x, rect.y, fillWidth, rect.height);
                        EditorGUI.DrawRect(fillRect, fillColor);
                    }
                }
                else
                {
                    // StartTime：目标动作从 StartTime 播放至尾
                    float fillWidth = Mathf.Max(0f, rect.xMax - pointerX);
                    if (fillWidth > 0.5f)
                    {
                        Rect fillRect = new Rect(pointerX, rect.y, fillWidth, rect.height);
                        EditorGUI.DrawRect(fillRect, fillColor);
                    }
                }
            }

            // 6. 绘制高亮指针（垂直指示线 + 顶部/底部精致三角形手柄）
            Handles.color = pointerColor;
            Handles.DrawLine(new Vector2(pointerX, rect.y), new Vector2(pointerX, rect.yMax));

            if (IsEnabled)
            {
                // 顶部倒三角
                Vector3[] topTriangle = new Vector3[]
                {
                    new Vector3(pointerX - 4f, rect.y - 1f, 0f),
                    new Vector3(pointerX + 4f, rect.y - 1f, 0f),
                    new Vector3(pointerX, rect.y + 4f, 0f)
                };
                Handles.DrawAAConvexPolygon(topTriangle);

                // 底部正三角
                Vector3[] bottomTriangle = new Vector3[]
                {
                    new Vector3(pointerX - 4f, rect.yMax + 1f, 0f),
                    new Vector3(pointerX + 4f, rect.yMax + 1f, 0f),
                    new Vector3(pointerX, rect.yMax - 4f, 0f)
                };
                Handles.DrawAAConvexPolygon(bottomTriangle);
            }

            // 7. 悬浮或拖拽时绘制悬浮气泡 (Tooltip Badge)
            if (IsEnabled && (isHovering || _isDragging))
            {
                DrawBadgeTooltip(rect, pointerX, clampedTime, norm, totalFrames);
            }
        }

        private void UpdateTimeFromMouse(float mouseX, Rect rect, float safeDuration, int totalFrames)
        {
            float norm = Mathf.Clamp01((mouseX - rect.x) / Mathf.Max(1f, rect.width));
            float targetTime;

            // 默认按整帧步长吸附，避免微弱抖动
            int frame = Mathf.Clamp(Mathf.RoundToInt(norm * totalFrames), 0, totalFrames);
            targetTime = (float)frame / Fps;
            targetTime = Mathf.Clamp(targetTime, 0f, safeDuration);

            if (Mathf.Abs(targetTime - CurrentTime) > 0.0001f)
            {
                CurrentTime = targetTime;
                OnValueChanged?.Invoke(CurrentTime);
            }
        }

        private void DrawBadgeTooltip(Rect trackRect, float pointerX, float currentTime, float norm, int totalFrames)
        {
            int currentFrame = Mathf.RoundToInt(currentTime * Fps);
            string tipText = Unit switch
            {
                MiniSliderUnit.Seconds => $"{currentTime:0.00}s ({currentFrame}F)",
                MiniSliderUnit.Frames => $"{currentFrame}F ({currentTime:0.00}s)",
                MiniSliderUnit.Normalized => $"{norm:0.00} ({currentFrame}F)",
                _ => $"{currentTime:0.00}s"
            };

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(1f, 0.92f, 0.45f) },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };

            float badgeWidth = 78f;
            float badgeHeight = 16f;
            float badgeX = Mathf.Clamp(pointerX - badgeWidth * 0.5f, trackRect.x, trackRect.xMax - badgeWidth);
            float badgeY = trackRect.y - badgeHeight - 4f;

            Rect badgeRect = new Rect(badgeX, badgeY, badgeWidth, badgeHeight);

            // 绘制深色半透明圆角阴影底与金黄色细边框
            EditorGUI.DrawRect(badgeRect, new Color(0.12f, 0.12f, 0.14f, 0.94f));
            Handles.color = new Color(1f, 0.85f, 0.25f, 0.88f);
            Handles.DrawWireCube(badgeRect.center, badgeRect.size);

            GUI.Label(badgeRect, tipText, badgeStyle);
        }
    }
}
