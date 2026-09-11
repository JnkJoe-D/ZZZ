namespace ATEditor
{

    public interface IAttackWarningHandler
    {
        void RegisterWarningMarker(AttackWarningClip clip);
        void RegisterWarningMarker(WarningSignalType signalType, AttackWeight weight, float detectionRadius, float detectionAngle);
        void UnregisterWarningMarker();
    }
}