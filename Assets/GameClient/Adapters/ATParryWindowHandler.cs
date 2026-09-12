using UnityEngine;
using ATEditor;
using Game.Logic;

namespace Game.Adapters
{
    public class ATParryWindowHandler : IParryWindowHandler
    {
        private readonly CharacterEntity _entity;
        private ParryClashContract _currentContract;

        public ATParryWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void OnParryWindowEnter()
        {
            if (_entity != null && _entity.DataModule != null)
            {
                var parryData = _entity.DataModule.Get<ParryRuntimeData>();
                if (parryData != null)
                {
                    parryData.IsParrying = true;
                }
            }

            // 1. 优先检查并接管已有生效中的契约（例如由 Start 动作延续至 H 动作）
            var existingContract = CombatWarningManager.GetActiveContractByRole(_entity);
            if (existingContract != null && existingContract.IsValid)
            {
                _currentContract = existingContract;
                _currentContract.ExpireTime = Time.time + 5.0f; // 刷新有效期
                return;
            }

            // 2. 首次进入或无延续契约时，结合预警上下文进行校验并建立契约
            AttackWarningMarker marker = null;
            var actionData = _entity?.DataModule?.Get<ActionRuntimeData>();
            if (actionData != null && actionData.MatchedWarningMarker != null)
            {
                marker = actionData.MatchedWarningMarker;
            }
            else if (_entity != null)
            {
                marker = CombatWarningManager.GetValidWarning(_entity, WarningSignalType.Yellow_Parryable);
            }

            if (marker != null && marker.Attacker != null && marker.Attacker.gameObject.activeInHierarchy)
            {
                _currentContract = new ParryClashContract
                {
                    Attacker = marker.Attacker,
                    ParryRole = _entity,
                    Marker = marker,
                    ExpireTime = Time.time + 5.0f,
                    IsResolved = false
                };
                CombatWarningManager.RegisterContract(_currentContract);
            }
        }

        public void OnParryWindowExit(bool isInterrupted)
        {
            if (_entity != null && _entity.DataModule != null)
            {
                var parryData = _entity.DataModule.Get<ParryRuntimeData>();
                if (parryData != null)
                {
                    parryData.IsParrying = false;
                }
            }

            // 核心保护：打断时不注销契约，保持多段连招交接保护；仅在自然结束时注销契约！
            if (!isInterrupted)
            {
                if (_currentContract != null)
                {
                    CombatWarningManager.UnregisterContract(_currentContract);
                    _currentContract = null;
                }
                if (_entity != null)
                {
                    CombatWarningManager.UnregisterContractsByRole(_entity);
                }
            }
        }

        public void SetParryWindowActive(bool active)
        {
            if (active)
            {
                OnParryWindowEnter();
            }
            else
            {
                OnParryWindowExit(isInterrupted: false);
            }
        }
    }
}
