namespace Game.GamePlay
{
    public class ParryRuntimeData : IEntityRuntimeData
    {
        public bool IsParrying { get; set; }
        public bool ParrySucceeded { get; set; }
        public CharacterEntity LastParriedAttacker { get; set; }
        public ATEditor.ParryWeight LastParryWeight { get; set; } = ATEditor.ParryWeight.Heavy;
        public IParryClashHandler ClashHandler { get; set; }

        public void Reset()
        {
            IsParrying = false;
            ParrySucceeded = false;
            LastParriedAttacker = null;
            LastParryWeight = ATEditor.ParryWeight.Heavy;
            ClashHandler = null;
        }
    }
}
