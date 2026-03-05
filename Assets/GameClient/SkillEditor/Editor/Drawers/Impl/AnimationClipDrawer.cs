using UnityEditor;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(SkillAnimationClip))]
    public class AnimationClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            var animClip = clip as SkillAnimationClip;
            if (animClip == null) return;

            EditorGUILayout.LabelField("动画片段", EditorStyles.boldLabel);
            base.DrawInspector(clip);

            // 绘制匹配长度按钮
            if (animClip.animationClip != null)
            {
                GUILayout.Space(10);
                
                // 使用内置图标 d_Refresh
                var content = EditorGUIUtility.IconContent("d_Refresh");
                content.text = " 匹配动画时长";

                if (GUILayout.Button(content, GUILayout.Height(30)))
                {
                    var window = EditorWindow.GetWindow<SkillEditorWindow>();
                    if (window != null)
                    {
                        var timeline = window.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            Undo.RecordObject(timeline, "Match Animation Length");
                            clip.duration = animClip.animationClip.length;
                            EditorUtility.SetDirty(timeline);
                        }
                    }
                }

                // --- 新增：匹配偏移按钮 ---
                GUIContent matchContent = new GUIContent(EditorGUIUtility.IconContent("d_TransformTool").image, "使当前片段第一帧的根位置叠加前一个片段的运行轨迹，实现完美接续，无缝配合 Crossfade。");
                matchContent.text = " 匹配对齐至上一片段";

                if (GUILayout.Button(matchContent, GUILayout.Height(30)))
                {
                    if (EditorApplication.isPlaying)
                    {
                        Debug.LogWarning("无法在运行模式下执行匹配对齐，这会干扰当前的动画播放状态。");
                        return;
                    }
                    
                    var window = EditorWindow.GetWindow<SkillEditorWindow>();
                    if (window != null && window.IsInPlayMode)
                    {
                        Debug.LogWarning("无法在预览播放状态下执行匹配对齐，请先暂停播放。");
                        return;
                    }

                    if (window != null && window.prevoewTarget != null)
                    {
                        var timeline = window.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            var prevClip = AnimationUtility.FindPreviousClipOnSameTrack(timeline, animClip);
                            if (prevClip != null)
                            {
                                Undo.RecordObject(timeline, "Match Offsets");
                                AnimationUtility.MatchOffsetsToPreviousClip(timeline, animClip, prevClip, window.prevoewTarget);
                                window.Repaint();
                            }
                            else
                            {
                                Debug.LogWarning("未找到同一个动画轨道上的前一个片段，无法执行匹配。");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("匹配偏移需要场景中有生效的 Preview Avatar。");
                    }
                }

                // --- 新增：清除偏移按钮 ---
                if (animClip.useMatchOffset || animClip.positionOffset != Vector3.zero || animClip.rotationOffset != Vector3.zero)
                {
                    if (GUILayout.Button("清除位移对齐(Clear Offsets)"))
                    {
                        var window = EditorWindow.GetWindow<SkillEditorWindow>();
                        var timeline = window?.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            Undo.RecordObject(timeline, "Clear Offsets");
                            animClip.positionOffset = Vector3.zero;
                            animClip.rotationOffset = Vector3.zero;
                            animClip.useMatchOffset = false;
                            EditorUtility.SetDirty(timeline);
                            window?.Repaint();
                        }
                    }
                }
            }
        }
    }
}
