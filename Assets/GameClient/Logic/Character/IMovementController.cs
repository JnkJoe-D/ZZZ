using SkillEditor;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 标准化移动代理接口
    /// 替换对原生 UnityEngine.CharacterController 的死板依赖，提供可自定制、防穿模的接口层。
    /// </summary>
    public interface IMovementController
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
    }
}
