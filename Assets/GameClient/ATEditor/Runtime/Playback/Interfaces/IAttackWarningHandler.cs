namespace ATEditor
{

    public interface IAttackWarningHandler
    {
        void RegisterWarningMarker(AttackWarningClip clip);
        void RegisterWarningMarker(WarningSignalType signalType, float detectionRadius, float detectionAngle);
        void UnregisterWarningMarker();
    }
}