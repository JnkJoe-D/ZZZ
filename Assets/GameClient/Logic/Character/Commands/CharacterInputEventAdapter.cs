using System;
using Game.Input;

namespace Game.Logic.Character
{
    public sealed class CharacterInputEventAdapter
    {
        private readonly Func<IInputCommandHandler> _handlerProvider;
        private IInputProvider _provider;

        public CharacterInputEventAdapter(Func<IInputCommandHandler> handlerProvider)
        {
            _handlerProvider = handlerProvider;
        }

        public void Bind(IInputProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            _provider = provider;
            provider.OnBasicAttackStarted += HandleBasicAttackStarted;
            provider.OnBasicAttackCanceled += HandleBasicAttackCanceled;
            provider.OnBasicAttackHoldStart += HandleBasicAttackHoldStart;
            provider.OnBasicAttackHold += HandleBasicAttackHold;
            provider.OnBasicAttackHoldCancel += HandleBasicAttackHoldCancel;
            provider.OnSpecialAttack += HandleSpecialAttack;
            provider.OnSpecialAttackHoldStart += HandleSpecialAttackHoldStart;
            provider.OnSpecialAttackHold += HandleSpecialAttackHold;
            provider.OnSpecialAttackHoldCancel += HandleSpecialAttackHoldCancel;
            provider.OnUltimate += HandleUltimate;
            provider.OnEvadeFrontStarted += HandleEvadeFront;
            provider.OnEvadeBackStarted += HandleEvadeBack;
        }

        public void Unbind(IInputProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            provider.OnBasicAttackStarted -= HandleBasicAttackStarted;
            provider.OnBasicAttackCanceled -= HandleBasicAttackCanceled;
            provider.OnBasicAttackHoldStart -= HandleBasicAttackHoldStart;
            provider.OnBasicAttackHold -= HandleBasicAttackHold;
            provider.OnBasicAttackHoldCancel -= HandleBasicAttackHoldCancel;
            provider.OnSpecialAttack -= HandleSpecialAttack;
            provider.OnSpecialAttackHoldStart -= HandleSpecialAttackHoldStart;
            provider.OnSpecialAttackHold -= HandleSpecialAttackHold;
            provider.OnSpecialAttackHoldCancel -= HandleSpecialAttackHoldCancel;
            provider.OnUltimate -= HandleUltimate;
            provider.OnEvadeFrontStarted -= HandleEvadeFront;
            provider.OnEvadeBackStarted -= HandleEvadeBack;

            if (ReferenceEquals(_provider, provider))
            {
                _provider = null;
            }
        }

        private IInputCommandHandler CurrentHandler =>
            _handlerProvider?.Invoke() ?? CharacterStateBase.InputHandlerStatic;

        private void Dispatch(CommandType commandType, CommandPhase phase)
        {
            CurrentHandler.Handle(CharacterCommandFactory.Create(commandType, phase, _provider));
        }

        private void HandleBasicAttackStarted() => Dispatch(CommandType.BasicAttack, CommandPhase.Started);
        private void HandleBasicAttackCanceled() => Dispatch(CommandType.BasicAttack, CommandPhase.Canceled);
        private void HandleBasicAttackHoldStart() { }
        private void HandleBasicAttackHold() => Dispatch(CommandType.BasicAttack, CommandPhase.Performed);
        private void HandleBasicAttackHoldCancel() => Dispatch(CommandType.BasicAttack, CommandPhase.Canceled);
        private void HandleSpecialAttack() => Dispatch(CommandType.SpecialAttack, CommandPhase.Started);
        private void HandleSpecialAttackHoldStart() { }
        private void HandleSpecialAttackHold() => Dispatch(CommandType.SpecialAttack, CommandPhase.Performed);
        private void HandleSpecialAttackHoldCancel() => Dispatch(CommandType.SpecialAttack, CommandPhase.Canceled);
        private void HandleUltimate() => Dispatch(CommandType.Ultimate, CommandPhase.Started);
        private void HandleEvadeFront() => Dispatch(CommandType.Evade, CommandPhase.Started);
        private void HandleEvadeBack() => Dispatch(CommandType.Evade, CommandPhase.Started);
    }
}
