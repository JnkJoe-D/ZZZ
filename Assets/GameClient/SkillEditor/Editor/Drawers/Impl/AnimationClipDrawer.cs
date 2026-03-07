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
                            clip.Duration = animClip.animationClip.length;
                            EditorUtility.SetDirty(timeline);
                        }
                    }
                }

            }
        }
    }
}
