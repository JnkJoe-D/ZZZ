using System;
using UnityEngine;

namespace Game.GamePlay
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
        /// 支持手柄摇杆与 WASD 的通用读取。带有常规松开阻尼衰减，专用于走跑状态机与通用移动路由。
        /// </summary>
        Vector2 GetMovementDirection();
        
        /// <summary>
        /// 获取当前物理原始移动输入方向（无阻尼、零延迟，直接反映硬件按键/摇杆真实瞬时状态）
        /// </summary>
        Vector2 GetRawMovementDirection();

        /// <summary>
        /// 获取输入残留缓存方向（带平滑滞后阻尼，专用于与当前原生输入对比，计算输入变化向量或180度大幅度转向）
        /// </summary>
        Vector2 GetResidualMovementDirection();

        /// <summary>
        /// 获取上一次移动方向（历史残留缓存，与 GetResidualMovementDirection 语义对齐，用于检测瞬间大幅度掉头）
        /// </summary>
        Vector2 GetLastMovementDirection();

        /// <summary>
        /// 是否有有效移动输入（常规阻尼输入，松开后在衰减期内仍判定为有，防止轻微抖动停顿）
        /// </summary>
        bool HasMoveInput();

        /// <summary>
        /// 是否有原始物理移动输入（硬性物理按键/摇杆按下状态）
        /// </summary>
        bool HasRawMoveInput();

        /// <summary>
        /// 是否存在有效的输入残留缓存（用于确认此前是否处于移动输入状态，避免从完全静止起步时被误判为掉头）
        /// </summary>
        bool HasResidualMoveInput();

        // ==========================================
        // Held 状态查询（物理按键持有状态，输入层维护，共享且唯一）
        // key 约定为 ActionCommandType 枚举值，避免输入层直接依赖逻辑层类型
        // ==========================================

        /// <summary> 查询指定按键是否处于 Held 状态 </summary>
        bool IsHeld(int actionKey);

        /// <summary> 设置 Held 状态（由输入事件回调驱动） </summary>
        void SetHeld(int actionKey, bool held);

        // ==========================================
        // 时域敲击/连续点击查询 (Tap Tracker)
        // 遵循四象限时间隔离，严格使用 Time.unscaledTime
        // ==========================================

        /// <summary>
        /// 获取指定按键在最近 windowSeconds 内的敲击/点击次数 (滑动时域)
        /// </summary>
        int GetTapCount(HardwareInputType actionKey, float windowSeconds);

        /// <summary>
        /// 获取自指定时间戳以来该按键的敲击次数 (基于动作/窗口起始点，单位：秒)
        /// </summary>
        int GetTapCountSince(HardwareInputType actionKey, float startTime);

        /// <summary>
        /// 清除指定按键的时域点击记录 (用于动作完成或消耗后的重置)
        /// </summary>
        void ResetTapTracker(HardwareInputType actionKey);

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
        /// <summary>移动输入彻底归零时触发 (从 >0 归为 0 的瞬间边沿触发)</summary>
        event Action OnMovementZero;
        /// <summary>原始物理移动输入归零时立即触发 (无阻尼，专供短输入/起步即时停止)</summary>
        event Action OnRawMovementZero;

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
