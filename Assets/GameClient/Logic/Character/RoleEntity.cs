using Game.Input;
using Game.Camera;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 玩家控制的实体。
    /// 在初始化时自动挂载本地输入与相机控制组件。
    /// </summary>
    public class RoleEntity : CharacterEntity
    {
        protected override void InitRequiredComponents()
        {
            // 玩家特有的组件：本地输入捕获、基础运动控制、相机控制
            InputProvider = gameObject.AddComponent<LocalPlayerInputProvider>();
            
            MovementController = GetComponent<MovementController>();
            if (MovementController == null) MovementController = gameObject.AddComponent<MovementController>();

            CameraController = GetComponent<CharacterCameraController>();
            if (CameraController == null) CameraController = gameObject.AddComponent<CharacterCameraController>();
        }
    }
}
