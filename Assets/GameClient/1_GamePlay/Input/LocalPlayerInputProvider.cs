using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Input;
namespace Game.GamePlay
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
        public event Action OnMovementZero;
        public event Action OnRawMovementZero;

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
        private Vector2 _lastRawMoveInput;
        private Vector2 _smoothedMoveInput;
        private Vector2 _smoothMoveVelocity;
        private RoleConfigAsset _cachedRoleConfig;
        private readonly HashSet<int> _heldActions = new();

        /// <summary>
        /// 绑定当前出场角色的配置，用于动态获取手感衰减阻尼参数
        /// </summary>
        public void SetRoleConfig(RoleConfigAsset config)
        {
            _cachedRoleConfig = config;
        }

        public bool IsHeld(int actionKey)
        {
            if (actionKey == (int)HardwareInputType.Move)
            {
                return HasMoveInput();
            }

            if (_input != null)
            {
                switch ((HardwareInputType)actionKey)
                {
                    case HardwareInputType.BasicAttack:
                        return _input.GamePlay.LightAttack.IsPressed() || _heldActions.Contains(actionKey);
                    case HardwareInputType.SpecialAttack:
                        return _input.GamePlay.SpecialSkill.IsPressed() || _heldActions.Contains(actionKey);
                    case HardwareInputType.Evade:
                        return _input.GamePlay.Evade.IsPressed() || _heldActions.Contains(actionKey);
                    case HardwareInputType.Ultimate:
                        return _input.GamePlay.Ultimate.IsPressed() || _heldActions.Contains(actionKey);
                    case HardwareInputType.Interact:
                        return _input.GamePlay.Interact.IsPressed() || _heldActions.Contains(actionKey);
                    case HardwareInputType.Switch:
                        return _input.GamePlay.SwitchNext.IsPressed() || _input.GamePlay.SwitchPre.IsPressed() || _heldActions.Contains(actionKey);
                }
            }

            return _heldActions.Contains(actionKey);
        }

        public void SetHeld(int actionKey, bool held)
        {
            if (held) _heldActions.Add(actionKey);
            else _heldActions.Remove(actionKey);
        }

        private void Awake()
        {
            _input = new PlayerControl();

            // 订阅瞬发与持续事件（并在事件回调触发的第一瞬间立即更新输入向量，保证后续路由校验即时命中）
            _input.GamePlay.Move.started += ctx =>
            {
                Vector2 val = ctx.ReadValue<Vector2>();
                if (val.sqrMagnitude > 0.01f)
                {
                    _currentMoveInput = val;
                    _smoothedMoveInput = val;
                    _smoothMoveVelocity = Vector2.zero;
                    _lastRawMoveInput = val;
                }
                OnMoveStarted?.Invoke();
            };

            _input.GamePlay.Move.performed += ctx =>
            {
                Vector2 val = ctx.ReadValue<Vector2>();
                _currentMoveInput = val;
                if (val.sqrMagnitude > 0.01f)
                {
                    _smoothedMoveInput = val;
                    _smoothMoveVelocity = Vector2.zero;
                    _lastRawMoveInput = val;
                }
                OnMovePerformed?.Invoke();
            };

            _input.GamePlay.Move.canceled += _ =>
            {
                OnMoveCanceled?.Invoke();
                _heldActions.Remove((int)HardwareInputType.Move);
                _currentMoveInput = Vector2.zero;
                if (_lastRawMoveInput.sqrMagnitude > 0.01f)
                {
                    _lastRawMoveInput = Vector2.zero;
                    OnRawMovementZero?.Invoke();
                }
            };
            _input.GamePlay.MoveHeld.performed += _ =>
            {
                if (HasMoveInput())
                {
                    OnMoveHeld?.Invoke();
                    _heldActions.Add((int)HardwareInputType.Move);
                }
            };
            _input.GamePlay.MoveHeld.canceled += _ =>
            {
                _heldActions.Remove((int)HardwareInputType.Move);
            };

            // 闪避
            _input.GamePlay.Evade.started += _ => OnEvadeStarted?.Invoke();
            _input.GamePlay.Evade.performed += _ => OnEvadePerformed?.Invoke();
            _input.GamePlay.Evade.canceled += _ =>
            {
                OnEvadeCanceled?.Invoke();
                _heldActions.Remove((int)HardwareInputType.Evade); 
            };
            _input.GamePlay.EvadeHeld.performed += _ =>
            {
                OnEvadeHeld?.Invoke();
                _heldActions.Add((int)HardwareInputType.Evade);
            };
            _input.GamePlay.EvadeHeld.canceled += _ =>
            {
                _heldActions.Remove((int)HardwareInputType.Evade);
            };

            // 普通攻击
            _input.GamePlay.LightAttack.started += _ => OnBasicAttackStarted?.Invoke();
            _input.GamePlay.LightAttack.performed += _ => OnBasicAttackPerformed?.Invoke();
            _input.GamePlay.LightAttack.canceled += _ =>
            {
                OnBasicAttackCanceled?.Invoke();
                _heldActions.Remove((int)HardwareInputType.BasicAttack);
            };
            _input.GamePlay.LightAttackHeld.performed += _ =>
            {
                OnBasicAttackHeld?.Invoke();
                _heldActions.Add((int)HardwareInputType.BasicAttack);
            };
            _input.GamePlay.LightAttackHeld.canceled += _ =>
            {
                _heldActions.Remove((int)HardwareInputType.BasicAttack);
            };

            // 特殊技
            _input.GamePlay.SpecialSkill.started += _ => OnSpecialAttackStarted?.Invoke();
            _input.GamePlay.SpecialSkill.performed += _ => OnSpecialAttackPerformed?.Invoke();
            _input.GamePlay.SpecialSkill.canceled += _ =>
            {
                OnSpecialAttackCanceled?.Invoke();
                _heldActions.Remove((int)HardwareInputType.SpecialAttack);
            };
            _input.GamePlay.SpecialSkillHeld.performed += _ =>
            {
                OnSpecialAttackHeld?.Invoke();
                _heldActions.Add((int)HardwareInputType.SpecialAttack);
            };
            _input.GamePlay.SpecialSkillHeld.canceled += _ =>
            {
                _heldActions.Remove((int)HardwareInputType.SpecialAttack);
            };

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
            _heldActions.Clear();
            _currentMoveInput = Vector2.zero;
            _lastRawMoveInput = Vector2.zero;
            _smoothedMoveInput = Vector2.zero;
            _smoothMoveVelocity = Vector2.zero;
            _input.Disable();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Disable();
                _input.Dispose();
                _input = null;
            }
        }

        private void Update()
        {
            // 1. 每帧获取物理原始输入
            _currentMoveInput = _input != null ? _input.GamePlay.Move.ReadValue<Vector2>() : Vector2.zero;

            // 2. 原始物理输入边沿检测 (从有到无瞬间立即触发 OnRawMovementZero，无阻尼)
            bool hadRawInput = _lastRawMoveInput.sqrMagnitude > 0.01f;
            bool hasRawInput = _currentMoveInput.sqrMagnitude > 0.01f;
            _lastRawMoveInput = _currentMoveInput;

            if (hadRawInput && !hasRawInput)
            {
                OnRawMovementZero?.Invoke();
            }

            // 3. 从 RoleConfigAsset 读取衰减阻尼时长（默认 0.08s）
            float decelDuration = _cachedRoleConfig != null ? _cachedRoleConfig.InputConfig.MoveInputDecelerationDuration : 0.08f;
            float dt = Time.unscaledDeltaTime;

            bool hadSmoothedInput = _smoothedMoveInput.sqrMagnitude > 0.001f;

            if (hasRawInput)
            {
                // 按下时：零延迟、无增量阻尼，立即 100% 响应物理输入
                _smoothedMoveInput = _currentMoveInput;
                _smoothMoveVelocity = Vector2.zero;
            }
            else if (hadSmoothedInput && decelDuration > 0f)
            {
                // 松开时：纯衰减阻尼保护（防止切换前后左右按键时的空窗期误停下，进而导致重新起步）
                _smoothedMoveInput = Vector2.SmoothDamp(_smoothedMoveInput, Vector2.zero, ref _smoothMoveVelocity, decelDuration, float.MaxValue, dt);
                if (_smoothedMoveInput.sqrMagnitude < 0.001f)
                {
                    _smoothedMoveInput = Vector2.zero;
                    _smoothMoveVelocity = Vector2.zero;
                    // 仅在衰减彻底归零时，才触发停下动作
                    OnMovementZero?.Invoke();
                }
            }
            else if (hadSmoothedInput)
            {
                // 无阻尼配置时立即归零并触发停下
                _smoothedMoveInput = Vector2.zero;
                _smoothMoveVelocity = Vector2.zero;
                OnMovementZero?.Invoke();
            }
        }

        // ==========================================
        // 实现 IInputProvider 接口
        // ==========================================
        
        public Vector2 GetMovementDirection()
        {
            return _smoothedMoveInput;
        }

        public Vector2 GetLastMovementDirection()
        {
            return _smoothedMoveInput;
        }

        public bool HasMoveInput()
        {
            return _smoothedMoveInput.sqrMagnitude > 0.001f;
        }
        public bool HasRawMoveInput()
        {
            return _currentMoveInput.sqrMagnitude > 0.001f;
        }
    }
}
