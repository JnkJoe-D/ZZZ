using Game.Logic.Action.Combo;

namespace Game.Logic.Character
{
    public interface IInputCommandHandler
    {
        void Handle(CharacterCommand command);
    }

    public class NullInputCommandHandler : IInputCommandHandler
    {
        public void Handle(CharacterCommand command) { }
    }

    public abstract class ForwardingInputCommandHandler : IInputCommandHandler
    {
        protected readonly CharacterEntity Entity;

        protected ForwardingInputCommandHandler(CharacterEntity entity)
        {
            Entity = entity;
        }

        protected virtual void Forward(CharacterCommand command)
        {
            Entity?.ComboController?.OnInput(command);
        }

        public virtual void Handle(CharacterCommand command)
        {
            if (command == null)
            {
                return;
            }

            Forward(command);
        }
    }

    public sealed class DashInputCommandHandler : ForwardingInputCommandHandler
    {
        public DashInputCommandHandler(CharacterEntity entity) : base(entity) { }
    }

    public sealed class ComboInputCommandHandler : ForwardingInputCommandHandler
    {
        public ComboInputCommandHandler(CharacterEntity entity) : base(entity) { }
    }

    public sealed class DefaultInputCommandHandler : ForwardingInputCommandHandler
    {
        public DefaultInputCommandHandler(CharacterEntity entity) : base(entity) { }
    }
}
