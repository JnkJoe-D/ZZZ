using UnityEngine;
using Game.Framework;

namespace Game.GamePlay
{
    /// <summary>
    /// 怪物大脑与决策协同器。
    /// 纯领域业务模块，统筹怪物的存活状态、受击硬直与失衡槽，动态求值自控力并驱动行为树黑板。
    /// </summary>
    public class MonsterBrainCoordinator : IEntityModule
    {
        private MonsterEntity _monster;
        private HitReactionRuntimeData _hitData;
        private bool _lastSelfControlState = true;

        public bool IsSelfControl
        {
            get
            {
                if (_monster == null) return false;

                // 1. 死亡无自控
                if (_monster.LifecycleComponent != null && _monster.LifecycleComponent.IsDead) return false;

                // 2. 受击硬直中无自控
                if (_hitData != null && _hitData.InHitReaction) return false;

                // 3. 失衡倒地无自控
                var attrs = _monster.StatusModule?.Attributes;
                if (attrs != null && attrs.Has(AttributeId.Daze))
                {
                    float daze = attrs.GetCurrent(AttributeId.Daze);
                    float maxDaze = attrs.GetCurrent(AttributeId.MaxDaze);
                    if (maxDaze > 0f && daze >= maxDaze) return false;
                }

                return true;
            }
        }

        public void Initialize(CharacterEntity owner)
        {
            _monster = (MonsterEntity)owner;
            _hitData = _monster.DataModule?.Get<HitReactionRuntimeData>();

            if (_hitData != null)
            {
                _hitData.OnValueChanged -= HandleHitDataChanged;
                _hitData.OnValueChanged += HandleHitDataChanged;
            }

            if (_monster.StatusModule?.Attributes != null)
            {
                _monster.StatusModule.Attributes.OnAttributeChanged -= HandleAttributeChanged;
                _monster.StatusModule.Attributes.OnAttributeChanged += HandleAttributeChanged;
            }

            _lastSelfControlState = IsSelfControl;
        }

        private void HandleHitDataChanged(string key, object oldValue, object newValue)
        {
            if (key == nameof(HitReactionRuntimeData.InHitReaction))
            {
                SyncBlackboardSelfControl();
            }
        }

        private void HandleAttributeChanged(AttributeId id, float oldValue, float newValue)
        {
            if (id == AttributeId.Daze || id == AttributeId.MaxDaze)
            {
                SyncBlackboardSelfControl();
            }
        }

        public void SyncBlackboardSelfControl()
        {
            if (_monster?.BTRunner?.RuntimeBlackboard != null)
            {
                bool selfControl = IsSelfControl;
                _lastSelfControlState = selfControl;
                _monster.BTRunner.RuntimeBlackboard.Set(BBKeyMapper.GetString(BBKey.IsSelfControl), selfControl);
            }
        }

        public void LogicTick(float logicDeltaTime)
        {
            bool current = IsSelfControl;
            if (current != _lastSelfControlState)
            {
                _lastSelfControlState = current;
                SyncBlackboardSelfControl();
            }
        }

        public void Dispose()
        {
            if (_hitData != null)
            {
                _hitData.OnValueChanged -= HandleHitDataChanged;
                _hitData = null;
            }

            if (_monster?.StatusModule?.Attributes != null)
            {
                _monster.StatusModule.Attributes.OnAttributeChanged -= HandleAttributeChanged;
            }

            _monster = null;
        }
    }
}
