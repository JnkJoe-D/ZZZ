using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(RotationClip))]
    public class RotationClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showRotation = true;

        public override void DrawInspector(ClipBase clip)
        {
            var rotClip = clip as RotationClip;
            if (rotClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 转向与朝向控制卡片
            _showRotation = EditorGUILayout.Foldout(_showRotation, "转向与朝向控制", true, EditorStyles.foldoutHeader);
            if (_showRotation)
            {
                EditorGUILayout.BeginVertical("box");
                rotClip.referenceDirection = (RotationReference)EditorGUILayout.EnumPopup("参考方向基准", rotClip.referenceDirection);
                rotClip.rotationMode = (RotationMode)EditorGUILayout.EnumPopup("旋转插值方式", rotClip.rotationMode);
                rotClip.updateFrequency = (UpdateFrequency)EditorGUILayout.EnumPopup("更新执行频率", rotClip.updateFrequency);
                rotClip.localRotationOffset = EditorGUILayout.Vector3Field("本地旋转偏移", rotClip.localRotationOffset);

                string tip = rotClip.referenceDirection switch
                {
                    RotationReference.Input => "朝向摇杆/按键输入的世界坐标方向",
                    RotationReference.InputWithCamera => "结合相机视角的摇杆输入朝向（同常规移动转向）",
                    RotationReference.Target => "严格锁定面向当前锁定的战斗目标（无目标则不转）",
                    RotationReference.TargetThenInput => "优先锁定战斗目标；若无目标则降级为输入方向",
                    RotationReference.TargetThenInputWithCamera => "优先锁定战斗目标；若无目标则降级为相机视角输入方向",
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
                    Undo.RecordObjects(UndoContext, "Modify Rotation Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Rotation Clip");
            }
        }
    }
}
