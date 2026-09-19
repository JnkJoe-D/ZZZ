namespace Game.GamePlay
{
    public class SwitchRuntimeData : EntityRuntimeDataBase
    {
        public bool IsSwitchOutPending => Get<bool>(nameof(IsSwitchOutPending));
    }
}
