using UnityEngine;
using ATEditor;
namespace Game.GamePlay
{
    /// <summary>
    /// 标准化移动代理组件接口。
    /// 规范实体位移、重力吸附、Root Motion 与旋转朝向控制。
    /// </summary>
    public interface IMovementComponent : IEntityComponent
    {
        CharacterController CharacterController { get; }
        float TurnSpeed { get; set; }
        /// <summary> 实体的有效物理几何半径（来自 CharacterController 或 CapsuleCollider） </summary>
        float CharacterRadius { get; }
        void Init(CharacterEntity entity);
        /// <summary>
        /// 驱动角色向指定世界坐标系的方向向量平移
        /// </summary>
        /// <param name="velocity">每秒移动的法向速度量</param>
        void Move(Vector3 velocity);

        void ResetVisualOffset();
        void SetVisualRecover(bool active, float speed = 0f);
        void SetFilterMode(MotionWindowLocalDeltaFilterMode filterMode);
        void SetCollisionMode(RootMotionCollisionMode mode);
        void SetObstacleMask(LayerMask mask);
        void SetVisualOffsetMode(MotionWindowVisualOffsetMode visualOffsetMode);
        /// <summary>
        /// 设置角色朝向
        /// </summary>
        void RotateTo(Vector3 worldDirection, float speed = -1f, Vector3 localOffset = default, float dt = -1f);
        void RotateToImmediately(Vector3 worldDirection, Vector3 localOffset = default);

        void FaceTo(Vector2 inputDir, float speed = -1f, Vector3 localOffset = default);
        void FaceTo(Vector3 worldDir, float speed = -1f, Vector3 localOffset = default);

        void FaceToImmediately(Vector2 inputDir, Vector3 localOffset = default);
        void FaceToImmediately(Vector3 worldDir, Vector3 localOffset = default);

        void FaceToTarget(Transform target, float speed = -1f, Vector3 localOffset = default);
        void FaceToTargetImmediately(Transform target, Vector3 localOffset = default);
        Vector3 CalculateWorldDirection(Vector2 inputDir);

        /// <summary>
        /// 是否在地面上
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// 当前移动速度向量 (m/s)
        /// </summary>
        Vector3 Velocity { get; }

        /// <summary>
        /// 当前垂直速度 (m/s)
        /// </summary>
        float VerticalVelocity { get; }

        /// <summary>
        /// 重置垂直速度
        /// </summary>
        void ResetVerticalVelocity();

        /// <summary>
        /// 设置垂直速度 (用于跳跃、击飞等动量赋予)
        /// </summary>
        void SetVerticalVelocity(float velocity);
    }
}
