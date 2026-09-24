using UnityEditor;
using UnityEngine;
using ATEditor;
using MAnimSystem;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AnimationClip))]
    public class AnimationClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showAnimSettings = true;
        private static bool _showStartOffset = true;

        public override void DrawInspector(ClipBase clip)
        {
            var animClip = clip as AnimationClip;
            if (animClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 动画资源与配置卡片
            _showAnimSettings = EditorGUILayout.Foldout(_showAnimSettings, "动画配置", true, EditorStyles.foldoutHeader);
            if (_showAnimSettings)
            {
                EditorGUILayout.BeginVertical("box");

                animClip.animationClip = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField("动画资源", animClip.animationClip, typeof(UnityEngine.AnimationClip), false);
                animClip.playbackSpeed = EditorGUILayout.FloatField("播放速度", animClip.playbackSpeed);
                animClip.layer = (EAnimLayer)EditorGUILayout.EnumPopup("目标动画层", animClip.layer);
                animClip.overrideMask = (AvatarMask)EditorGUILayout.ObjectField("目标动画遮罩", animClip.overrideMask, typeof(AvatarMask), false);

                if (animClip.animationClip != null)
                {
                    EditorGUILayout.Space(6);
                    var content = EditorGUIUtility.IconContent("d_Refresh");
                    content.text = " 匹配动画时长 (" + animClip.animationClip.length.ToString("F2") + "s)";

                    if (GUILayout.Button(content, GUILayout.Height(26)))
                    {
                        var window = EditorWindow.GetWindow<ATEditorWindow>(false, "技能编辑器", false);
                        if (window != null)
                        {
                            var timeline = window.GetCurrentTimeline();
                            if (timeline != null)
                            {
                                Undo.RecordObject(timeline, "Match Animation Length");
                                clip.Duration = animClip.animationClip.length;
                                EditorUtility.SetDirty(timeline);
                            }
                        }
                        else
                        {
                            clip.Duration = animClip.animationClip.length;
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            // 3. 动画起始偏移卡片
            _showStartOffset = EditorGUILayout.Foldout(_showStartOffset, "起始偏移 (AnimStartOffset)", true, EditorStyles.foldoutHeader);
            if (_showStartOffset)
            {
                EditorGUILayout.BeginVertical("box");

                float clipLength = animClip.animationClip != null ? animClip.animationClip.length : 0f;

                // 3-a. 模式选择
                animClip.animStartOffsetMode = (AnimationClip.EAnimStartOffsetMode)EditorGUILayout.EnumPopup(
                    new GUIContent("偏移模式", "Disabled = 从第 0 帧开始\nSeconds = 绝对秒数偏移\nNormalized = 归一化百分比 [0,1]"),
                    animClip.animStartOffsetMode);

                if (animClip.animStartOffsetMode != AnimationClip.EAnimStartOffsetMode.Disabled)
                {
                    if (animClip.animationClip == null)
                    {
                        EditorGUILayout.HelpBox("请先指定动画资源，才能配置起始偏移。", MessageType.Warning);
                    }
                    else
                    {
                        // 3-b. 具体偏移值
                        if (animClip.animStartOffsetMode == AnimationClip.EAnimStartOffsetMode.Seconds)
                        {
                            // Seconds 模式：FloatField，失焦后钳位到 [0, clip.length]
                            EditorGUILayout.BeginHorizontal();
                            float newSec = EditorGUILayout.FloatField(
                                new GUIContent("偏移秒数", $"范围 [0, {clipLength:F3}s]"),
                                animClip.animStartOffsetSeconds);
                            // 实时钳位，并在输入框旁显示上限提示
                            animClip.animStartOffsetSeconds = Mathf.Clamp(newSec, 0f, clipLength);
                            var oldColor = GUI.contentColor;
                            GUI.contentColor = new Color(0.55f, 0.55f, 0.55f, 1f);
                            GUILayout.Label($"/ {clipLength:F2}s", GUILayout.Width(60));
                            GUI.contentColor = oldColor;
                            EditorGUILayout.EndHorizontal();

                            // 进度条可视化
                            if (clipLength > 0f)
                            {
                                Rect barRect = EditorGUILayout.GetControlRect(false, 4);
                                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f, 0.6f));
                                float ratio = animClip.animStartOffsetSeconds / clipLength;
                                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height),
                                    new Color(0.35f, 0.75f, 1f, 0.9f));
                            }
                        }
                        else // Normalized
                        {
                            // Normalized 模式：0~1 Slider + 换算秒数预览
                            EditorGUILayout.BeginHorizontal();
                            animClip.animStartOffsetNormalized = EditorGUILayout.Slider(
                                new GUIContent("偏移百分比", "0 = 动画开头，1 = 动画末帧"),
                                animClip.animStartOffsetNormalized, 0f, 1f);
                            var oldColor = GUI.contentColor;
                            GUI.contentColor = new Color(0.55f, 0.55f, 0.55f, 1f);
                            float previewSec = animClip.animStartOffsetNormalized * clipLength;
                            GUILayout.Label($"≈ {previewSec:F2}s", GUILayout.Width(60));
                            GUI.contentColor = oldColor;
                            EditorGUILayout.EndHorizontal();
                        }

                        // 3-c. 最终偏移预览
                        float resolved = animClip.GetResolvedAnimStartOffsetSeconds();
                        EditorGUILayout.LabelField("实际偏移量",
                            $"{resolved:F3} s  ({(clipLength > 0f ? resolved / clipLength * 100f : 0f):F1}%)",
                            EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Animation Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Animation Clip");
            }
        }
    }
}
