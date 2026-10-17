using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 移动与轨迹设置卡片抽屉 (ProjectileMovementConfig)
    /// 管理飞行位移参数并在 SceneView 中绘制直线射程参考虚线
    /// </summary>
    public class ProjectileMovementCardDrawer : CardDrawerBase<ProjectileMovementConfig>
    {
        private readonly bool _isMandatory;
        public override bool IsMandatory => _isMandatory;
        public override string CardTitle => "移动与轨迹设置";

        public ProjectileMovementCardDrawer(bool isMandatory = true)
        {
            _isMandatory = isMandatory;
        }

        protected override void OnDrawBody(CardDrawContext context)
        {
            DrawSubField(Data, "moveMode");
            if (Data.moveMode != ProjectileMoveMode.Static)
            {
                DrawSubField(Data, "initialSpeed");
            }
            if (Data.moveMode == ProjectileMoveMode.StraightLine)
            {
                DrawSubField(Data, "acceleration");
                DrawSubField(Data, "maxSpeed");
            }
            else if (Data.moveMode == ProjectileMoveMode.TargetTracking)
            {
                DrawSubField(Data, "turnSpeed");
            }
            else if (Data.moveMode == ProjectileMoveMode.Parabola)
            {
                DrawSubField(Data, "gravityScale");
            }
            DrawSubField(Data, "orientToVelocity");
            DrawSubField(Data, "maxDistance");
        }

        protected override void OnDrawSceneGUI(CardDrawContext context)
        {
            if (Data.moveMode == ProjectileMoveMode.StraightLine)
            {
                float range = Data.maxDistance > 0 ? Mathf.Min(Data.maxDistance, 20f) : 10f;
                Handles.color = new Color(0f, 1f, 1f, 0.4f);
                Vector3 forwardDir = context.OriginPose.rotation * Vector3.forward;
                Handles.DrawDottedLine(context.OriginPose.position, context.OriginPose.position + forwardDir * range, 3f);
            }
        }
    }
}
