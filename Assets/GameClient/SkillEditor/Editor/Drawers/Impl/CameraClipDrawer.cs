using UnityEditor;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(CameraClip))]
    public class CameraClipDrawer : ClipDrawer
    {
        private const string PREF_KEYFRAME_VALUE = "CameraClipDrawer_CustomKeyframeValue";

        public override void DrawInspector(ClipBase clip)
        {
            var camClip = clip as CameraClip;
            if (camClip == null) return;
            
            EditorGUILayout.LabelField("相机运镜设置", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            base.DrawInspector(clip);
            if (EditorGUI.EndChangeCheck())
            {
                // 可以触发重绘
            }

            GUILayout.Space(15);
            EditorGUILayout.LabelField("快捷录制工具", EditorStyles.boldLabel);

            SkillEditorWindow window = null;
            if (EditorWindow.HasOpenInstances<SkillEditorWindow>())
            {
                window = EditorWindow.GetWindow<SkillEditorWindow>(false, "技能编辑器", false);
            }

            if (window != null)
            {
                var curve = camClip.pathCurve;

                // 1. 首尾点快捷调节面板
                if (curve != null && curve.keys.Length >= 2)
                {
                    EditorGUILayout.LabelField("端点快捷控制 (Time, Value)", EditorStyles.miniBoldLabel);
                    var firstKey = curve.keys[0];
                    var lastKey = curve.keys[curve.keys.Length - 1];

                    Vector2 startPoint = new Vector2(firstKey.time, firstKey.value);
                    Vector2 endPoint = new Vector2(lastKey.time, lastKey.value);

                    EditorGUI.BeginChangeCheck();
                    startPoint = EditorGUILayout.Vector2Field("起始点", startPoint);
                    endPoint = EditorGUILayout.Vector2Field("结束点", endPoint);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(window.GetCurrentTimeline(), "Update Camera Curve Endpoints");
                        
                        firstKey.time = startPoint.x;
                        firstKey.value = startPoint.y;
                        curve.MoveKey(0, firstKey);

                        lastKey.time = endPoint.x;
                        lastKey.value = endPoint.y;
                        curve.MoveKey(curve.keys.Length - 1, lastKey);

                        UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve, 0, UnityEditor.AnimationUtility.TangentMode.Auto);
                        UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, 0, UnityEditor.AnimationUtility.TangentMode.Auto);
                        UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve, curve.keys.Length - 1, UnityEditor.AnimationUtility.TangentMode.Auto);
                        UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, curve.keys.Length - 1, UnityEditor.AnimationUtility.TangentMode.Auto);
                        
                        EditorUtility.SetDirty(window.GetCurrentTimeline());
                        window.Repaint();
                    }
                }
                
                GUILayout.Space(10);
                EditorGUILayout.LabelField("定点关键帧插入", EditorStyles.miniBoldLabel);

                // 获取光标相对此 Clip 的时间点参数
                float timeIndicator = window.GetState() != null ? window.GetState().timeIndicator : 0f;
                float localTime = timeIndicator - camClip.StartTime;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"当前光标局地时间: {localTime:F2}s");
                GUILayout.EndHorizontal();

                float _customKeyframeValue = EditorPrefs.GetFloat(PREF_KEYFRAME_VALUE, 0f);
                
                EditorGUI.BeginChangeCheck();
                _customKeyframeValue = EditorGUILayout.FloatField("要插入的值 (Path Position)", _customKeyframeValue);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetFloat(PREF_KEYFRAME_VALUE, _customKeyframeValue);
                }

                bool inRange = localTime >= -0.1f && localTime <= camClip.Duration + 0.1f;
                using (new EditorGUI.DisabledScope(!inRange))
                {
                    if (GUILayout.Button($"在此刻 ({localTime:F2}s) 插入点", GUILayout.Height(30)))
                    {
                        float evalTime = camClip.sampleMode == CurveSampleMode.NormalizedTime
                                         ? Mathf.Clamp01(localTime / camClip.Duration)
                                         : localTime;

                        Undo.RecordObject(window.GetCurrentTimeline(), "Insert Custom Curve Key");
                        
                        int keyIndex = -1;
                        for (int i = 0; i < curve.keys.Length; i++)
                        {
                            if (Mathf.Abs(curve.keys[i].time - evalTime) < 0.001f)
                            {
                                keyIndex = i;
                                break;
                            }
                        }

                        if (keyIndex >= 0)
                        {
                            var key = curve.keys[keyIndex];
                            key.value = _customKeyframeValue;
                            curve.MoveKey(keyIndex, key);
                        }
                        else
                        {
                            Keyframe newKey = new Keyframe(evalTime, _customKeyframeValue);
                            curve.AddKey(newKey);
                        }
                        
                        for (int i = 0; i < curve.keys.Length; i++)
                        {
                            UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve, i, UnityEditor.AnimationUtility.TangentMode.Auto);
                            UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, i, UnityEditor.AnimationUtility.TangentMode.Auto);
                        }
                        EditorUtility.SetDirty(window.GetCurrentTimeline());
                        window.Repaint();
                    }
                }
                
                if (!inRange)
                {
                    EditorGUILayout.HelpBox("当前播放器时间轴指针不在该片段的活动辖区内，无法打下关键帧。", MessageType.Warning);
                }
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, SkillEditorState state, Color clipColor, string displayName)
        {
            // 1. 先绘制底部的标准高亮框与颜色背景
            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);

            // 2. 准备绘制相机曲线
            var camClip = clip as CameraClip;
            if (camClip == null || camClip.pathCurve == null || clipRect.width < 5f) return;

            // 计算曲线在此 clip 中的高度映射极值
            float maxCurveVal = 1f; 
            float minCurveVal = 0f;
            if (camClip.pathCurve.keys.Length > 0)
            {
                minCurveVal = camClip.pathCurve.keys[0].value;
                foreach (var k in camClip.pathCurve.keys)
                {
                    if (k.value > maxCurveVal) maxCurveVal = k.value;
                    if (k.value < minCurveVal) minCurveVal = k.value;
                }
            }
            float valRange = maxCurveVal - minCurveVal;
            if (valRange <= 0.001f) valRange = 1f; // 避免除0崩溃

            // 横向采样率（基于像素宽度，2个像素一个点足矣，限制峰值降低性能损耗）
            int resolution = Mathf.FloorToInt(clipRect.width / 2f);
            if (resolution < 2) return;
            if (resolution > 200) resolution = 200;

            Vector3[] points = new Vector3[resolution];
            float clipDuration = clip.Duration;
            if (clipDuration <= 0) return;

            float curveDisplayHeight = clipRect.height - 4f; 
            float bottomY = clipRect.y + clipRect.height - 2f; 

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                float px = clipRect.x + t * clipRect.width;

                float evalTime = t;
                if (camClip.sampleMode == CurveSampleMode.NormalizedTime)
                {
                    evalTime = t; // [0, 1] 映射
                }
                else
                {
                    evalTime = t * clipDuration; // 真实秒数映射
                }
                
                // 根据对应插值取样并限制越界
                float evalValue = camClip.pathCurve.Evaluate(evalTime);
                float hRatio = Mathf.Clamp01((evalValue - minCurveVal) / valRange);
                float py = bottomY - hRatio * curveDisplayHeight;

                points[i] = new Vector3(px, py, 0f);
            }

            // 3. 执行画布渲染 (抗锯齿矢量线)
            Handles.BeginGUI();
            // 在底板颜色上层画一条半透明霓虹绿的路径流线
            Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
            Handles.DrawAAPolyLine(3f, points);
            Handles.EndGUI();
        }
    }
}
