namespace Game.GamePlay
{
    public class ActionRuntimeData : EntityRuntimeDataBase
    {
        public ActionConfigAsset NextActionToCast => Get<ActionConfigAsset>(nameof(NextActionToCast));
        public bool IsShortMoveInput => Get<bool>(nameof(IsShortMoveInput));
        public AttackWarningMarker MatchedWarningMarker => Get<AttackWarningMarker>(nameof(MatchedWarningMarker));

        public override void Reset()
        {
            base.Reset();
        }
    }
}
