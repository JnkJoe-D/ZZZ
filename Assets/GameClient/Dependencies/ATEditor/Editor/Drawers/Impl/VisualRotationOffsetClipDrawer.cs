using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(VisualRotationOffsetClip))]
    public class VisualRotationOffsetClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showRootSnap = true;
        private static bool _showOffset = true;
        private static bool _showRecover = true;

        public override void DrawInspector(ClipBase clip)
        {
            var offsetClip = clip as VisualRotationOffsetClip;
            if (offsetClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 根节点对齐物理设置
            _showRootSnap = EditorGUILayout.Foldout(_showRootSnap, "物理根节点对齐 (Parent Root Snap)", true, EditorStyles.foldoutHeader);
            if (_showRootSnap)
            {
                EditorGUILayout.BeginVertical("box");
                offsetClip.snapRootOnEnter = EditorGUILayout.Toggle("进入时对齐输入方向", offsetClip.snapRootOnEnter);
                if (offsetClip.snapRootOnEnter)
                {
                    EditorGUILayout.HelpBox("进入瞬间立即将 GameObject 物理胶囊体对齐当前摇杆输入方向，保证物理权威与后续输入基准绝对正确！", MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // 3. 模型反向补偿设置
            _showOffset = EditorGUILayout.Foldout(_showOffset, "模型视觉反向补偿 (Visual Counter-Rotation)", true, EditorStyles.foldoutHeader);
            if (_showOffset)
            {
                EditorGUILayout.BeginVertical("box");
                offsetClip.initialCounterYaw = EditorGUILayout.FloatField("初始反向偏航角 (度)", offsetClip.initialCounterYaw);
                EditorGUILayout.HelpBox($"进入瞬间给视觉模型子节点注入 {offsetClip.initialCounterYaw:F0}° 偏航补偿。与父节点旋转相互抵消，保证第 0 帧世界朝向纹丝不动，杜绝反向闪现！", MessageType.None);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // 4. 回正与淡出曲线
            _showRecover = EditorGUILayout.Foldout(_showRecover, "平滑回正与淡出 (Auto Recover)", true, EditorStyles.foldoutHeader);
            if (_showRecover)
            {
                EditorGUILayout.BeginVertical("box");
                offsetClip.autoRecoverInWindow = EditorGUILayout.Toggle("窗口期内平滑回正", offsetClip.autoRecoverInWindow);
                if (offsetClip.autoRecoverInWindow)
                {
                    offsetClip.fadeCurve = EditorGUILayout.CurveField("补偿淡出曲线", offsetClip.fadeCurve);
                    EditorGUILayout.HelpBox("随着转身动画进程推进，模型反向补偿角按曲线淡出至 0 度，与动画骨骼完成姿态自然对齐。", MessageType.Info);
                }
                offsetClip.BlendOutDuration = EditorGUILayout.Slider("退出收敛时长 (秒)", offsetClip.BlendOutDuration, 0f, 0.2f);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Visual Rotation Offset Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Visual Rotation Offset Clip");
            }
        }
    }
}
