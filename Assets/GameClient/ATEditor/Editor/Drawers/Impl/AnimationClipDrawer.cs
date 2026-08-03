using UnityEditor;
using UnityEngine;
using ATEditor;
using Game.MAnimSystem;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AnimationClip))]
    public class AnimationClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showAnimSettings = true;

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
