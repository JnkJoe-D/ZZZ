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
            provider.OnMoveStarted += HandleMoveStarted;
            provider.OnMovePerformed += HandleMovePerformed;
            provider.OnMoveCanceled += HandleMoveCanceled;
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
            provider.OnEvadeHold += HandleEvadeHold;
            provider.OnEvadeHoldCancel += HandleEvadeHoldCancel;
        }

        public void Unbind(IInputProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            provider.OnMoveStarted -= HandleMoveStarted;
            provider.OnMovePerformed -= HandleMovePerformed;
            provider.OnMoveCanceled -= HandleMoveCanceled;
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
            provider.OnEvadeHold -= HandleEvadeHold;
            provider.OnEvadeHoldCancel -= HandleEvadeHoldCancel;

            if (ReferenceEquals(_provider, provider))
            {
                _provider = null;
            }
        }

        private IInputCommandHandler CurrentHandler =>
            _handlerProvider?.Invoke() ?? CharacterStateBase.InputHandlerStatic;

        private void Dispatch(InputCommand commandType, CommandPhase phase)
        {
            CurrentHandler.Handle(CharacterCommandFactory.Create(commandType, phase, _provider));
        }

        private void HandleMoveStarted() => Dispatch(InputCommand.Move, CommandPhase.Started);
        private void HandleMovePerformed() => Dispatch(InputCommand.Move, CommandPhase.Performed);
        private void HandleMoveCanceled() => Dispatch(InputCommand.Move, CommandPhase.Canceled);

        private void HandleBasicAttackStarted() => Dispatch(InputCommand.BasicAttack, CommandPhase.Started);
        private void HandleBasicAttackCanceled() => Dispatch(InputCommand.BasicAttack, CommandPhase.Canceled);
        private void HandleBasicAttackHoldStart() { }
        private void HandleBasicAttackHold() => Dispatch(InputCommand.BasicAttack, CommandPhase.Performed);
        private void HandleBasicAttackHoldCancel() => Dispatch(InputCommand.BasicAttack, CommandPhase.Canceled);
        private void HandleSpecialAttack() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Started);
        private void HandleSpecialAttackHoldStart() { }
        private void HandleSpecialAttackHold() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Performed);
        private void HandleSpecialAttackHoldCancel() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Canceled);
        private void HandleUltimate() => Dispatch(InputCommand.Ultimate, CommandPhase.Started);
        private void HandleEvadeFront() => Dispatch(InputCommand.Evade, CommandPhase.Started);
        private void HandleEvadeBack() => Dispatch(InputCommand.Evade, CommandPhase.Started);
        private void HandleEvadeHold() => Dispatch(InputCommand.Evade, CommandPhase.Performed);
        private void HandleEvadeHoldCancel() => Dispatch(InputCommand.Evade, CommandPhase.Canceled);
    }
}
