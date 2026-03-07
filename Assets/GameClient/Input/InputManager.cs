using Game.Framework;
using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// 客户端输入全局管理器
    /// 负责统筹 Input Actions 生命期，并控制 Action Map 的挂起和恢复（如打开 UI 时屏蔽底层操作）
    /// 挂靠于 GameRoot 下
    /// </summary>
    public class InputManager : Game.Framework.Singleton<InputManager>
    {
        // 此处先不直接绑定特定的强类型 PlayerInputActions，
        // 而是提供一个标准的初始化和禁用/启用口子。
        // （待后续 Unity Editor 中生成具体的 PlayerInputActions 脚本后注入或在此处 new 出）
        // private PlayerInputActions _inputActions;
        
        public void Initialize()
        {
            // TODO: 等待 PlayerInputActions 脚本生成后取消注释
            // _inputActions = new PlayerInputActions();
            // _inputActions.Enable();
            
            Debug.Log("[InputManager] 初始化完成");
        }

        public void Shutdown()
        {
            // if (_inputActions != null)
            // {
            //     _inputActions.Disable();
            //     _inputActions = null;
            // }
            Debug.Log("[InputManager] 已关闭");
        }

        /// <summary>
        /// 开启玩家主控制层
        /// </summary>
        public void EnablePlayerInput()
        {
            // _inputActions?.Player.Enable();
            // _inputActions?.UI.Disable();
            Debug.Log("[InputManager] 玩家输入已启用");
        }

        /// <summary>
        /// 关闭玩家主控制层，专注 UI (比如打开全屏大面板时)
        /// </summary>
        public void EnableUIInput()
        {
            // _inputActions?.Player.Disable();
            // _inputActions?.UI.Enable();
            Debug.Log("[InputManager] 玩家输入已禁用，切换至 UI 层");
        }
        
        // public PlayerInputActions Actions => _inputActions;
    }
}
