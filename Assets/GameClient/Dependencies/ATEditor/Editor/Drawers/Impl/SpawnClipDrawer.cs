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

        // 子卡片抽屉实例
        private readonly TransformBindCardDrawer _bindCard = new TransformBindCardDrawer(isMandatory: true);
        private readonly LifecycleCardDrawer _lifecycleCard = new LifecycleCardDrawer(isMandatory: true);
        private readonly ProjectileMovementCardDrawer _movementCard = new ProjectileMovementCardDrawer(isMandatory: true);
        private readonly CombatDetectionCardDrawer _combatCard = new CombatDetectionCardDrawer(isMandatory: false);

        public SpawnClipDrawer()
        {
            _bindCard.CardKey = "SpawnClip_TransformBind";
            _lifecycleCard.CardKey = "SpawnClip_Lifecycle";
            _movementCard.CardKey = "SpawnClip_Movement";
            _combatCard.CardKey = "SpawnClip_CombatDetection";
        }

        public override void DrawInspector(ClipBase clip)
        {
            var spawnClip = clip as SpawnClip;
            if (spawnClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 构建卡片绘制上下文
            CardDrawContext context = new CardDrawContext
            {
                Clip = spawnClip,
                UndoContext = UndoContext,
                State = GetEditorState(),
                PreviewTarget = GetPreviewTarget()
            };

            // 2. 基础信息卡片 (粒子系统风格 Header，必需)
            bool baseEnabled = true;
            CardDrawerBase<object>.DrawParticleStyleHeader("基础信息", isMandatory: true, ref baseEnabled, ref _showBase);
            if (_showBase)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.clipName = EditorGUILayout.TextField("片段名称", spawnClip.clipName);
                spawnClip.isEnabled = EditorGUILayout.Toggle("启用", spawnClip.isEnabled);
                spawnClip.StartTime = Mathf.Max(0f, EditorGUILayout.FloatField("开始时间", spawnClip.StartTime));
                spawnClip.Duration = Mathf.Max(0.01f, EditorGUILayout.FloatField("持续时间", spawnClip.Duration));
                EditorGUILayout.EndVertical();
            }

            // 3. 预制体资源与标识卡片 (粒子系统风格 Header，必需)
            bool resEnabled = true;
            CardDrawerBase<object>.DrawParticleStyleHeader("预制体资源与标识", isMandatory: true, ref resEnabled, ref _showResource);
            if (_showResource)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.prefab = (GameObject)EditorGUILayout.ObjectField("生成预制体", spawnClip.prefab, typeof(GameObject), false);
                spawnClip.eventTag = EditorGUILayout.TextField("事件透传标签", spawnClip.eventTag);
                EditorGUILayout.EndVertical();
            }

            // 4. 生成与挂载设置卡片 (组件化抽屉，必需)
            if (spawnClip.bindConfig != null)
            {
                _bindCard.BindData(spawnClip.bindConfig);
                _bindCard.DrawInspector(context);
            }

            // 5. 生命周期设置卡片 (组件化抽屉，必需)
            if (spawnClip.lifecycleConfig != null)
            {
                _lifecycleCard.BindData(spawnClip.lifecycleConfig);
                _lifecycleCard.DrawInspector(context);
            }

            // 6. 移动与轨迹设置卡片 (组件化抽屉，必需)
            if (spawnClip.movementConfig != null)
            {
                _movementCard.BindData(spawnClip.movementConfig);
                _movementCard.DrawInspector(context);
            }

            // 7. 攻击检测设置卡片 (组件化抽屉，可选卡片：Header 内置复选框控制启用，取消勾选时收起且停用)
            _combatCard.IsEnabled = spawnClip.enableAttackDetection;
            _combatCard.BindData(new CombatDetectionCardData(
                spawnClip.hitBoxScope, 
                spawnClip.attackPolicy, 
                enabled => spawnClip.enableAttackDetection = enabled
            ));
            _combatCard.DrawInspector(context);

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

            // 2. 绘制朝向指示箭头
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

            // 3. 构建卡片场景绘制上下文 (传递生成物当前运动原点)
            CardDrawContext context = new CardDrawContext
            {
                Clip = clip,
                State = state,
                OriginPose = new Pose(pos, rot),
                NeedsApplyCardOffset = true,
                PreviewTarget = state?.previewTarget
            };

            // 4. 委托移动卡片绘制射程虚线
            if (clip.movementConfig != null)
            {
                _movementCard.BindData(clip.movementConfig);
                _movementCard.DrawSceneGUI(context);
            }

            // 5. 委托攻击检测卡片绘制检测盒 (完整支持 Sphere/Box/Capsule/Sector/Ring 全形状)
            if (clip.enableAttackDetection && clip.hitBoxScope != null)
            {
                _combatCard.IsEnabled = clip.enableAttackDetection;
                _combatCard.BindData(new CombatDetectionCardData(clip.hitBoxScope, clip.attackPolicy));
                _combatCard.DrawSceneGUI(context);
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

        private static ATEditorState GetEditorState()
        {
            if (EditorWindow.HasOpenInstances<ATEditorWindow>())
            {
                var window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                return window != null ? window.GetState() : null;
            }
            return null;
        }

        private static GameObject GetPreviewTarget()
        {
            var state = GetEditorState();
            if (state != null)
            {
                if (state.PreviewContext != null && state.PreviewContext.Owner != null) return state.PreviewContext.Owner;
                return state.previewTarget;
            }
            return null;
        }
    }
}
