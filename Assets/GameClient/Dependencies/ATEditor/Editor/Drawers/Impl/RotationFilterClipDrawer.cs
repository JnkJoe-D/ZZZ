using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(RotationFilterClip))]
    public class RotationFilterClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showAxis = true;
        private static bool _showConstraint = true;
        private static bool _showExitAlign = true;

        public override void DrawInspector(ClipBase clip)
        {
            var rotClip = clip as RotationFilterClip;
            if (rotClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 轴向过滤设置
            _showAxis = EditorGUILayout.Foldout(_showAxis, "旋转轴向过滤开关 (Axis Mask)", true, EditorStyles.foldoutHeader);
            if (_showAxis)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("勾选代表【过滤/阻断】该轴向旋转。通常推荐过滤 X (Pitch) 与 Z (Roll)，防止角色在根旋转时意外倾斜。", MessageType.None);
                
                EditorGUILayout.BeginHorizontal();
                rotClip.axisMask.FilterX = EditorGUILayout.ToggleLeft("过滤 X (Pitch 俯仰)", rotClip.axisMask.FilterX, GUILayout.Width(150));
                rotClip.axisMask.FilterY = EditorGUILayout.ToggleLeft("过滤 Y (Yaw 偏航)", rotClip.axisMask.FilterY, GUILayout.Width(150));
                rotClip.axisMask.FilterZ = EditorGUILayout.ToggleLeft("过滤 Z (Roll 翻滚)", rotClip.axisMask.FilterZ, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();

                if (rotClip.axisMask.FilterY)
                {
                    EditorGUILayout.HelpBox("当前已过滤 Y 轴：角色在窗口内将完全禁止水平转向（Root 偏航角锁死）。", MessageType.Warning);
                }
                EditorGUILayout.EndVertical();
            }

            bool isRotationSuppressed = rotClip.axisMask.IsAllAxesFiltered || rotClip.axisMask.FilterY;
            if (isRotationSuppressed)
            {
                EditorGUILayout.HelpBox("当前旋转过滤全开（或 Y 轴已被过滤）：角度累积约束与退出吸附对齐策略已自动禁用，避免在动作退出时产生异常对齐跳变。", MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(isRotationSuppressed);

            // 3. 角度累积与截断约束
            _showConstraint = EditorGUILayout.Foldout(_showConstraint, "角度累积与截断约束 (Yaw Constraint)", true, EditorStyles.foldoutHeader);
            if (_showConstraint)
            {
                EditorGUILayout.BeginVertical("box");
                rotClip.enableMaxAngleLimit = EditorGUILayout.Toggle("启用最大累计角度限制", rotClip.enableMaxAngleLimit);
                if (rotClip.enableMaxAngleLimit)
                {
                    rotClip.maxAccumulatedAngle = EditorGUILayout.Slider("最大累计旋转角度 (度)", rotClip.maxAccumulatedAngle, 0f, 360f);
                    EditorGUILayout.HelpBox($"从窗口进入时刻算起，根旋转累计达到 {rotClip.maxAccumulatedAngle:F0}° 时自动截断，丢弃后续动画旋转（杜绝 TurnBack 后半段晃动）。", MessageType.Info);
                }

                rotClip.lockToSingleDirection = EditorGUILayout.Toggle("单向单调锁定 (防回弹晃动)", rotClip.lockToSingleDirection);
                if (rotClip.lockToSingleDirection)
                {
                    EditorGUILayout.HelpBox("只允许沿主掉头方向旋转，严格阻断反向回弹扰动，消除后半段跑动时的身体左右微颤。", MessageType.None);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // 4. 退出时刻对齐策略
            _showExitAlign = EditorGUILayout.Foldout(_showExitAlign, "退出对齐策略 (Exit Alignment)", true, EditorStyles.foldoutHeader);
            if (_showExitAlign)
            {
                EditorGUILayout.BeginVertical("box");
                rotClip.exitAlignMode = (RotationExitAlignMode)EditorGUILayout.EnumPopup("对齐模式", rotClip.exitAlignMode);

                switch (rotClip.exitAlignMode)
                {
                    case RotationExitAlignMode.None:
                        EditorGUILayout.HelpBox("退出时不进行额外朝向修正，保持当前自然旋转状态。", MessageType.None);
                        break;
                    case RotationExitAlignMode.SnapToRelativeTarget:
                        rotClip.targetRelativeYaw = EditorGUILayout.FloatField("相对进入朝向目标角 (度)", rotClip.targetRelativeYaw);
                        rotClip.alignBlendDuration = EditorGUILayout.Slider("对齐平滑时长 (秒)", rotClip.alignBlendDuration, 0f, 0.2f);
                        EditorGUILayout.HelpBox($"退出窗口时刻，强制或平滑对齐到相对于进入朝向偏移 {rotClip.targetRelativeYaw:F0}° 的精确方向（180度掉头专用）。", MessageType.Info);
                        break;
                    case RotationExitAlignMode.SnapToInputDirection:
                        rotClip.alignBlendDuration = EditorGUILayout.Slider("对齐平滑时长 (秒)", rotClip.alignBlendDuration, 0f, 0.2f);
                        EditorGUILayout.HelpBox("退出窗口时刻，若玩家当前有移动摇杆/按键输入，朝向精准吸附至当前输入方向，衔接直线奔跑。", MessageType.Info);
                        break;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Rotation Filter Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Rotation Filter Clip");
            }
        }
    }
}
