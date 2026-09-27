using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色动作受阻挤压与视觉模型位移偏移呈现器接口。
    /// 玩法层向表现层提出的契约，供动力学计算解耦模型偏移。
    /// </summary>
    public interface IVisualOffsetPresenter
    {
        void ApplyVisualOffset(Vector3 rawLocalDelta);
        void ResetVisualOffset();
        void SetVisualRecover(bool active, float speed = 0f);
        void SetVisualOffsetMode(MotionWindowVisualOffsetMode mode);

        /// <summary>
        /// 设置视觉模型局部偏航角旋转偏移 (单位：度)
        /// </summary>
        void SetVisualRotationOffset(float yawOffset);

        /// <summary>
        /// 重置视觉模型局部旋转偏移为 0 度
        /// </summary>
        void ResetVisualRotationOffset();
    }
}
