using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 全屏十字闪光解耦服务接口。
    /// 贯彻单向依赖契约，将时间轴片段触发逻辑解耦下发至玩法层，不直接依赖 View 层与时钟句柄。
    /// </summary>
    public interface ICrossFlashHandler : IService
    {
        /// <summary>
        /// 触发全屏十字闪光
        /// </summary>
        /// <param name="worldPosition">初始触发世界坐标</param>
        /// <param name="targetTransform">跟随目标骨骼 Transform (若 FollowTarget 为 false 可为 null)</param>
        /// <param name="positionOffset">绑定点的世界坐标偏移量 (仅同步坐标不同步旋转)</param>
        /// <param name="parameters">闪光参数</param>
        void TriggerCrossFlash(Vector3 worldPosition, Transform targetTransform, Vector3 positionOffset, in CrossFlashParameters parameters);
    }
}
