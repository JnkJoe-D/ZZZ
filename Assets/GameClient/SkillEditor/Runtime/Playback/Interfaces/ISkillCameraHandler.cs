using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 相机服务接口
    /// 采用 SetCamera 缓存模式：Enter 时设置目标相机，后续操作直接作用于已缓存引用
    /// </summary>
    public interface ISkillCameraHandler
    {
        /// <summary>
        /// 切换当前操作的虚拟相机（Handler 内部缓存该引用）
        /// </summary>
        /// <param name="cameraIndex">相机Id</param>
        void SetCamera(int cameraId,GameObject target);

        /// <summary>
        /// 设置已缓存相机的 PathPosition
        /// </summary>
        /// <param name="position">路径位置值（由曲线采样输出）</param>
        void SetPathPosition(float position);

        /// <summary>
        /// 释放当前缓存的相机引用（Clip 结束时调用）
        /// </summary>
        void ReleaseCamera();
    }
}
