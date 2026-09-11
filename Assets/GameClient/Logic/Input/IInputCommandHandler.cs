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

    /// <summary>
    /// 标准动作指令处理器：直接将指令转发至角色的 ActionController 处理
    /// </summary>
    public class DefaultActionCommandHandler : IActionCommandHandler
    {
        protected readonly RoleEntity Entity;

        public DefaultActionCommandHandler(RoleEntity entity)
        {
            Entity = entity;
        }

        protected virtual void Forward(CharacterCommand command)
        {
            Entity?.ActionController?.OnInput(command);
        }

        public virtual void Handle(CharacterCommand command)
        {
            if (command == null) return;
            Forward(command);
        }
    }

    // ──────────────── 向后兼容过渡类 ────────────────
    public abstract class ForwardingInputCommandHandler : DefaultActionCommandHandler
    {
        protected ForwardingInputCommandHandler(RoleEntity entity) : base(entity) { }
    }

    public sealed class DashInputCommandHandler : DefaultActionCommandHandler
    {
        public DashInputCommandHandler(RoleEntity entity) : base(entity) { }
    }

    public sealed class ComboInputCommandHandler : DefaultActionCommandHandler
    {
        public ComboInputCommandHandler(RoleEntity entity) : base(entity) { }
    }

    public sealed class DefaultInputCommandHandler : DefaultActionCommandHandler
    {
        public DefaultInputCommandHandler(RoleEntity entity) : base(entity) { }
    }
}
