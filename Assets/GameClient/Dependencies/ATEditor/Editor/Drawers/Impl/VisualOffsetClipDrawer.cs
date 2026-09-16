using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(VisualOffsetClip))]
    public class VisualOffsetClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showOffset = true;

        public override void DrawInspector(ClipBase clip)
        {
            var visualClip = clip as VisualOffsetClip;
            if (visualClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 视觉偏移轴卡片
            _showOffset = EditorGUILayout.Foldout(_showOffset, "视觉偏移设置", true, EditorStyles.foldoutHeader);
            if (_showOffset)
            {
                EditorGUILayout.BeginVertical("box");
                visualClip.visualOffsetMode = (MotionWindowVisualOffsetMode)EditorGUILayout.EnumPopup("视觉偏移锁定轴", visualClip.visualOffsetMode);

                string tip = visualClip.visualOffsetMode switch
                {
                    MotionWindowVisualOffsetMode.None => "不进行偏移锁定",
                    MotionWindowVisualOffsetMode.X => "锁定 X 轴偏移（仅渲染层移动）",
                    MotionWindowVisualOffsetMode.Z => "锁定 Z 轴偏移（仅渲染层移动）",
                    MotionWindowVisualOffsetMode.XZ => "锁定 XZ 平面偏移（将物理碰撞体保留在原地，模型沿动画位移）",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(tip))
                {
                    EditorGUILayout.HelpBox(tip, MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Visual Offset Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Visual Offset Clip");
            }
        }
    }

    [CustomDrawer(typeof(VisualOffsetRecoverClip))]
    public class VisualOffsetRecoverClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showRecover = true;

        public override void DrawInspector(ClipBase clip)
        {
            var recoverClip = clip as VisualOffsetRecoverClip;
            if (recoverClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 视觉回正参数卡片
            _showRecover = EditorGUILayout.Foldout(_showRecover, "视觉回正设置", true, EditorStyles.foldoutHeader);
            if (_showRecover)
            {
                EditorGUILayout.BeginVertical("box");
                recoverClip.recoverySpeed = EditorGUILayout.FloatField("强制回正速度", recoverClip.recoverySpeed);
                EditorGUILayout.HelpBox("当动画位移不足以回正时，提供的最小向心速度，确保模型平滑归位。", MessageType.None);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Visual Offset Recover Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Visual Offset Recover Clip");
            }
        }
    }
}
