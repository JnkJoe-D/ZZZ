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
        private static bool _showSpawnConfig = true;
        private static bool _showLogicTags = true;

        public override void DrawInspector(ClipBase clip)
        {
            var spawnClip = clip as SpawnClip;
            if (spawnClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 生成实体与挂载设置卡片
            _showSpawnConfig = EditorGUILayout.Foldout(_showSpawnConfig, "生成实体与挂载设置", true, EditorStyles.foldoutHeader);
            if (_showSpawnConfig)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.prefab = (GameObject)EditorGUILayout.ObjectField("生成预制体", spawnClip.prefab, typeof(GameObject), false);
                spawnClip.bindPoint = (BindPoint)EditorGUILayout.EnumPopup("生成挂载点", spawnClip.bindPoint);
                if (spawnClip.bindPoint == BindPoint.CustomBone)
                {
                    spawnClip.customBoneName = EditorGUILayout.TextField("自定义骨骼名", spawnClip.customBoneName);
                }
                spawnClip.positionOffset = EditorGUILayout.Vector3Field("位置偏移", spawnClip.positionOffset);
                spawnClip.rotationOffset = EditorGUILayout.Vector3Field("旋转偏移", spawnClip.rotationOffset);
                spawnClip.detach = EditorGUILayout.Toggle("出生后脱离父节点", spawnClip.detach);
                EditorGUILayout.EndVertical();
            }

            // 3. 逻辑与标签设置卡片
            _showLogicTags = EditorGUILayout.Foldout(_showLogicTags, "逻辑与标签参数", true, EditorStyles.foldoutHeader);
            if (_showLogicTags)
            {
                EditorGUILayout.BeginVertical("box");
                spawnClip.destroyOnInterrupt = EditorGUILayout.Toggle("被动打断时销毁", spawnClip.destroyOnInterrupt);
                spawnClip.eventTag = EditorGUILayout.TextField("事件透传标签", spawnClip.eventTag);
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

        public override void DrawSceneGUI(ClipBase obj, ATEditorState state)
        {
            var clip = obj as SpawnClip;
            if (clip == null) return;

            // 如果还没到开始时间或已经结束，可以选择不画，或者用虚线/半透明甀
            // 为了直观配置，通常只要选中了这个轨遀片段，就保持高亮绘制
            
            GetMatrix(clip, state, out Vector3 pos, out Quaternion rot);

            Handles.color = spawnColor;

            // 1. 绘制生成原点 (小球佀
            Handles.SphereHandleCap(0, pos, Quaternion.identity, indicatorRadius, EventType.Repaint);
            
            Handles.color = spawnSolidColor;
            Handles.DrawSolidDisc(pos, rot * Vector3.up, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.right, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.forward, indicatorRadius);

            // 2. 绘制朝向指示箭头 (代表生成时的正前斀
            Handles.color = spawnColor;
            
            float arrowLength = 1.5f;
            Vector3 forwardDir = rot * Vector3.forward;
            Vector3 arrowEnd = pos + forwardDir * arrowLength;
            
            Handles.DrawLine(pos, arrowEnd);
            
            // 绘制箭头头部
            float arrowHeadSize = 0.3f;
            Vector3 rightDir = rot * Vector3.right;
            Vector3 upDir = rot * Vector3.up;

            Vector3 arrowBase = arrowEnd - forwardDir * arrowHeadSize;
            Handles.DrawLine(arrowEnd, arrowBase + rightDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase - rightDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase + upDir * (arrowHeadSize * 0.5f));
            Handles.DrawLine(arrowEnd, arrowBase - upDir * (arrowHeadSize * 0.5f));

            // 可选：绘制一个简单的十字准星辅助对齐
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            float crossSize = 0.5f;
            Handles.DrawLine(pos - rightDir * crossSize, pos + rightDir * crossSize);
            Handles.DrawLine(pos - upDir * crossSize, pos + upDir * crossSize);
        }

        private void GetMatrix(SpawnClip clip, ATEditorState state, out Vector3 pos, out Quaternion rot)
        {
            Transform parent = null;
            if (state != null && state.PreviewContext != null)
            {
                var actor = state.PreviewContext.GetService<IBoneGetter>();
                if (actor != null)
                {
                    parent = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }

            if (parent == null && state != null && state.previewTarget != null)
            {
                var getter = new Game.Adapters.ATBoneGetter(state.previewTarget);
                parent = getter.GetBone(clip.bindPoint, clip.customBoneName);
            }

            if (parent != null)
            {
                pos = parent.position + parent.rotation * clip.positionOffset;
                rot = parent.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else
            {
                pos = clip.positionOffset;
                rot = Quaternion.Euler(clip.rotationOffset);
            }
        }
    }
}
