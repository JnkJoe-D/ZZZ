using UnityEditor;
using UnityEngine;
using Game.Logic;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AttackWarningClip))]
    public class AttackWarningClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showSettings = true;

        public override void DrawInspector(ClipBase clip)
        {
            var warningClip = clip as AttackWarningClip;
            if (warningClip == null) return;

            EditorGUI.BeginChangeCheck();

            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            _showSettings = EditorGUILayout.Foldout(_showSettings, "预警设置", true, EditorStyles.foldoutHeader);
            if (_showSettings)
            {
                EditorGUILayout.BeginVertical("box");
                warningClip.SignalType = (WarningSignalType)EditorGUILayout.EnumPopup("预警类型 (Signal Type)", warningClip.SignalType);
                warningClip.Weight = (AttackWeight)EditorGUILayout.EnumPopup("攻击权重 (Weight)", warningClip.Weight);
                warningClip.DetectionRadius = EditorGUILayout.Slider("检测半径 (Radius)", warningClip.DetectionRadius, 1f, 20f);
                warningClip.DetectionAngle = EditorGUILayout.Slider("检测角度 (Angle)", warningClip.DetectionAngle, 10f, 360f);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Attack Warning Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Attack Warning Clip");
            }
        }
    }
}
