using UnityEditor;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(HitClip))]
    public class HitClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            base.DrawInspector(clip);
        }

        public override void DrawSceneGUI(ClipBase clip, SkillEditorState state)
        {
            var damageClip = clip as HitClip;
            if (damageClip == null) return;

            // 判断是否在时间范围内，只有在非停止状态下（预览或播放）才允许激活
            bool isActive = !state.isStopped && state.timeIndicator >= clip.StartTime && state.timeIndicator <= clip.StartTime + clip.Duration;

            // 获取 Matrix (传入 state 以供获取 Context)
            GetMatrix(damageClip, state, out Vector3 pos, out Quaternion rot);

            // 绘制
            Color wireColor = isActive ? new Color(0, 1, 0, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Color solidColor = isActive ? new Color(0, 1, 0, 0.2f) : new Color(0.5f, 0.5f, 0.5f, 0.1f);
            
            var shape = damageClip.shape;

            Handles.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
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
                    // 修正 Capsule 绘制，高度必须大于 2 倍半径，多出的部分才是两端圆球的偏移
                    float extraHeight = Mathf.Max(0, shape.height - shape.radius * 2);
                    Vector3 upCap = Vector3.up * (extraHeight / 2f);
                    Vector3 downCap = Vector3.down * (extraHeight / 2f);
                    
                    // 顶面圆和底面圆 (平行于 XZ 平面)
                    Handles.DrawWireArc(upCap, Vector3.up, Vector3.forward, 360, shape.radius);
                    Handles.DrawWireArc(downCap, Vector3.up, Vector3.forward, 360, shape.radius);

                    // 上半球 (垂直于 XZ 平面的两个交叉半圆)
                    // X轴旋转: 从后向前，正旋转经过上
                    Handles.DrawWireArc(upCap, Vector3.right, Vector3.back, 180, shape.radius);
                    // Z轴旋转: 从左向右，正旋转经过上
                    Handles.DrawWireArc(upCap, Vector3.forward, Vector3.right, 180, shape.radius);

                    // 下半球 (垂直于 XZ 平面的两个交叉半圆)
                    // X轴旋转: 从前向后，正旋转经过下
                    Handles.DrawWireArc(downCap, Vector3.right, Vector3.forward, 180, shape.radius);
                    // Z轴旋转: 从右向左，正旋转经过下
                    Handles.DrawWireArc(downCap, Vector3.forward, Vector3.left, 180, shape.radius);

                    // 躯干连接线 (垂直高度线条)
                    Handles.DrawLine(upCap + Vector3.forward * shape.radius, downCap + Vector3.forward * shape.radius);
                    Handles.DrawLine(upCap + Vector3.back * shape.radius, downCap + Vector3.back * shape.radius);
                    Handles.DrawLine(upCap + Vector3.right * shape.radius, downCap + Vector3.right * shape.radius);
                    Handles.DrawLine(upCap + Vector3.left * shape.radius, downCap + Vector3.left * shape.radius);
                    break;

                case HitBoxType.Sector:
                    float hS = shape.height / 2f;
                    Vector3 forwardDir = Vector3.forward;
                    Vector3 rightBoundary = Quaternion.Euler(0, shape.angle / 2, 0) * forwardDir;
                    Vector3 leftBoundary = Quaternion.Euler(0, -shape.angle / 2, 0) * forwardDir;
                    
                    Vector3 upCenter = Vector3.up * hS;
                    Vector3 downCenter = Vector3.down * hS;

                    // 上下弧线
                    Handles.DrawWireArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    Handles.DrawWireArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);

                    // 侧边连线
                    Handles.DrawLine(upCenter, upCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(upCenter, upCenter + leftBoundary * shape.radius);
                    Handles.DrawLine(downCenter, downCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(downCenter, downCenter + leftBoundary * shape.radius);

                    // 垂直连线 (连接上下两片的顶点和圆心)
                    Handles.DrawLine(upCenter, downCenter);
                    Handles.DrawLine(upCenter + rightBoundary * shape.radius, downCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(upCenter + leftBoundary * shape.radius, downCenter + leftBoundary * shape.radius);

                    // 填充区域 (这里画底面和顶面即可)
                    Handles.color = solidColor;
                    Handles.DrawSolidArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    Handles.DrawSolidArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    break;

                case HitBoxType.Ring:
                    float hR = shape.height / 2f;
                    Vector3 upRing = Vector3.up * hR;
                    Vector3 downRing = Vector3.down * hR;

                    // 外圈上下
                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    // 内圈上下
                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);

                    // 垂直连线辅助阅读
                    Handles.DrawLine(upRing + Vector3.forward * shape.radius, downRing + Vector3.forward * shape.radius);
                    Handles.DrawLine(upRing - Vector3.forward * shape.radius, downRing - Vector3.forward * shape.radius);
                    Handles.DrawLine(upRing + Vector3.right * shape.radius, downRing + Vector3.right * shape.radius);
                    Handles.DrawLine(upRing - Vector3.right * shape.radius, downRing - Vector3.right * shape.radius);
                    
                    Handles.DrawLine(upRing + Vector3.forward * shape.innerRadius, downRing + Vector3.forward * shape.innerRadius);
                    Handles.DrawLine(upRing - Vector3.forward * shape.innerRadius, downRing - Vector3.forward * shape.innerRadius);
                    Handles.DrawLine(upRing + Vector3.right * shape.innerRadius, downRing + Vector3.right * shape.innerRadius);
                    Handles.DrawLine(upRing - Vector3.right * shape.innerRadius, downRing - Vector3.right * shape.innerRadius);
                    break;
            }

            Handles.matrix = Matrix4x4.identity;
        }

        private void GetMatrix(HitClip clip, SkillEditorState state, out Vector3 pos, out Quaternion rot)
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
