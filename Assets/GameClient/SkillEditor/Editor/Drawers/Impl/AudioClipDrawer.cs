using UnityEditor;
using SkillEditor;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(SkillAudioClip))]
    public class AudioClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            var audioClip = clip as SkillAudioClip;
            if (audioClip == null) return;

            EditorGUILayout.LabelField("音频片段设置", EditorStyles.boldLabel);

            if (audioClip.audioClips == null) 
            {
                audioClip.audioClips = new System.Collections.Generic.List<UnityEngine.AudioClip>();
            }

            int originalCount = audioClip.audioClips.Count;
            // 绘制音频列表 (因为 SkillInspectorBase 尚不支持通用 List 反射)
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("音频资源池 (随机选用)", EditorStyles.boldLabel);
            if (UnityEngine.GUILayout.Button("+", UnityEngine.GUILayout.Width(20)))
            {
                if (UndoContext != null && UndoContext.Length > 0) Undo.RecordObjects(UndoContext, "Add AudioClip");
                audioClip.audioClips.Add(null);
                UnityEngine.GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int i = 0; i < audioClip.audioClips.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                audioClip.audioClips[i] = (UnityEngine.AudioClip)EditorGUILayout.ObjectField(audioClip.audioClips[i], typeof(UnityEngine.AudioClip), false);
                if (UnityEngine.GUILayout.Button("X", UnityEngine.GUILayout.Width(20)))
                {
                    removeIndex = i;
                    UnityEngine.GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (removeIndex >= 0)
            {
                if (UndoContext != null && UndoContext.Length > 0) Undo.RecordObjects(UndoContext, "Remove AudioClip");
                audioClip.audioClips.RemoveAt(removeIndex);
            }
            
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() || originalCount != audioClip.audioClips.Count)
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    foreach (var ctx in UndoContext)
                    {
                        EditorUtility.SetDirty(ctx);
                    }
                }
                // 通知基类标记改动
                var eventField = typeof(SkillInspectorBase).GetField("OnInspectorChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (eventField != null)
                {
                    var eventDelegate = (System.MulticastDelegate)eventField.GetValue(this);
                    if (eventDelegate != null)
                    {
                        foreach (var handler in eventDelegate.GetInvocationList())
                        {
                            handler.Method.Invoke(handler.Target, null);
                        }
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                // 通知基类标记改动 (这里不通过基类自动触发，只是用 GUI 表示有修改即可，Timeline 会在自身逻辑里序列化或通过其他属性触发Dirty)
                // 暂时留空。如果有强需求保存，在 TimelineWindow 统一按 Ctrl+S 即可
            }

            // 使用基类的反射绘制剩余的通用属性（它会跳过不支持的 List）
            base.DrawInspector(clip);
        }
    }
}
