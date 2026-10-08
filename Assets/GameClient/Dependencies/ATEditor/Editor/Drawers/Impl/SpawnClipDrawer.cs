using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(SpawnClip))]
    public class SpawnClipDrawer : ClipDrawer
    {
        private static readonly Color spawnColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
        private static readonly Color spawnSolidColor = new Color(0f, 1f, 1f, 0.2f);
        private const float indicatorRadius = 0.2f;

        private static bool _showBase = true;
        private static bool _showResource = true;
        private static bool _showBindConfig = true;
        private static bool _showLifecycle = true;
        private static bool _showMovement = true;
        private static bool _showAttack = true;

        public override void DrawInspector(ClipBase clip)
        {
            var spawnClip = clip as SpawnClip;
            if (spawnClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 资源与标识设置卡片
            _showResource = EditorGUILayout.Foldout(_showResource, "预制体资源与标识", true, EditorStyles.foldoutHeader);
            if (_showResource)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.prefab = (GameObject)EditorGUILayout.ObjectField("生成预制体", spawnClip.prefab, typeof(GameObject), false);
                spawnClip.eventTag = EditorGUILayout.TextField("事件透传标签", spawnClip.eventTag);
                EditorGUILayout.EndVertical();
            }

            // 3. 生成与挂载设置卡片
            _showBindConfig = EditorGUILayout.Foldout(_showBindConfig, "生成与挂载设置", true, EditorStyles.foldoutHeader);
            if (_showBindConfig && spawnClip.bindConfig != null)
            {
                EditorGUILayout.BeginVertical("box");
                DrawSubField(spawnClip.bindConfig, "bindPoint");
                if (spawnClip.bindConfig.bindPoint == BindPoint.CustomBone)
                {
                    spawnClip.bindConfig.customBoneName = EditorGUILayout.TextField("自定义骨骼名", spawnClip.bindConfig.customBoneName);
                }
                DrawSubField(spawnClip.bindConfig, "positionOffset");
                DrawSubField(spawnClip.bindConfig, "rotationOffset");
                DrawSubField(spawnClip.bindConfig, "scale");
                DrawSubField(spawnClip.bindConfig, "followTarget");
                if (spawnClip.bindConfig.followTarget)
                {
                    DrawSubField(spawnClip.bindConfig, "followMode");
                }
                EditorGUILayout.EndVertical();
            }

            // 4. 生命周期设置卡片
            _showLifecycle = EditorGUILayout.Foldout(_showLifecycle, "生命周期设置", true, EditorStyles.foldoutHeader);
            if (_showLifecycle && spawnClip.lifecycleConfig != null)
            {
                EditorGUILayout.BeginVertical("box");
                DrawSubField(spawnClip.lifecycleConfig, "destroyOnEnd");
                DrawSubField(spawnClip.lifecycleConfig, "maxLifeTime");
                DrawSubField(spawnClip.lifecycleConfig, "destroyOnInterrupt");
                DrawSubField(spawnClip.lifecycleConfig, "stopEmissionOnEnd");
                EditorGUILayout.EndVertical();
            }

            // 5. 移动与轨迹设置卡片
            _showMovement = EditorGUILayout.Foldout(_showMovement, "移动与轨迹设置", true, EditorStyles.foldoutHeader);
            if (_showMovement && spawnClip.movementConfig != null)
            {
                EditorGUILayout.BeginVertical("box");
                DrawSubField(spawnClip.movementConfig, "moveMode");
                if (spawnClip.movementConfig.moveMode != ProjectileMoveMode.Static)
                {
                    DrawSubField(spawnClip.movementConfig, "initialSpeed");
                }
                if (spawnClip.movementConfig.moveMode == ProjectileMoveMode.StraightLine)
                {
                    DrawSubField(spawnClip.movementConfig, "acceleration");
                    DrawSubField(spawnClip.movementConfig, "maxSpeed");
                }
                else if (spawnClip.movementConfig.moveMode == ProjectileMoveMode.TargetTracking)
                {
                    DrawSubField(spawnClip.movementConfig, "turnSpeed");
                }
                else if (spawnClip.movementConfig.moveMode == ProjectileMoveMode.Parabola)
                {
                    DrawSubField(spawnClip.movementConfig, "gravityScale");
                }
                DrawSubField(spawnClip.movementConfig, "orientToVelocity");
                DrawSubField(spawnClip.movementConfig, "maxDistance");
                EditorGUILayout.EndVertical();
            }

            // 6. 攻击检测设置卡片 (可选)
            _showAttack = EditorGUILayout.Foldout(_showAttack, "攻击检测设置 (可选)", true, EditorStyles.foldoutHeader);
            if (_showAttack)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.enableAttackDetection = EditorGUILayout.Toggle("启用攻击检测", spawnClip.enableAttackDetection);
                if (spawnClip.enableAttackDetection)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("── 检测盒范围 ──", EditorStyles.boldLabel);
                    if (spawnClip.hitBoxScope != null)
                    {
                        DrawSubField(spawnClip.hitBoxScope, "shape");
                        DrawSubField(spawnClip.hitBoxScope, "positionOffset");
                        DrawSubField(spawnClip.hitBoxScope, "rotationOffset");
                        DrawSubField(spawnClip.hitBoxScope, "showHitBoxGizmos");
                    }

                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("── 判定策略与效果 ──", EditorStyles.boldLabel);
                    if (spawnClip.attackPolicy != null)
                    {
                        DrawSubField(spawnClip.attackPolicy, "detectFrequency");
                        if (spawnClip.attackPolicy.detectFrequency == Frequency.Times)
                        {
                            DrawSubField(spawnClip.attackPolicy, "times");
                        }
                        DrawSubField(spawnClip.attackPolicy, "maxHitTargets");
                        if (spawnClip.attackPolicy.maxHitTargets > 0)
                        {
                            DrawSubField(spawnClip.attackPolicy, "targetSortMode");
                        }
                        DrawSubField(spawnClip.attackPolicy, "hitDirectionMode");
                        if (spawnClip.attackPolicy.hitDirectionMode == HitDirectionMode.OnEnterCustomRelative)
                        {
                            DrawSubField(spawnClip.attackPolicy, "customHitDirection");
                        }
                        DrawSubField(spawnClip.attackPolicy, "hitLayerMask");
                        DrawSubField(spawnClip.attackPolicy, "isSelfImpacted");
                        DrawSubField(spawnClip.attackPolicy, "detects");
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Spawn Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Spawn Clip");
            }
        }

        private void DrawSubField(object target, string fieldName)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                DrawField(field, target);
            }
        }

        public override void DrawSceneGUI(ClipBase obj, ATEditorState state)
        {
            var clip = obj as SpawnClip;
            if (clip == null) return;

            GetMatrix(clip, state, out Vector3 pos, out Quaternion rot);

            Handles.color = spawnColor;

            // 1. 绘制生成原点
            Handles.SphereHandleCap(0, pos, Quaternion.identity, indicatorRadius, EventType.Repaint);
            
            Handles.color = spawnSolidColor;
            Handles.DrawSolidDisc(pos, rot * Vector3.up, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.right, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.forward, indicatorRadius);

            // 2. 绘制朝向指示箭头与运动速度方向
            Handles.color = spawnColor;
            float arrowLength = 1.5f;
            Vector3 forwardDir = rot * Vector3.forward;
            Vector3 arrowEnd = pos + forwardDir * arrowLength;
            Handles.DrawLine(pos, arrowEnd);

            float arrowHeadSize = 0.3f;
            Vector3 rightDir = rot * Vector3.right;
            Vector3 upDir = rot * Vector3.up;
            Vector3 arrowBase = arrowEnd - forwardDir * arrowHeadSize;
            Handles.DrawLine(arrowEnd, arrowBase + rightDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase - rightDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase + upDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase - upDir * (arrowHeadSize * 0.5f));

            // 3. 若有速度且为直线位移，绘制射程参考线
            if (clip.movementConfig != null && clip.movementConfig.moveMode == ProjectileMoveMode.StraightLine)
            {
                float range = clip.movementConfig.maxDistance > 0 ? Mathf.Min(clip.movementConfig.maxDistance, 20f) : 10f;
                Handles.color = new Color(0f, 1f, 1f, 0.4f);
                Handles.DrawDottedLine(pos, pos + forwardDir * range, 3f);
            }

            // 4. 若开启攻击检测且开启 Gizmo，绘制攻击盒
            if (clip.enableAttackDetection && clip.hitBoxScope != null && clip.hitBoxScope.showHitBoxGizmos)
            {
                Vector3 boxPos = pos + rot * clip.hitBoxScope.positionOffset;
                Quaternion boxRot = rot * Quaternion.Euler(clip.hitBoxScope.rotationOffset);
                Handles.color = new Color(1f, 0.2f, 0.2f, 0.6f);
                var shape = clip.hitBoxScope.shape;
                if (shape != null)
                {
                    if (shape.shapeType == HitBoxType.Sphere)
                    {
                        Handles.DrawWireDisc(boxPos, Vector3.up, shape.radius);
                        Handles.DrawWireDisc(boxPos, Vector3.right, shape.radius);
                        Handles.DrawWireDisc(boxPos, Vector3.forward, shape.radius);
                    }
                    else if (shape.shapeType == HitBoxType.Box)
                    {
                        Matrix4x4 oldMat = Handles.matrix;
                        Handles.matrix = Matrix4x4.TRS(boxPos, boxRot, Vector3.one);
                        Handles.DrawWireCube(Vector3.zero, shape.size);
                        Handles.matrix = oldMat;
                    }
                }
            }
        }

        private void GetMatrix(SpawnClip clip, ATEditorState state, out Vector3 pos, out Quaternion rot)
        {
            Transform parent = null;
            var bindConfig = clip.bindConfig ?? new TransformBindConfig();

            if (state != null && state.PreviewContext != null)
            {
                var actor = state.PreviewContext.GetService<IBoneGetter>();
                if (actor != null)
                {
                    parent = actor.GetBone(bindConfig.bindPoint, bindConfig.customBoneName);
                }
            }

            if (parent == null && state != null && state.previewTarget != null)
            {
                var getter = new ATBoneGetter(state.previewTarget);
                parent = getter.GetBone(bindConfig.bindPoint, bindConfig.customBoneName);
            }

            if (parent != null)
            {
                pos = parent.position + parent.rotation * bindConfig.positionOffset;
                rot = parent.rotation * Quaternion.Euler(bindConfig.rotationOffset);
            }
            else
            {
                pos = bindConfig.positionOffset;
                rot = Quaternion.Euler(bindConfig.rotationOffset);
            }
        }
    }
}
