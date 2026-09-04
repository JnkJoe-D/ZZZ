namespace Game.Logic
{
    public class ParryRuntimeData : IEntityRuntimeData
    {
        public bool IsParrying { get; set; }
        public bool ParrySucceeded { get; set; }
        public CharacterEntity LastParriedAttacker { get; set; }
        public ATEditor.AttackWeight LastParriedWeight { get; set; }

        public void Reset()
        {
            IsParrying = false;
            ParrySucceeded = false;
            LastParriedAttacker = null;
        }
    }
}
