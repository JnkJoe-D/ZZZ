using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Logic;
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

        private PlayerControl _input;
        private Vector2 _currentMoveInput;
        private Vector2 _lastMoveInput;
        private readonly HashSet<int> _heldActions = new();

        public bool IsHeld(int actionKey) => _heldActions.Contains(actionKey);
        public void SetHeld(int actionKey, bool held)
        {
            if (held) _heldActions.Add(actionKey);
            else _heldActions.Remove(actionKey);
        }

        private void Awake()
        {
            _input = new PlayerControl();

            // 订阅瞬发事件
            _input.GamePlay.Move.started += _ => OnMoveStarted?.Invoke();
            _input.GamePlay.Move.performed += _ => OnMovePerformed?.Invoke();
            _input.GamePlay.Move.canceled += _ =>
            {
                OnMoveCanceled?.Invoke();
                _heldActions.Remove((int)InputCommand.Move);
            };
            _input.GamePlay.MoveHeld.performed += _ =>
            {
                OnMoveHeld?.Invoke();
                _heldActions.Add((int)InputCommand.Move);
            };
            // 闪避
            _input.GamePlay.Evade.started += _ => OnEvadeStarted?.Invoke();
            _input.GamePlay.Evade.performed += _ => OnEvadePerformed?.Invoke();
            _input.GamePlay.Evade.canceled += _ =>
            {
                OnEvadeCanceled?.Invoke();
                _heldActions.Remove((int)InputCommand.Evade); 
            };
            _input.GamePlay.EvadeHeld.performed += _ =>
            {
                OnEvadeHeld?.Invoke();
                _heldActions.Add((int)InputCommand.Evade);
            };
            // 普通攻击
            _input.GamePlay.LightAttack.started += _ => OnBasicAttackStarted?.Invoke();
            _input.GamePlay.LightAttack.performed += _ => OnBasicAttackPerformed?.Invoke();
            _input.GamePlay.LightAttack.canceled += _ =>
            {
                OnBasicAttackCanceled?.Invoke();
                _heldActions.Remove((int)InputCommand.BasicAttack);
            };
            _input.GamePlay.LightAttackHeld.performed += _ =>
            {
                OnBasicAttackHeld?.Invoke();
                _heldActions.Add((int)InputCommand.BasicAttack);
            };
            // 特殊技
            _input.GamePlay.SpecialSkill.started += _ => OnSpecialAttackStarted?.Invoke();
            _input.GamePlay.SpecialSkill.performed += _ => OnSpecialAttackPerformed?.Invoke();
            _input.GamePlay.SpecialSkill.canceled += _ =>
            {
                OnSpecialAttackCanceled?.Invoke();
                _heldActions.Remove((int)InputCommand.SpecialAttack);
            };
            _input.GamePlay.SpecialSkillHeld.performed += _ =>
            {
                OnSpecialAttackHeld?.Invoke();
                _heldActions.Add((int)InputCommand.SpecialAttack);
            };
            // 
            _input.GamePlay.Ultimate.started += _ => OnUltimateStarted?.Invoke();
            _input.GamePlay.Interact.started += _ => OnGameplayInteractStarted?.Invoke();
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
