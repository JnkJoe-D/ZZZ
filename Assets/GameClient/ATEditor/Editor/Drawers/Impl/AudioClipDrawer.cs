using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AudioClip))]
    public class AudioClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showPool = true;
        private static bool _showAudioSettings = true;

        public override void DrawInspector(ClipBase clip)
        {
            var audioClip = clip as AudioClip;
            if (audioClip == null) return;

            if (audioClip.audioClips == null)
            {
                audioClip.audioClips = new System.Collections.Generic.List<UnityEngine.AudioClip>();
            }

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 音频资源池卡片
            _showPool = EditorGUILayout.Foldout(_showPool, $"音频资源池 ({audioClip.audioClips.Count})", true, EditorStyles.foldoutHeader);
            if (_showPool)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("音频剪辑列表 (播放时随机选用)", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("+ 添加", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    audioClip.audioClips.Add(null);
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                int removeIndex = -1;
                for (int i = 0; i < audioClip.audioClips.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    audioClip.audioClips[i] = (UnityEngine.AudioClip)EditorGUILayout.ObjectField($"剪辑 [{i}]", audioClip.audioClips[i], typeof(UnityEngine.AudioClip), false);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        removeIndex = i;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (removeIndex >= 0)
                {
                    audioClip.audioClips.RemoveAt(removeIndex);
                }

                if (audioClip.audioClips.Count == 0)
                {
                    EditorGUILayout.HelpBox("请至少添加一个 AudioClip 音频资源", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            // 3. 播放与空间设置卡片
            _showAudioSettings = EditorGUILayout.Foldout(_showAudioSettings, "播放与空间设置", true, EditorStyles.foldoutHeader);
            if (_showAudioSettings)
            {
                EditorGUILayout.BeginVertical("box");
                audioClip.volume = EditorGUILayout.Slider("音量", audioClip.volume, 0f, 1f);
                audioClip.pitch = EditorGUILayout.Slider("音调", audioClip.pitch, 0.1f, 3f);
                audioClip.spatialBlend = EditorGUILayout.Slider("空间混合", audioClip.spatialBlend, 0f, 1f);
                audioClip.loop = EditorGUILayout.Toggle("循环播放", audioClip.loop);
                audioClip.isAffectSpeed = EditorGUILayout.Toggle("同步角色速度", audioClip.isAffectSpeed);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Audio Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Audio Clip");
            }
        }
    }
}
