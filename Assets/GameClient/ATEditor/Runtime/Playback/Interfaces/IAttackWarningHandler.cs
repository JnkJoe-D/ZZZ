namespace ATEditor
{

    public interface IAttackWarningHandler
    {
        void RegisterWarningMarker(WarningSignalType signalType, AttackWeight weight, float detectionRadius, float detectionAngle);
        void UnregisterWarningMarker();
    }
}