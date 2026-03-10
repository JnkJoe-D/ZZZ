using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(SpawnClip))]
    public class SpawnClipDrawer : ClipDrawer
    {
        private static readonly Color spawnColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
        private static readonly Color spawnSolidColor = new Color(0f, 1f, 1f, 0.2f);
        private const float indicatorRadius = 0.2f;

        public override void DrawInspector(ClipBase clip)
        {
            base.DrawInspector(clip);
        }

        public override void DrawSceneGUI(ClipBase obj, SkillEditorState state)
        {
            var clip = obj as SpawnClip;
            if (clip == null) return;

            // 如果还没到开始时间或已经结束，可以选择不画，或者用虚线/半透明画
            // 为了直观配置，通常只要选中了这个轨道/片段，就保持高亮绘制
            
            GetMatrix(clip, state, out Vector3 pos, out Quaternion rot);

            Handles.color = spawnColor;

            // 1. 绘制生成原点 (小球体)
            Handles.SphereHandleCap(0, pos, Quaternion.identity, indicatorRadius, EventType.Repaint);
            
            Handles.color = spawnSolidColor;
            Handles.DrawSolidDisc(pos, rot * Vector3.up, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.right, indicatorRadius);
            Handles.DrawSolidDisc(pos, rot * Vector3.forward, indicatorRadius);

            // 2. 绘制朝向指示箭头 (代表生成时的正前方)
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

        private void GetMatrix(SpawnClip clip, SkillEditorState state, out Vector3 pos, out Quaternion rot)
        {
            Transform parent = null;
            if (state != null && state.PreviewContext != null)
            {
                var actor = state.PreviewContext.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    parent = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
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
