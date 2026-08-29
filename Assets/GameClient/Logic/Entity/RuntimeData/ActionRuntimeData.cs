using Game.Logic;

namespace Game.Logic
{
    public class ActionRuntimeData : IEntityRuntimeData
    {
        public ActionState TargetGroundSubState { get; set; } = ActionState.Idle;
        public ActionConfigAsset NextActionToCast { get; set; }
        public bool IsShortMoveInput { get; set; }
        
        public AttackWarningMarker MatchedWarningMarker { get; set; }

        public void Reset()
        {
            TargetGroundSubState = ActionState.Idle;
            NextActionToCast = null;
            IsShortMoveInput = false;
            MatchedWarningMarker = null;
        }
    }
}
