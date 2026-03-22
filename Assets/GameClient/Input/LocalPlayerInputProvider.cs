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
        public event Action OnEvadeFrontStarted;
        public event Action OnEvadeBackStarted;
        public event Action OnBasicAttackStarted;
        public event Action OnBasicAttackCanceled;
        public event Action OnBasicAttackHoldStart;
        public event Action OnBasicAttackHold;
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
            _input.GamePlay.EvadeFront.started += _ => OnEvadeFrontStarted?.Invoke();
            _input.GamePlay.EvadeBack.started += _ => OnEvadeBackStarted?.Invoke();
            _input.GamePlay.LightAttack.started += _ => OnBasicAttackStarted?.Invoke();
            _input.GamePlay.LightAttack.canceled += _ => OnBasicAttackCanceled?.Invoke();
            _input.GamePlay.LightAttackHold.started += _ => OnBasicAttackHoldStart?.Invoke();
            _input.GamePlay.LightAttackHold.performed += _ => OnBasicAttackHold?.Invoke();
            _input.GamePlay.LightAttackHold.canceled += _ => OnBasicAttackHoldCancel?.Invoke();
            _input.GamePlay.SpecialSkill.started += _ => OnSpecialAttack?.Invoke();
            _input.GamePlay.SpecialSkillHold.started += _ => OnSpecialAttackHold?.Invoke();
            _input.GamePlay.SpecialSkillHold.performed += _ => OnSpecialAttackHoldStart?.Invoke();
            _input.GamePlay.SpecialSkillHold.canceled += _ => OnSpecialAttackHoldCancel?.Invoke();
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
            return _currentMoveInput;
        }

        public Vector2 GetLastMovementDirection()
        {
            return _lastMoveInput;
        }

        public bool HasMovementInput()
        {
            return _currentMoveInput.sqrMagnitude > 0.01f;
        }

        public bool GetActionState(InputActionType type)
        {
            // 用 IsPressed 支持长按检测
            if (type == InputActionType.Dash)
            {
                // 注意这里查的是您在 InputMap 里命名的 Dodge
                return _input.GamePlay.EvadeBack.IsPressed();
            }
            return false;
        }
    }
}
