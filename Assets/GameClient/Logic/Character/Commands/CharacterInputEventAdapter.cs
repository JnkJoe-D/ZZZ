using System;
using Game.Input;

namespace Game.Logic
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
            provider.OnSwitchNext += HandleSwitchStarted;
            provider.OnSwitchPre += HandleSwitchPre;

            provider.OnMoveStarted += HandleMoveStarted;
            provider.OnMovePerformed += HandleMovePerformed;
            provider.OnMoveCanceled += HandleMoveCanceled;
            provider.OnMoveHeld += HandleMoveHeld;

            provider.OnBasicAttackStarted += HandleBasicAttackStarted;
            provider.OnBasicAttackPerformed += HandleBasicAttackPerformed;
            provider.OnBasicAttackCanceled += HandleBasicAttackCanceled;
            provider.OnBasicAttackHeld += HandleBasicAttackHeld;

            provider.OnSpecialAttackStarted += HandleSpecialAttackStarted;
            provider.OnSpecialAttackPerformed += HandleSpecialAttackPerformed;
            provider.OnSpecialAttackCanceled += HandleSpecialAttackCanceled;
            provider.OnSpecialAttackHeld += HandleSpecialAttackHeld;

            provider.OnUltimateStarted += HandleUltimate;
            provider.OnGameplayInteractStarted += HandleGameplayInteract;

            provider.OnEvadeStarted += HandleEvadeStarted;
            provider.OnEvadePerformed += HandleEvadePerformed;
            provider.OnEvadeCanceled += HandleEvadeCanceled;
            provider.OnEvadeHeld += HandleEvadeHeld;
        }

        public void Unbind(IInputProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            provider.OnSwitchNext -= HandleSwitchStarted;
            provider.OnSwitchPre -= HandleSwitchPre;

            provider.OnMoveStarted -= HandleMoveStarted;
            provider.OnMovePerformed -= HandleMovePerformed;
            provider.OnMoveCanceled -= HandleMoveCanceled;
            provider.OnMoveHeld -= HandleMoveHeld;

            provider.OnBasicAttackStarted -= HandleBasicAttackStarted;
            provider.OnBasicAttackPerformed -= HandleBasicAttackPerformed;
            provider.OnBasicAttackCanceled -= HandleBasicAttackCanceled;
            provider.OnBasicAttackHeld -= HandleBasicAttackHeld;

            provider.OnSpecialAttackStarted -= HandleSpecialAttackStarted;
            provider.OnSpecialAttackPerformed -= HandleSpecialAttackPerformed;
            provider.OnSpecialAttackCanceled -= HandleSpecialAttackCanceled;
            provider.OnSpecialAttackHeld -= HandleSpecialAttackHeld;

            provider.OnUltimateStarted -= HandleUltimate;
            provider.OnGameplayInteractStarted -= HandleGameplayInteract;

            provider.OnEvadeStarted -= HandleEvadeStarted;
            provider.OnEvadePerformed -= HandleEvadePerformed;
            provider.OnEvadeCanceled -= HandleEvadeCanceled;
            provider.OnEvadeHeld -= HandleEvadeHeld;

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

        private void HandleSwitchStarted() => Dispatch(InputCommand.Switch, CommandPhase.Started);
        private void HandleSwitchPre() => Dispatch(InputCommand.Switch, CommandPhase.Canceled); // Or some other phase if appropriate

        private void HandleMoveStarted() => Dispatch(InputCommand.Move, CommandPhase.Started);
        private void HandleMovePerformed() => Dispatch(InputCommand.Move, CommandPhase.Performed);
        private void HandleMoveCanceled() => Dispatch(InputCommand.Move, CommandPhase.Canceled);
        private void HandleMoveHeld() => Dispatch(InputCommand.Move, CommandPhase.Held);

        private void HandleBasicAttackStarted() => Dispatch(InputCommand.BasicAttack, CommandPhase.Started);
        private void HandleBasicAttackPerformed() => Dispatch(InputCommand.BasicAttack, CommandPhase.Performed);
        private void HandleBasicAttackCanceled() => Dispatch(InputCommand.BasicAttack, CommandPhase.Canceled);
        private void HandleBasicAttackHeld() => Dispatch(InputCommand.BasicAttack, CommandPhase.Held);

        private void HandleSpecialAttackStarted() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Started);
        private void HandleSpecialAttackPerformed() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Performed);
        private void HandleSpecialAttackCanceled() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Canceled);
        private void HandleSpecialAttackHeld() => Dispatch(InputCommand.SpecialAttack, CommandPhase.Held);

        private void HandleUltimate() => Dispatch(InputCommand.Ultimate, CommandPhase.Started);
        private void HandleGameplayInteract() => Dispatch(InputCommand.Interact, CommandPhase.Started);

        private void HandleEvadeStarted() => Dispatch(InputCommand.Evade, CommandPhase.Started);
        private void HandleEvadePerformed() => Dispatch(InputCommand.Evade, CommandPhase.Performed);
        private void HandleEvadeCanceled() => Dispatch(InputCommand.Evade, CommandPhase.Canceled);
        private void HandleEvadeHeld() => Dispatch(InputCommand.Evade, CommandPhase.Held);
    }
}
