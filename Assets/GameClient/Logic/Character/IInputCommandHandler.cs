using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 指令处理器接口：定义状态如何响应具体的输入动作
    /// </summary>
    public interface IInputCommandHandler
    {
        void OnBasicAttackStarted();
        void OnBasicAttackCanceled();
        void OnBasicAttackHoldStart();
        void OnBasicAttackHold();
        void OnBasicAttackHoldCancel();
        void OnSpecialAttack();
        void OnUltimate();
        void OnEvadeFront();
        void OnEvadeBack();
    }

    /// <summary>
    /// 空处理器：不做任何响应（用于受击、死亡、过场等）
    /// </summary>
    public class NullInputCommandHandler : IInputCommandHandler
    {
        public void OnBasicAttackStarted() { }
        public void OnBasicAttackCanceled() { }
        public void OnBasicAttackHoldStart() { }
        public void OnBasicAttackHold() { }
        public void OnBasicAttackHoldCancel() { }
        public void OnSpecialAttack() { }
        public void OnUltimate() { }
        public void OnEvadeFront() { }
        public void OnEvadeBack() { }
    }

    /// <summary>
    /// 冲刺状态处理器：
    /// </summary>
    public class DashInputCommandHandler : IInputCommandHandler
    {
        private CharacterEntity _entity;
        public DashInputCommandHandler(CharacterEntity entity) => _entity = entity;

        public void OnBasicAttackStarted()
        {
            if (_entity.Config == null) return;
            if (_entity.Config.dashAttack != null)
            {
                _entity.NextActionToCast = _entity.Config.dashAttack;
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnBasicAttackCanceled() { }
        public void OnBasicAttackHoldStart() { }
        public void OnBasicAttackHold() { }
        public void OnBasicAttackHoldCancel() { }

        public void OnSpecialAttack()
        {
            if (_entity.Config != null && _entity.Config.specialSkill != null)
            {
                _entity.NextActionToCast = _entity.Config.specialSkill;
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnUltimate()
        {
            if (_entity.Config != null && _entity.Config.Ultimate != null)
            {
                _entity.NextActionToCast = _entity.Config.Ultimate;
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnEvadeFront()
        {
            if (!_entity.CanEvade()) return;
            _entity.NextActionToCast = _entity.Config.evadeFront[0];
            _entity.StateMachine.ChangeState<CharacterEvadeState>();
        }

        public void OnEvadeBack()
        {
            if (!_entity.CanEvade()) return;
            _entity.NextActionToCast = _entity.Config.evadeBack[0];
            _entity.StateMachine.ChangeState<CharacterEvadeState>();
        }
    }

    /// <summary>
    /// 连招处理器：转发给 ComboController
    /// </summary>
    public class ComboInputCommandHandler : IInputCommandHandler
    {
        private CharacterEntity _entity;
        public ComboInputCommandHandler(CharacterEntity entity) => _entity = entity;

        public void OnBasicAttackStarted() => _entity.ComboController.OnInput(BufferedInputType.BasicAttack);
        public void OnBasicAttackCanceled() { }
        public void OnBasicAttackHoldStart() { }
        public void OnBasicAttackHold() => _entity.ComboController.OnInput(BufferedInputType.BasicAttackHold);
        public void OnBasicAttackHoldCancel() { }
        public void OnSpecialAttack() => _entity.ComboController.OnInput(BufferedInputType.SpecialAttack);
        public void OnUltimate() => _entity.ComboController.OnInput(BufferedInputType.Ultimate);
        public void OnEvadeFront() => _entity.ComboController.OnInput(BufferedInputType.EvadeFront);
        public void OnEvadeBack() => _entity.ComboController.OnInput(BufferedInputType.EvadeBack);
    }
    public class DefaultInputCommandHandler : IInputCommandHandler
    {
        private CharacterEntity _entity;
        public DefaultInputCommandHandler(CharacterEntity entity) => _entity = entity;

        public void OnBasicAttackStarted()
        {
            if (_entity.Config == null) return;
            if (_entity.Config.lightAttacks != null && _entity.Config.lightAttacks.Length > 0)
            {
                _entity.NextActionToCast = _entity.Config.lightAttacks[0];
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnBasicAttackCanceled() { }
        public void OnBasicAttackHoldStart() { }
        public void OnBasicAttackHold() { }
        public void OnBasicAttackHoldCancel() { }

        public void OnSpecialAttack()
        {
            if (_entity.Config != null && _entity.Config.specialSkill != null)
            {
                _entity.NextActionToCast = _entity.Config.specialSkill;
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnUltimate()
        {
            if (_entity.Config != null && _entity.Config.Ultimate != null)
            {
                _entity.NextActionToCast = _entity.Config.Ultimate;
                _entity.StateMachine.ChangeState<CharacterSkillState>();
            }
        }

        public void OnEvadeFront()
        {
            if (!_entity.CanEvade()) return;
            _entity.NextActionToCast = _entity.Config.evadeFront[0];
            _entity.StateMachine.ChangeState<CharacterEvadeState>();
        }

        public void OnEvadeBack()
        {
            if (!_entity.CanEvade()) return;
            _entity.NextActionToCast = _entity.Config.evadeBack[0];
            _entity.StateMachine.ChangeState<CharacterEvadeState>();
        }
    }
}
