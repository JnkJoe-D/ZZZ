using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 客户端输入全局管理器。
    /// 统筹玩家输入状态，控制游戏输入与 UI 层的挂起/恢复（如打开全屏 UI 时屏蔽角色操作与运镜）。
    /// 挂靠于 GameRoot 下。
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        public bool IsPlayerInputEnabled { get; private set; } = true;

        public void Initialize()
        {
            IsPlayerInputEnabled = true;
            GLog.Info(LogTags.Input, "初始化完成");
        }

        public void Shutdown()
        {
            IsPlayerInputEnabled = false;
            GLog.Info(LogTags.Input, "已关闭");
        }

        /// <summary>
        /// 统一设置玩法/战斗角色输入总开关。
        /// </summary>
        public void SetGameplayInputActive(bool active)
        {
            if (IsPlayerInputEnabled == active)
            {
                return;
            }

            IsPlayerInputEnabled = active;
            TeamManager.Instance?.SetInputEnable(active);
            GLog.Info(LogTags.Input, $"玩法输入已{(active ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 开启玩家主控制层
        /// </summary>
        public void EnablePlayerInput()
        {
            SetGameplayInputActive(true);
        }

        /// <summary>
        /// 关闭玩家主控制层，专注 UI (比如打开全屏大面板时)
        /// </summary>
        public void EnableUIInput()
        {
            SetGameplayInputActive(false);
        }
    }
}
