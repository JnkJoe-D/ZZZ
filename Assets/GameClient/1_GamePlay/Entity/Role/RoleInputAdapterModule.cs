using System;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色输入适配与生命周期管理模块。
    /// 纯领域模块，管理 CharacterInputEventAdapter、输入源防抖绑定与切换。
    /// </summary>
    public class RoleInputAdapterModule : IEntityModule
    {
        private RoleEntity _role;
        private CharacterInputEventAdapter _inputEventAdapter;
        private IInputProvider _boundInputProvider;
        private IInputProvider _fallbackInputProvider;
        private RoleTeamContext _teamContext;
        private bool _isInputBound;

        public bool IsInputBound => _isInputBound;
        public IInputProvider BoundInputProvider => _boundInputProvider;

        public IInputProvider EffectiveInputProvider => _teamContext?.InputProvider ?? _fallbackInputProvider;

        public bool IsSharedProvider(IInputProvider provider) =>
            provider != null &&
            _teamContext != null &&
            _teamContext.InputProvider != null &&
            ReferenceEquals(provider, _teamContext.InputProvider);

        public bool UsesSharedInputProvider => IsSharedProvider(EffectiveInputProvider);

        public void Initialize(CharacterEntity owner)
        {
            _role = (RoleEntity)owner;
            _inputEventAdapter = new CharacterInputEventAdapter(ResolveCurrentInputHandler);
        }

        private IActionCommandHandler ResolveCurrentInputHandler()
        {
            if (_role?.StateMachine?.CurrentState is CharacterStateBase charState && charState.InputHandler != null)
            {
                return charState.InputHandler;
            }
            return CharacterStateBase.InputHandlerStatic;
        }

        public void SetFallbackInputProvider(IInputProvider provider)
        {
            _fallbackInputProvider = provider;
            if (_isInputBound && _teamContext?.InputProvider == null)
            {
                Bind(provider);
            }
        }

        public void Bind(IInputProvider provider)
        {
            if (provider == null) return;

            if (_isInputBound)
            {
                if (ReferenceEquals(_boundInputProvider, provider)) return;

                _inputEventAdapter?.Unbind(_boundInputProvider);
                _isInputBound = false;
                _boundInputProvider = null;
            }

            _inputEventAdapter?.Bind(provider);
            _boundInputProvider = provider;
            _isInputBound = true;
        }

        public void Unbind()
        {
            if (!_isInputBound) return;

            _inputEventAdapter?.Unbind(_boundInputProvider);
            _boundInputProvider = null;
            _isInputBound = false;
        }

        public void UpdateTeamContext(RoleTeamContext context)
        {
            _teamContext = context;
            IInputProvider previousProvider = _boundInputProvider;
            bool wasBound = _isInputBound;
            if (wasBound)
            {
                Unbind();
            }

            IInputProvider newProvider = context?.InputProvider;
            if (!ReferenceEquals(previousProvider, newProvider))
            {
                DisableReplacedInputProvider(previousProvider, newProvider);
            }

            if (wasBound && newProvider != null)
            {
                Bind(newProvider);
            }
        }

        public void SetInputActive(bool active)
        {
            IInputProvider targetProvider = EffectiveInputProvider;
            if (active)
            {
                if (targetProvider is Behaviour inputBehaviour)
                {
                    inputBehaviour.enabled = true;
                }
                Bind(targetProvider);
            }
            else
            {
                bool isShared = IsSharedProvider(targetProvider);
                Unbind();
                if (!isShared && targetProvider is Behaviour inputBehaviour)
                {
                    inputBehaviour.enabled = false;
                }
            }
        }

        public void DisableReplacedInputProvider(IInputProvider previousProvider, IInputProvider currentProvider)
        {
            if (previousProvider == null || ReferenceEquals(previousProvider, currentProvider))
                return;

            // 队伍共享输入提供器生命周期由 TeamManager 统管，单个角色替换时严禁将其禁用
            if (IsSharedProvider(previousProvider))
                return;

            if (previousProvider is Behaviour previousBehaviour)
            {
                previousBehaviour.enabled = false;
            }
        }

        public void OnLogicTick(float logicDeltaTime) { }

        public void Dispose()
        {
            Unbind();
            _inputEventAdapter = null;
            _role = null;
        }
    }
}
