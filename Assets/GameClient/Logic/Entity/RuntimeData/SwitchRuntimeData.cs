namespace Game.Logic
{
    public class SwitchRuntimeData : IEntityRuntimeData
    {
        public bool IsSwitchOutPending { get; set; }

        public void Reset()
        {
            IsSwitchOutPending = false;
        }
    }
}
