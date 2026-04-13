using System;
using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// 依附于本地玩家 GameObject 上的输入捕获提供者
    /// 将全局/底层的键鼠信号转化为 IInputProvider 标准事件
    /// </summary>
    public class LocalPlayerInputProvider : MonoBehaviour, IInputProvider
    {
        public event Action OnSwitchNext;
        public event Action OnSwitchPre;
        public event Action OnMoveStarted;
        public event Action OnMovePerformed;
        public event Action OnMoveCanceled;
        public event Action OnMoveHeld;
        public event Action OnMoveHeldCanceled;
        // public event Action OnEvadeFrontStarted;
        public event Action OnEvadeStarted;
        public event Action OnEvadePerformed;
        public event Action OnEvadeCanceled;
        public event Action OnEvadeHeld;
        public event Action OnBasicAttackStarted;
        public event Action OnBasicAttackCanceled;
        public event Action OnBasicAttackHoldStart;
        public event Action OnBasicAttackHeld;
        public event Action OnBasicAttackHoldCancel;
        public event Action OnSpecialAttack;
        public event Action OnSpecialAttackHoldStart;
        public event Action OnSpecialAttackHold;
        public event Action OnSpecialAttackHoldCancel;
        public event Action OnUltimate;
        public event Action OnGameplayInteract;

        private PlayerControl _input;
        private Vector2 _currentMoveInput;
        private Vector2 _lastMoveInput;

        private void Awake()
        {
            _input = new PlayerControl();

            // 订阅瞬发事件
            _input.GamePlay.Move.started += _ => OnMoveStarted?.Invoke();
            _input.GamePlay.Move.performed += _ => OnMovePerformed?.Invoke();
            _input.GamePlay.Move.canceled += _ => OnMoveCanceled?.Invoke();
            _input.GamePlay.MoveHeld.performed += _ => OnMoveHeld?.Invoke();
            _input.GamePlay.MoveHeld.canceled += _ => OnMoveHeldCanceled?.Invoke();

            // _input.GamePlay.EvadeFront.started += _ => OnEvadeFrontStarted?.Invoke();
            _input.GamePlay.Evade.started += _ => OnEvadeStarted?.Invoke();
            _input.GamePlay.Evade.performed += _ => OnEvadePerformed?.Invoke();
            _input.GamePlay.Evade.canceled += _ => OnEvadeCanceled?.Invoke();
            _input.GamePlay.EvadeHeld.performed += _ => OnEvadeHeld?.Invoke();
            _input.GamePlay.LightAttack.started += _ => OnBasicAttackStarted?.Invoke();
            _input.GamePlay.LightAttack.canceled += _ => OnBasicAttackCanceled?.Invoke();
            _input.GamePlay.LightAttackHeld.started += _ => OnBasicAttackHoldStart?.Invoke();
            _input.GamePlay.LightAttackHeld.performed += _ => OnBasicAttackHeld?.Invoke();
            _input.GamePlay.LightAttackHeld.canceled += _ => OnBasicAttackHoldCancel?.Invoke();
            _input.GamePlay.SpecialSkill.started += _ => OnSpecialAttack?.Invoke();
            _input.GamePlay.SpecialSkillHeld.started += _ => OnSpecialAttackHold?.Invoke();
            _input.GamePlay.SpecialSkillHeld.performed += _ => OnSpecialAttackHoldStart?.Invoke();
            _input.GamePlay.SpecialSkillHeld.canceled += _ => OnSpecialAttackHoldCancel?.Invoke();
            _input.GamePlay.Ultimate.started += _ => OnUltimate?.Invoke();
            _input.GamePlay.Interact.started += _ => OnGameplayInteract?.Invoke();
            _input.GamePlay.SwitchNext.started += _ => OnSwitchNext?.Invoke();
            _input.GamePlay.SwitchPre.started += _ => OnSwitchPre?.Invoke();

        }

        private void OnEnable()
        {
            _input.Enable();
        }

        private void OnDisable()
        {
            _input.Disable();
        }

        private void Update()
        {
            // 每帧获取摇杆/WASD数据
            _lastMoveInput = _currentMoveInput;
            _currentMoveInput = _input.GamePlay.Move.ReadValue<Vector2>();
        }

        // ==========================================
        // 实现 IInputProvider 接口
        // ==========================================
        
        public Vector2 GetMovementDirection()
        {
            return _input?.GamePlay.Move.ReadValue<Vector2>() ?? Vector2.zero;
        }

        public Vector2 GetLastMovementDirection()
        {
            return _lastMoveInput;
        }

        public bool HasMovementInput()
        {
            return GetMovementDirection().sqrMagnitude > 0.01f;
        }
    }
}
