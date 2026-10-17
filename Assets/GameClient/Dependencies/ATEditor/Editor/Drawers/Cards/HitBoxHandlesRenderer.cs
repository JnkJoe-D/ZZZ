using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 攻击检测盒在 SceneView 中的统一 Handles 渲染器
    /// 集中管理 Sphere, Box, Capsule, Sector, Ring 等全部碰撞几何形状的辅助线与面元渲染
    /// </summary>
    public static class HitBoxHandlesRenderer
    {
        public static void DrawGizmos(HitBoxShape shape, Vector3 pos, Quaternion rot, bool isActive, Color? customWireColor = null, Color? customSolidColor = null)
        {
            if (shape == null) return;

            Color wireColor = customWireColor ?? (isActive ? new Color(0, 1, 0, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            Color solidColor = customSolidColor ?? (isActive ? new Color(0, 1, 0, 0.2f) : new Color(0.5f, 0.5f, 0.5f, 0.1f));

            Matrix4x4 oldMatrix = Handles.matrix;
            Color oldColor = Handles.color;

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
                    float extraHeight = Mathf.Max(0, shape.height - shape.radius * 2);
                    Vector3 upCap = Vector3.up * (extraHeight / 2f);
                    Vector3 downCap = Vector3.down * (extraHeight / 2f);

                    Handles.DrawWireArc(upCap, Vector3.up, Vector3.forward, 360, shape.radius);
                    Handles.DrawWireArc(downCap, Vector3.up, Vector3.forward, 360, shape.radius);

                    Handles.DrawWireArc(upCap, Vector3.right, Vector3.back, 180, shape.radius);
                    Handles.DrawWireArc(upCap, Vector3.forward, Vector3.right, 180, shape.radius);

                    Handles.DrawWireArc(downCap, Vector3.right, Vector3.forward, 180, shape.radius);
                    Handles.DrawWireArc(downCap, Vector3.forward, Vector3.left, 180, shape.radius);

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
                    float hR = shape.height / 2f;
                    Vector3 upRing = Vector3.up * hR;
                    Vector3 downRing = Vector3.down * hR;

                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);

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

            Handles.color = oldColor;
            Handles.matrix = oldMatrix;
        }
    }
}
