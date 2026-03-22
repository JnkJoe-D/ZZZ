using System;
using cfg;
using Game.Input;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// AI 专用输入代理，实现和玩家输入相同的接口，供角色状态机直接消费。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AIInputProvider : MonoBehaviour, IInputProvider
    {
        private Vector2 movementInput;
        private Vector2 lastInput;
        private Vector3 worldMovementDirection;
        private bool useWorldMovementDirection;

        public event Action OnSwitchNext;
        public event Action OnSwitchPre;
        public event Action OnEvadeBackStarted;
        public event Action OnBasicAttackStarted;
        public event Action OnBasicAttackCanceled;
        public event Action OnBasicAttackHoldStart;
        public event Action OnBasicAttackHold;
        public event Action OnBasicAttackHoldCancel;
        public event Action OnSpecialAttack;
        public event Action OnUltimate;
        public event Action OnGameplayInteract;
        public event Action OnEvadeFrontStarted;
        public event Action OnSpecialAttackHoldStart;
        public event Action OnSpecialAttackHold;
        public event Action OnSpecialAttackHoldCancel;


        /// <summary>
        /// 获取当前的二维移动输入。
        /// </summary>
        /// <returns>当前移动方向。</returns>
        public Vector2 GetMovementDirection()
        {
            return movementInput;
        }

        /// <summary>
        /// 尝试读取世界空间移动方向。
        /// </summary>
        /// <param name="worldDirection">输出的世界空间方向。</param>
        /// <returns>当前是否启用了世界空间方向。</returns>
        public bool TryGetWorldMovementDirection(out Vector3 worldDirection)
        {
            if (useWorldMovementDirection && worldMovementDirection.sqrMagnitude > 0.0001f)
            {
                worldDirection = worldMovementDirection;
                return true;
            }

            worldDirection = Vector3.zero;
            return false;
        }

        /// <summary>
        /// 判断当前是否存在移动输入。
        /// </summary>
        /// <returns>是否有移动输入。</returns>
        public bool HasMovementInput()
        {
            return movementInput.sqrMagnitude > 0.0001f || (useWorldMovementDirection && worldMovementDirection.sqrMagnitude > 0.0001f);
        }

        /// <summary>
        /// 读取指定动作的按住状态。
        /// </summary>
        /// <param name="type">要查询的动作类型。</param>
        /// <returns>动作是否处于按住状态。</returns>
        public bool GetActionState(InputActionType type)
        {
            return false;
        }

        /// <summary>
        /// 设置移动方向输入。
        /// </summary>
        /// <param name="direction">新的移动方向。</param>
        public void SetMovementDirection(Vector2 direction)
        {
            lastInput = movementInput;
            movementInput = Vector2.ClampMagnitude(direction, 1f);
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

        /// <summary>
        /// 设置移动方向输入。
        /// </summary>
        /// <param name="direction">移动方向。</param>
        /// <param name="isDashHeld">弃用参数。</param>
        [Obsolete("Use SetMovementDirection instead. isDashHeld is no longer driven by BT.")]
        public void SetMovement(Vector2 direction, bool isDashHeld)
        {
            SetMovementDirection(direction);
        }

        /// <summary>
        /// 以世界空间方向设置移动输入。
        /// </summary>
        /// <param name="direction">世界空间方向。</param>
        /// <param name="isDashHeld">弃用参数。</param>
        [Obsolete("Use SetWorldMovement(Vector3) instead.")]
        public void SetWorldMovement(Vector3 direction, bool isDashHeld)
        {
            SetWorldMovement(direction);
        }

        /// <summary>
        /// 以世界空间方向设置移动输入，供 AI 直接朝目标移动。
        /// </summary>
        /// <param name="direction">世界空间方向。</param>
        public void SetWorldMovement(Vector3 direction)
        {
            Vector3 normalizedDirection = direction;
            normalizedDirection.y = 0f;
            normalizedDirection = normalizedDirection.sqrMagnitude > 0.0001f
                ? normalizedDirection.normalized
                : Vector3.zero;

            worldMovementDirection = normalizedDirection;
            useWorldMovementDirection = normalizedDirection.sqrMagnitude > 0.0001f;

            lastInput = movementInput;
            movementInput = new Vector2(normalizedDirection.x, normalizedDirection.z);
        }

        /// <summary>
        /// 清空移动输入。
        /// </summary>
        public void ClearMovement()
        {
            lastInput = Vector2.zero;
            movementInput = Vector2.zero;
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

        /// <summary>
        /// 重置所有输入状态。
        /// </summary>
        public void ResetInputState()
        {
            movementInput = Vector2.zero;
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

        /// <summary>
        /// 重置输入状态的兼容入口。
        /// </summary>
        public void ResetState()
        {
            ResetInputState();
        }

        /// <summary>触发切换到下一个角色事件。</summary>
        public void TriggerSwitchNext() => OnSwitchNext?.Invoke();
        /// <summary>触发切换到上一个角色事件。</summary>
        public void TriggerSwitchPre() => OnSwitchPre?.Invoke();
        /// <summary>触发前闪避事件。</summary>
        public void TriggerEvadeFront() => OnEvadeFrontStarted?.Invoke();
        /// <summary>触发后闪避事件。</summary>
        public void TriggerEvadeBack() => OnEvadeBackStarted?.Invoke();
        /// <summary>触发普攻开始事件。</summary>
        public void TriggerBasicAttack() => OnBasicAttackStarted?.Invoke();
        /// <summary>触发普攻取消事件。</summary>
        public void TriggerBasicAttackCancel() => OnBasicAttackCanceled?.Invoke();
        /// <summary>触发普攻蓄力开始事件。</summary>
        public void TriggerBasicAttackHoldStart() => OnBasicAttackHoldStart?.Invoke();
        /// <summary>触发普攻蓄力持续事件。</summary>
        public void TriggerBasicAttackHold() => OnBasicAttackHold?.Invoke();
        /// <summary>触发普攻蓄力取消事件。</summary>
        public void TriggerBasicAttackHoldCancel() => OnBasicAttackHoldCancel?.Invoke();
        /// <summary>触发特殊技事件。</summary>
        public void TriggerSpecialAttack() => OnSpecialAttack?.Invoke();
        /// <summary>触发终结技事件。</summary>
        public void TriggerUltimate() => OnUltimate?.Invoke();
        /// <summary>触发交互事件。</summary>
        public void TriggerGameplayInteract() => OnGameplayInteract?.Invoke();

        public Vector2 GetLastMovementDirection()
        {
            return lastInput;
        }

    }
}
