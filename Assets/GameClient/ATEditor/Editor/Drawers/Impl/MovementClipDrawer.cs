using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(MovementClip))]
    public class MovementClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showDestination = true;
        private static bool _showValidation = true;
        private static bool _showDisplacement = true;

        private bool candidatesFoldout = true;
        private bool radialFallbackFoldout = true;
        private bool layersFoldout = true;

        public override void DrawInspector(ClipBase clip)
        {
            var moveClip = clip as MovementClip;
            if (moveClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础时序与片段信息
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 目标与参考坐标设置（包含单一目标模式 与 智能多级候选列表模式）
            _showDestination = EditorGUILayout.Foldout(_showDestination, "目标与参考坐标", true, EditorStyles.foldoutHeader);
            if (_showDestination)
            {
                DrawDestinationSection(moveClip);
            }

            // 3. 智能位置校验与环境安全（置于位移控制面板上方）
            if (moveClip.referenceDestination == ReferenceDestination.Target)
            {
                _showValidation = EditorGUILayout.Foldout(_showValidation, "智能位置校验与环境安全", true, EditorStyles.foldoutHeader);
                if (_showValidation)
                {
                    DrawValidationSection(moveClip);
                }
            }

            // 4. 位移控制与碰撞（置于最下方）
            _showDisplacement = EditorGUILayout.Foldout(_showDisplacement, "位移曲线与碰撞控制", true, EditorStyles.foldoutHeader);
            if (_showDisplacement)
            {
                DrawDisplacementSection(moveClip);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Movement Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Movement Clip");
            }
        }

        private void DrawDestinationSection(MovementClip moveClip)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("目标与参考设置", EditorStyles.boldLabel);

            moveClip.referenceDestination = (ReferenceDestination)EditorGUILayout.EnumPopup("参考目标", moveClip.referenceDestination);

            if (moveClip.referenceDestination == ReferenceDestination.Fixed)
            {
                moveClip.referenceCoordinate = (CoordinateSystem)EditorGUILayout.EnumPopup("参考坐标系", moveClip.referenceCoordinate);
                moveClip.targetPosition = EditorGUILayout.Vector3Field("目标位置", moveClip.targetPosition);
            }
            else // Target
            {
                moveClip.targetPositionEnum = (TargetPositionType)EditorGUILayout.EnumPopup("目标位置模式", moveClip.targetPositionEnum);

                if (moveClip.targetPositionEnum == TargetPositionType.CandidateList)
                {
                    // 智能多级候选列表模式：展示快捷预设与候选点管理列表
                    EditorGUILayout.Space(4);
                    DrawCandidateListSection(moveClip);
                }
                else
                {
                    moveClip.targetBaseDirection = (TargetBaseDirection)EditorGUILayout.EnumPopup("基准朝向", moveClip.targetBaseDirection);
                    moveClip.targetAnchor = (DistanceAnchor)EditorGUILayout.EnumPopup("目标参照锚点", moveClip.targetAnchor);
                    moveClip.selfAnchor = (DistanceAnchor)EditorGUILayout.EnumPopup("自身参照锚点", moveClip.selfAnchor);
                    moveClip.offsetRadius = EditorGUILayout.FloatField("额外半径偏移", moveClip.offsetRadius);

                    if (moveClip.targetPositionEnum == TargetPositionType.CustomAngle)
                    {
                        moveClip.angleOffset = EditorGUILayout.Slider("自定义角度(度)", moveClip.angleOffset, -180f, 180f);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCandidateListSection(MovementClip moveClip)
        {
            // 1. 快捷预设按钮组
            DrawQuickPresets(moveClip);

            EditorGUILayout.Space(4);

            // 2. 候选列表卡片
            if (moveClip.candidatePositions == null)
            {
                moveClip.candidatePositions = new MovementPositionCandidate[0];
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            candidatesFoldout = EditorGUILayout.Foldout(candidatesFoldout, $"候选目标位置优先级列表 ({moveClip.candidatePositions.Length} 项)", true, EditorStyles.foldoutHeader);
            
            if (GUILayout.Button("+ 添加候选点", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                var list = new List<MovementPositionCandidate>(moveClip.candidatePositions);
                list.Add(new MovementPositionCandidate($"候选点 #{list.Count + 1}", CandidatePositionType.EnemyBack));
                moveClip.candidatePositions = list.ToArray();
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            if (candidatesFoldout)
            {
                if (moveClip.candidatePositions.Length == 0)
                {
                    EditorGUILayout.HelpBox("候选列表为空，请点击右上角添加候选落点或应用上方预设。", MessageType.Warning);
                }

                for (int i = 0; i < moveClip.candidatePositions.Length; i++)
                {
                    var cand = moveClip.candidatePositions[i];
                    if (cand == null)
                    {
                        cand = new MovementPositionCandidate();
                        moveClip.candidatePositions[i] = cand;
                    }

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();

                    string priorityTag = (i == 0) ? "首选" : $"备选 {i}";
                    string title = string.IsNullOrEmpty(cand.label) ? $"[{priorityTag}] {cand.targetPositionEnum}" : $"[{priorityTag}] {cand.label}";
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                    if (i > 0 && GUILayout.Button("↑", GUILayout.Width(22)))
                    {
                        var temp = moveClip.candidatePositions[i];
                        moveClip.candidatePositions[i] = moveClip.candidatePositions[i - 1];
                        moveClip.candidatePositions[i - 1] = temp;
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    if (i < moveClip.candidatePositions.Length - 1 && GUILayout.Button("↓", GUILayout.Width(22)))
                    {
                        var temp = moveClip.candidatePositions[i];
                        moveClip.candidatePositions[i] = moveClip.candidatePositions[i + 1];
                        moveClip.candidatePositions[i + 1] = temp;
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        var list = new List<MovementPositionCandidate>(moveClip.candidatePositions);
                        list.RemoveAt(i);
                        moveClip.candidatePositions = list.ToArray();
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;
                    cand.label = EditorGUILayout.TextField("备注标签", cand.label);
                    cand.targetPositionEnum = (CandidatePositionType)EditorGUILayout.EnumPopup("目标位置模式", cand.targetPositionEnum);
                    cand.targetBaseDirection = (TargetBaseDirection)EditorGUILayout.EnumPopup("基准朝向", cand.targetBaseDirection);
                    cand.targetAnchor = (DistanceAnchor)EditorGUILayout.EnumPopup("目标参照锚点", cand.targetAnchor);
                    cand.selfAnchor = (DistanceAnchor)EditorGUILayout.EnumPopup("自身参照锚点", cand.selfAnchor);
                    cand.offsetRadius = EditorGUILayout.FloatField("额外半径偏移", cand.offsetRadius);

                    if (cand.targetPositionEnum == CandidatePositionType.CustomAngle)
                    {
                        cand.angleOffset = EditorGUILayout.Slider("自定义角度(度)", cand.angleOffset, -180f, 180f);
                    }

                    cand.heightOffset = EditorGUILayout.FloatField("高度微调", cand.heightOffset);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickPresets(MovementClip moveClip)
        {
            EditorGUILayout.LabelField("一键快捷预设策略", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("背后突击 (后→侧→前)", EditorStyles.miniButtonLeft, GUILayout.Height(22)))
            {
                MarkTimelineDirty("Apply Preset: Back-Flank-Front");
                moveClip.candidatePositions = new MovementPositionCandidate[]
                {
                    new MovementPositionCandidate("首选身后", CandidatePositionType.EnemyBack, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选左侧", CandidatePositionType.EnemyLeft, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选右侧", CandidatePositionType.EnemyRight, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("兜底身前", CandidatePositionType.EnemyFront, TargetBaseDirection.LineOfSight, 0f, 0f),
                };
                moveClip.enableSmartRadialFallback = true;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("正面突进 (前→侧→后)", EditorStyles.miniButtonMid, GUILayout.Height(22)))
            {
                MarkTimelineDirty("Apply Preset: Front-Flank-Back");
                moveClip.candidatePositions = new MovementPositionCandidate[]
                {
                    new MovementPositionCandidate("首选正前", CandidatePositionType.EnemyFront, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选左侧", CandidatePositionType.EnemyLeft, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选右侧", CandidatePositionType.EnemyRight, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选身后", CandidatePositionType.EnemyBack, TargetBaseDirection.LineOfSight, 0f, 0f),
                };
                moveClip.enableSmartRadialFallback = true;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("战术侧翼 (左/右)", EditorStyles.miniButtonRight, GUILayout.Height(22)))
            {
                MarkTimelineDirty("Apply Preset: Flanking");
                moveClip.candidatePositions = new MovementPositionCandidate[]
                {
                    new MovementPositionCandidate("首选左侧", CandidatePositionType.EnemyLeft, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("备选右侧", CandidatePositionType.EnemyRight, TargetBaseDirection.LineOfSight, 0f, 0f),
                    new MovementPositionCandidate("兜底正前", CandidatePositionType.EnemyFront, TargetBaseDirection.LineOfSight, 0f, 0f),
                };
                moveClip.enableSmartRadialFallback = true;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSection(MovementClip moveClip)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 启用位置校验大开关 (支持点击勾选)
            EditorGUILayout.BeginHorizontal();
            GUIStyle toggleHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = moveClip.enablePositionValidation ? new Color(0.3f, 0.95f, 0.45f) : Color.white }
            };

            moveClip.enablePositionValidation = EditorGUILayout.ToggleLeft(" ⚡ 启用智能位置校验与障碍回退", moveClip.enablePositionValidation, toggleHeaderStyle);
            EditorGUILayout.EndHorizontal();

            if (!moveClip.enablePositionValidation)
            {
                EditorGUILayout.HelpBox("未开启位置校验：角色将直接定位至目标点（可能卡入墙体或悬空）。勾选上方开关可开启物理可用性校验与避障保护。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("已开启智能校验：依次检测目标落点的物理可用性（地形障碍物、悬崖、视线阻隔）；若全部受阻且无安全落点，将自动取消移动以保证安全。", MessageType.Info);

                EditorGUILayout.Space(4);

                // 环形径向扩散搜索
                DrawRadialFallbackSettings(moveClip);

                EditorGUILayout.Space(4);

                // 物理碰撞与地面吸附层级
                DrawPhysicsLayerSettings(moveClip);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRadialFallbackSettings(MovementClip moveClip)
        {
            EditorGUILayout.BeginVertical("box");
            radialFallbackFoldout = EditorGUILayout.Foldout(radialFallbackFoldout, "同心圆周扩散搜索", true, EditorStyles.foldoutHeader);
            if (radialFallbackFoldout)
            {
                EditorGUI.indentLevel++;
                moveClip.enableSmartRadialFallback = EditorGUILayout.Toggle("启用圆周扩散搜索", moveClip.enableSmartRadialFallback);
                if (moveClip.enableSmartRadialFallback)
                {
                    moveClip.fallbackAngleStep = EditorGUILayout.Slider("搜索角度步长(度)", moveClip.fallbackAngleStep, 5f, 90f);
                    moveClip.maxFallbackAngle = EditorGUILayout.Slider("最大扇形偏角(度)", moveClip.maxFallbackAngle, 30f, 180f);
                    EditorGUILayout.HelpBox("当首选落点受阻时，在保持与敌人的攻击贴身距离不变的前提下，沿圆周左右两侧扇形测试安全落点。", MessageType.None);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPhysicsLayerSettings(MovementClip moveClip)
        {
            EditorGUILayout.BeginVertical("box");
            layersFoldout = EditorGUILayout.Foldout(layersFoldout, "物理碰撞与环境检测参数", true, EditorStyles.foldoutHeader);
            if (layersFoldout)
            {
                EditorGUI.indentLevel++;

                // Obstacle layers
                int obsMask = EditorGUILayout.MaskField("障碍物阻挡层", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(moveClip.obstacleLayers), InternalEditorUtility.layers);
                moveClip.obstacleLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(obsMask);

                // Ground layers
                int groundMask = EditorGUILayout.MaskField("地面检测层", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(moveClip.groundLayers), InternalEditorUtility.layers);
                moveClip.groundLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(groundMask);

                moveClip.groundCheckDistance = EditorGUILayout.FloatField("地面检测最大距离", moveClip.groundCheckDistance);
                moveClip.requireGrounded = EditorGUILayout.Toggle("必须贴合有效地面", moveClip.requireGrounded);
                moveClip.faceTargetOnArrival = EditorGUILayout.Toggle("到达后自动面向目标", moveClip.faceTargetOnArrival);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDisplacementSection(MovementClip moveClip)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("位移控制与碰撞", EditorStyles.boldLabel);

            moveClip.displacementType = (DisplacementType)EditorGUILayout.EnumPopup("位移方式", moveClip.displacementType);

            // 持续平滑位移时才显示曲线与忽略碰撞层级
            if (moveClip.displacementType == DisplacementType.Continuous)
            {
                moveClip.movementCurve = (MovementCurve)EditorGUILayout.EnumPopup("移动曲线", moveClip.movementCurve);
            }
            EditorGUILayout.EndVertical();
        }

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var moveClip = clip as MovementClip;
            if (moveClip == null || moveClip.referenceDestination != ReferenceDestination.Target) return;

            // 获取预览主控角色与目标对象
            GameObject previewRole = state?.PreviewContext?.Owner ?? state?.previewTarget ?? Selection.activeGameObject;
            if (previewRole == null) return;

            Transform targetTransform = null;
            var transformHandler = state?.PreviewContext?.GetService<ITransformHandler>();
            if (transformHandler != null)
            {
                targetTransform = transformHandler.GetTarget();
            }

            // 若预览上下文中未绑定 Target，尝试从场景中获取可能的目标物体作为辅助可视化参考
            if (targetTransform == null)
            {
                var enemyObj = GameObject.FindWithTag("Enemy") ?? GameObject.Find("Target") ?? GameObject.Find("Enemy");
                if (enemyObj != null && enemyObj != previewRole)
                {
                    targetTransform = enemyObj.transform;
                }
            }

            if (targetTransform == null) return;

            Vector3 targetCenter = targetTransform.position;
            Vector3 ownerPos = previewRole.transform.position;

            // 绘制候选定位点 Handles
            if (moveClip.targetPositionEnum == TargetPositionType.CandidateList && moveClip.candidatePositions != null)
            {
                for (int i = 0; i < moveClip.candidatePositions.Length; i++)
                {
                    var cand = moveClip.candidatePositions[i];
                    if (cand == null) continue;

                    Vector3 dir = MovementPositionSolver.CalculateOffsetDirection(cand.targetPositionEnum, cand.targetBaseDirection, cand.angleOffset, targetTransform, ownerPos, Vector3.zero);
                    float dist = 1.0f + cand.offsetRadius;
                    Vector3 candWorldPos = targetCenter + dir * dist + Vector3.up * cand.heightOffset;

                    Color color = (i == 0) ? new Color(0.2f, 1f, 0.4f, 0.8f) : new Color(0.2f, 0.8f, 1f, 0.6f);
                    Handles.color = color;
                    Handles.DrawWireDisc(candWorldPos, Vector3.up, 0.35f);
                    Handles.DrawDottedLine(targetCenter, candWorldPos, 3f);

                    string labelText = $"[{i + 1}] {(string.IsNullOrEmpty(cand.label) ? cand.targetPositionEnum.ToString() : cand.label)}";
                    Handles.Label(candWorldPos + Vector3.up * 0.4f, labelText, new GUIStyle { normal = { textColor = color }, fontStyle = FontStyle.Bold });
                }
            }
            else if (moveClip.targetPositionEnum != TargetPositionType.CandidateList)
            {
                Vector3 dir = MovementPositionSolver.CalculateOffsetDirection(moveClip.targetPositionEnum, moveClip.targetBaseDirection, moveClip.angleOffset, targetTransform, ownerPos, Vector3.zero);
                float dist = 1.0f + moveClip.offsetRadius;
                Vector3 singleWorldPos = targetCenter + dir * dist;

                Handles.color = new Color(0.2f, 1f, 0.4f, 0.8f);
                Handles.DrawWireDisc(singleWorldPos, Vector3.up, 0.35f);
                Handles.DrawDottedLine(targetCenter, singleWorldPos, 3f);
                Handles.Label(singleWorldPos + Vector3.up * 0.4f, $"[目标] {moveClip.targetPositionEnum}", new GUIStyle { normal = { textColor = new Color(0.2f, 1f, 0.4f, 0.8f) }, fontStyle = FontStyle.Bold });
            }
        }
    }
}
