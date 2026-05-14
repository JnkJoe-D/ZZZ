using System;
using UnityEngine;

namespace Game.Input
{
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

        // ==========================================
        // Held 状态查询（物理按键持有状态，输入层维护，共享且唯一）
        // key 约定为 InputCommand 枚举值，避免输入层直接依赖逻辑层类型
        // ==========================================

        /// <summary> 查询指定按键是否处于 Held 状态 </summary>
        bool IsHeld(int actionKey);

        /// <summary> 设置 Held 状态（由输入事件回调驱动） </summary>
        void SetHeld(int actionKey, bool held);

        // ==========================================
        // 瞬间触发事件
        // ==========================================

        /// <summary>切换下一个指令触发</summary>
        event Action OnSwitchNext;

        /// <summary>切换上一个指令触发</summary>
        event Action OnSwitchPre;

        /// <summary>移动方向输入触发</summary>
        event Action OnMoveStarted;
        event Action OnMovePerformed;
        event Action OnMoveCanceled;
        event Action OnMoveHeld;

        /// <summary>闪避触发</summary>
        event Action OnEvadeStarted;
        event Action OnEvadePerformed;
        event Action OnEvadeCanceled;
        event Action OnEvadeHeld;

        /// <summary>基础普攻指令触发</summary>
        event Action OnBasicAttackStarted;
        event Action OnBasicAttackPerformed;
        event Action OnBasicAttackCanceled;
        event Action OnBasicAttackHeld;

        /// <summary>特殊攻击触发 (如 E)</summary>
        event Action OnSpecialAttackStarted;
        event Action OnSpecialAttackPerformed;
        event Action OnSpecialAttackCanceled;
        event Action OnSpecialAttackHeld;
        /// <summary>终结技触发 (如 Q)</summary>
        event Action OnUltimateStarted;
        /// <summary>非城镇下交互 (如 F)</summary>
        event Action OnGameplayInteractStarted;
    }
}
