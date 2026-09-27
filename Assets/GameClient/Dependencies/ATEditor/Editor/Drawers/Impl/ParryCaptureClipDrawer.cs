using System;
using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(ParryCaptureClip))]
    public class ParryCaptureClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showCaptureConfig = true;

        public override void DrawInspector(ClipBase clip)
        {
            var parryClip = clip as ParryCaptureClip;
            if (parryClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片 (片段名称、启用、时间等)
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 招架捕获配置卡片
            _showCaptureConfig = EditorGUILayout.Foldout(_showCaptureConfig, "招架捕获配置", true, EditorStyles.foldoutHeader);
            if (_showCaptureConfig)
            {
                EditorGUILayout.BeginVertical("box");

                if (parryClip.data == null)
                {
                    parryClip.data = new ParryCaptureData();
                }

                parryClip.data.triggerTiming = (ParryCaptureTriggerTiming)EditorGUILayout.EnumPopup("触发时机", parryClip.data.triggerTiming);

                if (parryClip.data.triggerTiming == ParryCaptureTriggerTiming.Instant)
                {
                    EditorGUILayout.HelpBox("【瞬时触发 (Instant)】窗口内一旦捕获拼刀，同帧立即发布招架成功事件，打断当前动作切入反击动作（适用于招架秒反）。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("【退出触发 (OnExit)】窗口内捕获拼刀后暂存命中，等待当前片段播放完毕（OnExit）才发布招架成功事件（适用于格挡前摇/架势动作）。", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Parry Capture Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Parry Capture Clip");
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, ATEditorState state, Color clipColor, string displayName)
        {
            if (clip is ParryCaptureClip parryClip && parryClip.data != null)
            {
                string timingTag = parryClip.data.triggerTiming == ParryCaptureTriggerTiming.Instant ? "瞬时" : "退出";
                displayName = $"{clip.clipName} ({timingTag})";
            }

            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);
        }
    }
}
