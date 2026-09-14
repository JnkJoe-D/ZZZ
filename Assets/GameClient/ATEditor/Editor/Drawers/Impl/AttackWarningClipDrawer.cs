using UnityEditor;
using UnityEngine;
using Game.Logic;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(AttackWarningClip))]
    public class AttackWarningClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showWarningSettings = true;
        private static bool _showCoverageSettings = true;
        private static bool _showClashSettings = true;

        public override void DrawInspector(ClipBase clip)
        {
            var warningClip = clip as AttackWarningClip;
            if (warningClip == null) return;

            EditorGUI.BeginChangeCheck();

            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            if (warningClip.Duration < 0.7f)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 当前预警时长偏短 ({warningClip.Duration:F2}s < 0.7s)。人类平均反应与按键传递时间通常需 0.25s~0.4s，过短的时长容易压缩切人招架操作窗口导致漏招，建议预警时长配置在 0.7s ~ 1.0s 之间。",
                    MessageType.Warning);
            }

            _showWarningSettings = EditorGUILayout.Foldout(_showWarningSettings, "预警类型与感应范围", true, EditorStyles.foldoutHeader);
            if (_showWarningSettings)
            {
                EditorGUILayout.BeginVertical("box");
                warningClip.SignalType = (WarningSignalType)EditorGUILayout.EnumPopup("预警类型 (Signal Type)", warningClip.SignalType);
                if (warningClip.SignalType == WarningSignalType.Yellow_Parryable)
                {
                    warningClip.ParryWeight = (ParryWeight)EditorGUILayout.EnumPopup("招架强度 (Parry Weight)", warningClip.ParryWeight);
                }
                warningClip.DetectionRadius = EditorGUILayout.Slider("预警感应距离 (Detection Radius)", warningClip.DetectionRadius > 0 ? warningClip.DetectionRadius : 10f, 2f, 30f);
                warningClip.DetectionAngle = EditorGUILayout.Slider("预警感应角度 (Detection Angle)", warningClip.DetectionAngle > 0 ? warningClip.DetectionAngle : 180f, 10f, 360f);
                warningClip.ShowGizmos = EditorGUILayout.Toggle("视口显示辅助线框 (Show Gizmos)", warningClip.ShowGizmos);
                EditorGUILayout.EndVertical();
            }

            _showCoverageSettings = EditorGUILayout.Foldout(_showCoverageSettings, "威胁覆盖域配置 (Coverage Area)", true, EditorStyles.foldoutHeader);
            if (_showCoverageSettings)
            {
                EditorGUILayout.BeginVertical("box");
                if (warningClip.CoverageShape == null)
                {
                    warningClip.CoverageShape = new HitBoxShape { shapeType = HitBoxType.Sector, radius = 5f, angle = 120f, height = 2.5f };
                }

                warningClip.CoverageShape.shapeType = (HitBoxType)EditorGUILayout.EnumPopup("覆盖区域形状", warningClip.CoverageShape.shapeType);

                switch (warningClip.CoverageShape.shapeType)
                {
                    case HitBoxType.Sector:
                        warningClip.CoverageShape.radius = EditorGUILayout.Slider("检测半径 (Radius)", warningClip.CoverageShape.radius, 0.5f, 25f);
                        warningClip.CoverageShape.angle = EditorGUILayout.Slider("扇形夹角 (Angle)", warningClip.CoverageShape.angle, 10f, 360f);
                        warningClip.CoverageShape.height = EditorGUILayout.Slider("垂直高度 (Height)", warningClip.CoverageShape.height, 0.5f, 10f);
                        break;

                    case HitBoxType.Box:
                        warningClip.CoverageShape.size = EditorGUILayout.Vector3Field("检测盒尺寸 (Size)", warningClip.CoverageShape.size);
                        break;

                    case HitBoxType.Sphere:
                        warningClip.CoverageShape.radius = EditorGUILayout.Slider("球体半径 (Radius)", warningClip.CoverageShape.radius, 0.5f, 25f);
                        break;

                    case HitBoxType.Capsule:
                        warningClip.CoverageShape.radius = EditorGUILayout.Slider("胶囊体半径 (Radius)", warningClip.CoverageShape.radius, 0.2f, 10f);
                        warningClip.CoverageShape.height = EditorGUILayout.Slider("胶囊体高度 (Height)", warningClip.CoverageShape.height, 0.5f, 15f);
                        break;

                    case HitBoxType.Ring:
                        warningClip.CoverageShape.innerRadius = EditorGUILayout.Slider("内圈半径 (Inner Radius)", warningClip.CoverageShape.innerRadius, 0.1f, 20f);
                        warningClip.CoverageShape.radius = EditorGUILayout.Slider("外圈半径 (Outer Radius)", warningClip.CoverageShape.radius, warningClip.CoverageShape.innerRadius + 0.1f, 25f);
                        warningClip.CoverageShape.height = EditorGUILayout.Slider("环体高度 (Height)", warningClip.CoverageShape.height, 0.5f, 10f);
                        break;
                }

                warningClip.CoverageCenterOffset = EditorGUILayout.Vector3Field("覆盖中心偏移 (Center Offset)", warningClip.CoverageCenterOffset);
                EditorGUILayout.EndVertical();
            }

            _showClashSettings = EditorGUILayout.Foldout(_showClashSettings, "招架接刀身位配置 (Clash Position)", true, EditorStyles.foldoutHeader);
            if (_showClashSettings)
            {
                EditorGUILayout.BeginVertical("box");
                warningClip.ClashPositionOffset = EditorGUILayout.Vector3Field("接刀身位偏移 (Clash Offset)", warningClip.ClashPositionOffset);
                warningClip.AllowInPlaceParry = EditorGUILayout.Toggle("允许就地格挡 (In-Place Parry)", warningClip.AllowInPlaceParry);
                EditorGUILayout.HelpBox("若开启就地格挡，当玩家在覆盖域内切入时，不发生位移，仅瞬间转向面向怪物；若在外部，则精准瞬移至接刀身位偏移处。", MessageType.Info);

                if (GUILayout.Button("快速重置为正前方身位 (1.8m)"))
                {
                    warningClip.ClashPositionOffset = new Vector3(0f, 0f, 1.8f);
                }
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

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var warningClip = clip as AttackWarningClip;
            if (warningClip == null || !warningClip.ShowGizmos) return;

            Transform rootTrans = null;
            if (state != null && state.PreviewContext != null && state.PreviewContext.Owner != null)
            {
                rootTrans = state.PreviewContext.Owner.transform;
            }
            else if (state != null && state.previewTarget != null)
            {
                rootTrans = state.previewTarget.transform;
            }

            if (rootTrans == null) return;

            bool isActive = !state.isStopped && state.timeIndicator >= clip.StartTime && state.timeIndicator <= clip.StartTime + clip.Duration;

            // 1. 绘制威胁覆盖域 (Coverage Area)
            Vector3 covCenter = rootTrans.TransformPoint(warningClip.CoverageCenterOffset);
            Quaternion covRot = rootTrans.rotation;

            Color wireColor = isActive ? new Color(1f, 0.85f, 0f, 0.9f) : new Color(0.8f, 0.7f, 0.2f, 0.4f);
            Color solidColor = isActive ? new Color(1f, 0.85f, 0f, 0.2f) : new Color(0.8f, 0.7f, 0.2f, 0.05f);

            var shape = warningClip.CoverageShape;
            if (shape != null)
            {
                Handles.matrix = Matrix4x4.TRS(covCenter, covRot, Vector3.one);
                Handles.color = wireColor;

                switch (shape.shapeType)
                {
                    case HitBoxType.Sphere:
                        Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, shape.radius);
                        Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.up, 360f, shape.radius);
                        Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 360f, shape.radius);
                        Handles.color = solidColor;
                        Handles.DrawSolidDisc(Vector3.zero, Vector3.up, shape.radius);
                        break;

                    case HitBoxType.Box:
                        Handles.DrawWireCube(Vector3.zero, shape.size);
                        break;

                    case HitBoxType.Capsule:
                        float extraH = Mathf.Max(0, shape.height - shape.radius * 2);
                        Vector3 upCap = Vector3.up * (extraH * 0.5f);
                        Vector3 downCap = Vector3.down * (extraH * 0.5f);
                        Handles.DrawWireArc(upCap, Vector3.up, Vector3.forward, 360, shape.radius);
                        Handles.DrawWireArc(downCap, Vector3.up, Vector3.forward, 360, shape.radius);
                        Handles.DrawWireArc(upCap, Vector3.right, Vector3.back, 180, shape.radius);
                        Handles.DrawWireArc(downCap, Vector3.right, Vector3.forward, 180, shape.radius);
                        Handles.DrawLine(upCap + Vector3.forward * shape.radius, downCap + Vector3.forward * shape.radius);
                        Handles.DrawLine(upCap + Vector3.back * shape.radius, downCap + Vector3.back * shape.radius);
                        Handles.DrawLine(upCap + Vector3.right * shape.radius, downCap + Vector3.right * shape.radius);
                        Handles.DrawLine(upCap + Vector3.left * shape.radius, downCap + Vector3.left * shape.radius);
                        break;

                    case HitBoxType.Sector:
                        float hS = shape.height * 0.5f;
                        Vector3 forwardDir = Vector3.forward;
                        Vector3 rightBoundary = Quaternion.Euler(0, shape.angle * 0.5f, 0) * forwardDir;
                        Vector3 leftBoundary = Quaternion.Euler(0, -shape.angle * 0.5f, 0) * forwardDir;

                        Vector3 upCenter = Vector3.up * hS;
                        Vector3 downCenter = Vector3.down * hS;

                        Handles.DrawWireArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                        Handles.DrawWireArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                        Handles.DrawLine(upCenter, upCenter + rightBoundary * shape.radius);
                        Handles.DrawLine(upCenter, upCenter + leftBoundary * shape.radius);
                        Handles.DrawLine(downCenter, downCenter + rightBoundary * shape.radius);
                        Handles.DrawLine(downCenter, downCenter + leftBoundary * shape.radius);
                        Handles.DrawLine(upCenter, downCenter);
                        Handles.DrawLine(upCenter + rightBoundary * shape.radius, downCenter + rightBoundary * shape.radius);
                        Handles.DrawLine(upCenter + leftBoundary * shape.radius, downCenter + leftBoundary * shape.radius);

                        Handles.color = solidColor;
                        Handles.DrawSolidArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                        Handles.DrawSolidArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                        break;

                    case HitBoxType.Ring:
                        float hR = shape.height * 0.5f;
                        Vector3 upRing = Vector3.up * hR;
                        Vector3 downRing = Vector3.down * hR;
                        Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                        Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                        Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);
                        Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);
                        Handles.DrawLine(upRing + Vector3.forward * shape.radius, downRing + Vector3.forward * shape.radius);
                        Handles.DrawLine(upRing - Vector3.forward * shape.radius, downRing - Vector3.forward * shape.radius);
                        break;
                }
            }

            Handles.matrix = Matrix4x4.identity;

            // 2. 绘制招架接刀身位 (Clash Position) 与可拖拽交互手柄
            Vector3 clashWorldPos = rootTrans.TransformPoint(warningClip.ClashPositionOffset);
            Handles.color = isActive ? Color.green : new Color(0.2f, 0.8f, 0.2f, 0.6f);
            Handles.DrawWireDisc(clashWorldPos, Vector3.up, 0.35f);
            Handles.DrawWireDisc(clashWorldPos, Vector3.forward, 0.15f);
            Handles.DrawLine(rootTrans.position, clashWorldPos);

            var labelStyle = new GUIStyle
            {
                normal = new GUIStyleState { textColor = isActive ? Color.green : Color.yellow },
                fontStyle = FontStyle.Bold
            };
            Handles.Label(clashWorldPos + Vector3.up * 0.4f, "⚔️ 招架接刀点 (Clash Point)", labelStyle);

            EditorGUI.BeginChangeCheck();
            Vector3 newClashWorldPos = Handles.PositionHandle(clashWorldPos, rootTrans.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 newLocalOffset = rootTrans.InverseTransformPoint(newClashWorldPos);
                warningClip.ClashPositionOffset = newLocalOffset;

                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    var window = EditorWindow.GetWindow<ATEditorWindow>(false, "技能编辑器", false);
                    var timeline = window?.GetCurrentTimeline();
                    if (timeline != null)
                    {
                        Undo.RecordObject(timeline, "Move Clash Position Offset");
                        EditorUtility.SetDirty(timeline);
                    }
                    window?.Repaint();
                }
            }
        }
    }
}
