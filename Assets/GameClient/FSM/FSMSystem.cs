using System.Collections.Generic;
using UnityEngine;

namespace Game.FSM
{
    /// <summary>
    /// FSM 状态机系统泛型核心
    /// T 为该状态机的所有者类型（Owner）
    /// </summary>
    public class FSMSystem<T>
    {
        public T Owner { get; private set; }
        
        private IFSMState<T> _currentState;
        public IFSMState<T> CurrentState => _currentState;
        private readonly Dictionary<System.Type, IFSMState<T>> _stateCache = new Dictionary<System.Type, IFSMState<T>>();

        // 内部构造，外部由 FSMManager 创建
        internal FSMSystem(T owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 预先注册一个状态到状态机中
        /// </summary>
        public void AddState(IFSMState<T> state)
        {
            var type = state.GetType();
            if (!_stateCache.ContainsKey(type))
            {
                state.OnInit(this);
                _stateCache.Add(type, state);
            }
        }

        private bool _isPaused = false;

        /// <summary>
        /// 挂起状态机（用于被上层的 Skill 或 Ability 强行接管时冻结底层移动）
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 解除挂起
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// 切换状态 (附带准入准出协商)
        /// </summary>
        public bool ChangeState<TState>() where TState : IFSMState<T>
        {
            if (_isPaused) return false; // 冻结期间不允许内部连线切状态
            
            var type = typeof(TState);
            if (!_stateCache.TryGetValue(type, out var nextState))
            {
                Debug.LogError($"[FSM] 状态切换失败：未注册状态 {type.Name}");
                return false;
            }

            // --- 状态准入/准出双向协商 ---
            if (_currentState != null && !_currentState.CanExit()) return false;
            if (!nextState.CanEnter()) return false;

            _currentState?.OnExit();
            _currentState = nextState;
            _currentState?.OnEnter();

            return true;
        }

        /// <summary>
        /// 驱动状态机运行（由 FSMManager 统一调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_isPaused) return;
            _currentState?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 物理帧驱动
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (_isPaused) return;
            _currentState?.OnFixedUpdate(fixedDeltaTime);
        }

        public void Destroy()
        {
            _currentState?.OnExit();
            foreach (var state in _stateCache.Values)
            {
                state.OnDestroy();
            }
            _stateCache.Clear();
            _currentState = null;
            Owner = default;
        }
    }
}
