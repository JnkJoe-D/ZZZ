using System;
using System.Collections.Generic;
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
        private readonly HashSet<int> _heldActions = new();

        public event Action OnSwitchNext;
        public event Action OnSwitchPre;

        public event Action OnMoveStarted;
        public event Action OnMovePerformed;
        public event Action OnMoveCanceled;
        public event Action OnMoveHeld;

        public event Action OnEvadeStarted;
        public event Action OnEvadePerformed;
        public event Action OnEvadeCanceled;
        public event Action OnEvadeHeld;

        public event Action OnBasicAttackStarted;
        public event Action OnBasicAttackPerformed;
        public event Action OnBasicAttackCanceled;
        public event Action OnBasicAttackHeld;

        public event Action OnSpecialAttackStarted;
        public event Action OnSpecialAttackPerformed;
        public event Action OnSpecialAttackCanceled;
        public event Action OnSpecialAttackHeld;

        public event Action OnUltimateStarted;

        public event Action OnGameplayInteractStarted;

        public bool IsHeld(int actionKey) => _heldActions.Contains(actionKey);
        public void SetHeld(int actionKey, bool held)
        {
            if (held) _heldActions.Add(actionKey);
            else _heldActions.Remove(actionKey);
        }

        public Vector2 GetMovementDirection() => movementInput;

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

        public bool HasMovementInput()
        {
            return movementInput.sqrMagnitude > 0.0001f ||
                   (useWorldMovementDirection && worldMovementDirection.sqrMagnitude > 0.0001f);
        }

        public void SetMovementDirection(Vector2 direction)
        {
            lastInput = movementInput;
            movementInput = Vector2.ClampMagnitude(direction, 1f);
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

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

        public void ClearMovement()
        {
            lastInput = Vector2.zero;
            movementInput = Vector2.zero;
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

        public void ResetInputState()
        {
            movementInput = Vector2.zero;
            worldMovementDirection = Vector3.zero;
            useWorldMovementDirection = false;
        }

        public void TriggerSwitchNext() => OnSwitchNext?.Invoke();
        public void TriggerSwitchPre() => OnSwitchPre?.Invoke();

        public void TriggerMoveStarted() => OnMoveStarted?.Invoke();
        public void TriggerMovePerformed() => OnMovePerformed?.Invoke();
        public void TriggerMoveCanceled() => OnMoveCanceled?.Invoke();
        public void TriggerMoveHeld() => OnMoveHeld?.Invoke();

        public void TriggerEvadeStarted() => OnEvadeStarted?.Invoke();
        public void TriggerEvadePerformed() => OnEvadePerformed?.Invoke();
        public void TriggerEvadeCanceled() => OnEvadeCanceled?.Invoke();
        public void TriggerEvadeHeld() => OnEvadeHeld?.Invoke();

        public void TriggerBasicAttackStarted() => OnBasicAttackStarted?.Invoke();
        public void TriggerBasicAttackPerformed() => OnBasicAttackPerformed?.Invoke();
        public void TriggerBasicAttackCanceled() => OnBasicAttackCanceled?.Invoke();
        public void TriggerBasicAttackHeld() => OnBasicAttackHeld?.Invoke();

        public void TriggerSpecialAttackStarted() => OnSpecialAttackStarted?.Invoke();
        public void TriggerSpecialAttackPerformed() => OnSpecialAttackPerformed?.Invoke();
        public void TriggerSpecialAttackCanceled() => OnSpecialAttackCanceled?.Invoke();
        public void TriggerSpecialAttackHeld() => OnSpecialAttackHeld?.Invoke();

        public void TriggerUltimateStarted() => OnUltimateStarted?.Invoke();
        public void TriggerGameplayInteractStarted() => OnGameplayInteractStarted?.Invoke();

        public Vector2 GetLastMovementDirection() => lastInput;
    }
}
