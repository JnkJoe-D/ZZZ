namespace Game.GamePlay
{
    public class ParryRuntimeData : EntityRuntimeDataBase
    {
        public bool IsParrying => Get<bool>(nameof(IsParrying));
        public bool ParrySucceeded => Get<bool>(nameof(ParrySucceeded));
        public CharacterEntity LastParriedAttacker => Get<CharacterEntity>(nameof(LastParriedAttacker));
        public ATEditor.ParryWeight LastParryWeight => Get<ATEditor.ParryWeight>(nameof(LastParryWeight), ATEditor.ParryWeight.Heavy);
        public IParryClashHandler ClashHandler => Get<IParryClashHandler>(nameof(ClashHandler));

        public override void Reset()
        {
            base.Reset();
            Set(nameof(LastParryWeight), ATEditor.ParryWeight.Heavy);
        }
    }
}
