namespace Game.GamePlay
{
    public class ActionRuntimeData : EntityRuntimeDataBase
    {
        public ActionState TargetGroundSubState => Get<ActionState>(nameof(TargetGroundSubState), ActionState.Idle);
        public ActionConfigAsset NextActionToCast => Get<ActionConfigAsset>(nameof(NextActionToCast));
        public bool IsShortMoveInput => Get<bool>(nameof(IsShortMoveInput));
        public AttackWarningMarker MatchedWarningMarker => Get<AttackWarningMarker>(nameof(MatchedWarningMarker));

        public override void Reset()
        {
            base.Reset();
            Set(nameof(TargetGroundSubState), ActionState.Idle);
        }
    }
}
