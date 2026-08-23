using System;
using Game.Input;

namespace Game.Logic
{
    public sealed class CharacterInputEventAdapter
    {
        private readonly Func<IActionCommandHandler> _handlerProvider;
        private IInputProvider _provider;

        public CharacterInputEventAdapter(Func<IActionCommandHandler> handlerProvider)
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

        private IActionCommandHandler CurrentHandler =>
            _handlerProvider?.Invoke() ?? CharacterStateBase.InputHandlerStatic;

        private void Dispatch(HardwareInputType commandType, CommandPhase phase)
        {
            CurrentHandler.Handle(CharacterCommandFactory.Create(commandType, phase, _provider));
        }

        private void HandleSwitchStarted() => Dispatch(HardwareInputType.Switch, CommandPhase.Started);
        private void HandleSwitchPre() => Dispatch(HardwareInputType.Switch, CommandPhase.Canceled); // Or some other phase if appropriate

        private void HandleMoveStarted() => Dispatch(HardwareInputType.Move, CommandPhase.Started);
        private void HandleMovePerformed() => Dispatch(HardwareInputType.Move, CommandPhase.Performed);
        private void HandleMoveCanceled() => Dispatch(HardwareInputType.Move, CommandPhase.Canceled);
        private void HandleMoveHeld() => Dispatch(HardwareInputType.Move, CommandPhase.Held);

        private void HandleBasicAttackStarted() => Dispatch(HardwareInputType.BasicAttack, CommandPhase.Started);
        private void HandleBasicAttackPerformed() => Dispatch(HardwareInputType.BasicAttack, CommandPhase.Performed);
        private void HandleBasicAttackCanceled() => Dispatch(HardwareInputType.BasicAttack, CommandPhase.Canceled);
        private void HandleBasicAttackHeld() => Dispatch(HardwareInputType.BasicAttack, CommandPhase.Held);

        private void HandleSpecialAttackStarted() => Dispatch(HardwareInputType.SpecialAttack, CommandPhase.Started);
        private void HandleSpecialAttackPerformed() => Dispatch(HardwareInputType.SpecialAttack, CommandPhase.Performed);
        private void HandleSpecialAttackCanceled() => Dispatch(HardwareInputType.SpecialAttack, CommandPhase.Canceled);
        private void HandleSpecialAttackHeld() => Dispatch(HardwareInputType.SpecialAttack, CommandPhase.Held);

        private void HandleUltimate() => Dispatch(HardwareInputType.Ultimate, CommandPhase.Started);
        private void HandleGameplayInteract() => Dispatch(HardwareInputType.Interact, CommandPhase.Started);

        private void HandleEvadeStarted() => Dispatch(HardwareInputType.Evade, CommandPhase.Started);
        private void HandleEvadePerformed() => Dispatch(HardwareInputType.Evade, CommandPhase.Performed);
        private void HandleEvadeCanceled() => Dispatch(HardwareInputType.Evade, CommandPhase.Canceled);
        private void HandleEvadeHeld() => Dispatch(HardwareInputType.Evade, CommandPhase.Held);
    }
}
