using Game.Logic;

namespace Game.Logic
{
    public interface IActionCommandHandler
    {
        void Handle(CharacterCommand command);
    }

    public class NullInputCommandHandler : IActionCommandHandler
    {
        public void Handle(CharacterCommand command) { }
    }

    public abstract class ForwardingInputCommandHandler : IActionCommandHandler
    {
        protected readonly RoleEntity Entity;

        protected ForwardingInputCommandHandler(RoleEntity entity)
        {
            Entity = entity;
        }

        protected virtual void Forward(CharacterCommand command)
        {
            Entity?.ActionController?.OnInput(command);
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
        public DashInputCommandHandler(RoleEntity entity) : base(entity) { }
    }

    public sealed class ComboInputCommandHandler : ForwardingInputCommandHandler
    {
        public ComboInputCommandHandler(RoleEntity entity) : base(entity) { }
    }

    public sealed class DefaultInputCommandHandler : ForwardingInputCommandHandler
    {
        public DefaultInputCommandHandler(RoleEntity entity) : base(entity) { }
    }
}
