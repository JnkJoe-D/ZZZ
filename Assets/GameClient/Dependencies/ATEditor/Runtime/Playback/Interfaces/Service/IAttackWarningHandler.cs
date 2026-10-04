namespace ATEditor
{

    public interface IAttackWarningHandler : IService
    {
        void RegisterWarningMarker(AttackWarningClip clip);
        void RegisterWarningMarker(AttackWarningClip clip, float expectedHitTime);
        void RegisterWarningMarker(WarningSignalType signalType, float detectionRadius, float detectionAngle);
        void UnregisterWarningMarker();
    }
}