using UnityEditor;
using UnityEngine;

namespace ATEditor
{
    [CustomEditor(typeof(ActionDirector))]
    public sealed class ActionDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("默认能力：动画、挂点、粒子、音频、移动与根运动过滤。动画需要 Animator 与主轨道；默认后端独占 Animator 的图及根运动。已有项目移动驱动时请覆盖服务并避免双重消费。", MessageType.Info);
            var director = (ActionDirector)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("播放")) director.Play();
                    if (GUILayout.Button(director.IsPaused ? "继续" : "暂停"))
                    {
                        if (director.IsPaused) director.Resume();
                        else director.Pause();
                    }
                    if (GUILayout.Button("停止")) director.Stop();
                }
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("播放时间", director.CurrentTime.ToString("F3"));
                Repaint();
            }
        }
    }
}
