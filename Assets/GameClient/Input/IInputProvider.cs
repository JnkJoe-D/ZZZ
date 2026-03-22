using System;
using UnityEngine;

namespace Game.Input
{
    public enum InputActionType
    {
        None = 0,
        Dash = 1,
        Block = 2,
        // ... 未来有需要都可以往这加
    }

    /// <summary>
    /// 标准化玩家输入接口
    /// 遵循依赖倒置原则（DIP），将具体的输入实现设备（键盘鼠标/行为树AI/网络帧）与具体的业务解耦。
    /// 所有需要操控人物的模块仅需要引用此类。
    /// </summary>
    public interface IInputProvider
    {
        // ==========================================
        // 轮询状态属性 (适合 Update、FSM 主动拉取)
        // ==========================================

        /// <summary>
        /// 获取当前移动方向（归一化后的二维向量）
        /// 支持手柄摇杆与 WASD 的通用读取。
        /// </summary>
        Vector2 GetMovementDirection();
        
        /// <summary>
        /// 获取上一次移动方向（用于检测瞬间大幅度掉头）
        /// </summary>
        Vector2 GetLastMovementDirection();

        /// <summary>
        /// 是否有移动意图
        /// </summary>
        bool HasMovementInput();

        /// <summary>
        /// 查询某种指定的逻辑动作在其生命周期里是否处于“保持触发(Hold)”状态
        /// 适合那些不能单纯依靠按下瞬间判定（如：按住冲刺、长按防御）的玩法
        /// </summary>
        bool GetActionState(InputActionType type);

        // ==========================================
        // 瞬间触发事件 (适合按键按下/抬起等一次性行为)
        // 这些事件未来可被 Unity New Input System 改键
        // ==========================================

        /// <summary>切换下一个指令触发 (如 Space 键)</summary>
        event Action OnSwitchNext;

        /// <summary>切换上一个指令触发 (如 C 键)</summary>
        event Action OnSwitchPre;
        /// <summary>闪避触发 (如 Shift 键)</summary>
        event Action OnEvadeFrontStarted;
        event Action OnEvadeBackStarted;

        /// <summary>基础普攻指令触发 (如 鼠标左键)</summary>
        event Action OnBasicAttackStarted;
        /// <summary>基础普攻指令释放触发 (用于区分长按和点按)</summary>
        event Action OnBasicAttackCanceled;
        /// <summary>基础普攻指令长按触发 (如 鼠标左键)</summary>
        event Action OnBasicAttackHoldStart;
        event Action OnBasicAttackHold;
        event Action OnBasicAttackHoldCancel;

        /// <summary>特殊攻击触发 (如 E)</summary>
        event Action OnSpecialAttack;
        /// <summary>特殊攻击触发长按 (如 E)</summary>
        event Action OnSpecialAttackHoldStart;
        event Action OnSpecialAttackHold;
        event Action OnSpecialAttackHoldCancel;
        /// <summary>终结技触发 (如 Q)</summary>
        event Action OnUltimate;
        /// <summary>非城镇下交互 (如 F)</summary>
        event Action OnGameplayInteract;
    }
}
