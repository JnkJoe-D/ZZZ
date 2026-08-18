namespace Game.Logic
{
    public class SwitchRuntimeData
    {
        public bool IsSwitchOutPending { get; set; }

        public void Reset()
        {
            IsSwitchOutPending = false;
        }
    }
}
