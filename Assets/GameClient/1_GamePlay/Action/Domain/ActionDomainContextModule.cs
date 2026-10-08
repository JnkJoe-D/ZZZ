using System;
using System.Collections.Generic;

namespace Game.GamePlay
{
    /// <summary>
    /// 实体动作领域上下文模块（纯领域模块，零 MonoBehaviour 依赖）。
    /// 负责管理实体的多态领域实例、驱动领域生命周期流转、自顶向下下发受控时钟更新。
    /// </summary>
    public class ActionDomainContextModule : IEntityModule
    {
        private CharacterEntity _entity;
        private readonly Dictionary<ActionDomainId, ActionDomain> _domains = new();
        private readonly PassiveActionDomain _passiveDomain = new();

        private ActionDomain _currentDomain;
        private ActionConfigAsset _currentAction;

        public CharacterEntity Entity => _entity;
        public ActionDomain CurrentDomain => _currentDomain ?? _passiveDomain;
        public ActionDomainId CurrentDomainId => CurrentDomain.DomainId;
        public ActionConfigAsset CurrentAction => _currentAction;

        public void Initialize(CharacterEntity owner)
        {
            _entity = owner;
            _passiveDomain.Initialize(owner, this);
            _currentDomain = _passiveDomain;
        }

        /// <summary>
        /// 注册领域实例（装配期调用）。
        /// </summary>
        public void RegisterDomain(ActionDomain domain)
        {
            if (domain == null) return;
            domain.Initialize(_entity, this);
            _domains[domain.DomainId] = domain;
        }

        /// <summary>
        /// 根据领域 ID 获取注册的领域实例。
        /// </summary>
        public T GetDomain<T>(ActionDomainId domainId) where T : ActionDomain
        {
            if (_domains.TryGetValue(domainId, out var domain) && domain is T typed)
            {
                return typed;
            }
            return null;
        }

        /// <summary>
        /// 响应动作播放成功事件，进行领域生命周期裁决与推进。
        /// </summary>
        public void HandleActionChanged(ActionConfigAsset newAction)
        {
            ActionConfigAsset previousAction = _currentAction;
            _currentAction = newAction;

            ActionDomainId targetDomainId = newAction != null ? newAction.DomainId : ActionDomainId.Locomotion;

            if (!_domains.TryGetValue(targetDomainId, out var targetDomain))
            {
                // 若未注册特定领域（如 Monster 未注册或未配置），使用 PassiveDomain
                targetDomain = _passiveDomain;
            }

            if (_currentDomain == targetDomain)
            {
                // 同领域内平滑切招（如 Combat 内 Attack01 -> Attack02）
                _currentDomain.OnActionChangedWithinDomain(previousAction, newAction);
            }
            else
            {
                // 跨领域生命周期切换
                var previousDomain = _currentDomain;
                previousDomain?.OnExit(targetDomain);

                _currentDomain = targetDomain;
                _currentDomain?.OnEnter(newAction, previousDomain);
            }
        }

        /// <summary>
        /// 受控自顶向下逻辑更新。
        /// </summary>
        public void LogicTick(float deltaTime)
        {
            _currentDomain?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 检查当前领域是否允许响应特定指令。
        /// </summary>
        public bool CanAcceptCommand(CharacterCommand command)
        {
            return CurrentDomain.CanAcceptCommand(command);
        }

        public void Dispose()
        {
            _currentDomain?.OnExit(null);
            _currentDomain = null;

            foreach (var kvp in _domains)
            {
                kvp.Value?.Dispose();
            }
            _domains.Clear();
            _passiveDomain.Dispose();
        }
    }
}
