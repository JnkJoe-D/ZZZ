using Game.Logic.Character;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Game.Camera
{
    /// <summary>
    /// 脱水的、独立于各角色的专属相机控制接口
    /// 在业务逻辑中，不直接操作 Cinemachine 或 Camera.main
    /// 只通过它获取对齐向量，以及通知它开启和关闭自由旋转输入
    /// </summary>
    public interface ICameraController
    {
        void Init(CharacterEntity entity);
        /// <summary>
        /// 冻结/解冻相机视角的旋转输入（例如进入 UI 或者释放某些固定视角的锁敌技能时禁用）
        /// </summary>
        void EnableInput(bool enable);

        /// <summary>
        /// 提供目标当前的视觉主前向向量（只取水平 XZ 分量并正规化）
        /// 供移动系统和地面 FSM 推算实际挪动方向
        /// </summary>
        Vector3 GetForward();

        /// <summary>
        /// 提供目标当前的视觉主右向向量（只取水平 XZ 分量并正规化）
        /// </summary>
        Vector3 GetRight();

        // 未来可以按需扩展：
        // void SetFOV(float fov, float duration);
        // void LockOn(Transform target);
    }
}
