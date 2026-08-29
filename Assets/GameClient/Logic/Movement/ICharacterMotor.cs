using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    /// <summary>
    /// 标准化移动代理接口
    /// 替换对原生 UnityEngine.CharacterController 的死板依赖，提供可自定制、防穿模的接口层。
    /// </summary>
    public interface ICharacterMotor
    {
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
        void RotateTo(Vector3 worldDirection, float speed = -1f, Vector3 localOffset = default);
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
