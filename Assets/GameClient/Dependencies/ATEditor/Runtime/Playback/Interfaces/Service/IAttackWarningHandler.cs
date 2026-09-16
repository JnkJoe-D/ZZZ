namespace ATEditor
{

    public interface IAttackWarningHandler : IService
    {
        void RegisterWarningMarker(AttackWarningClip clip);
        void RegisterWarningMarker(WarningSignalType signalType, float detectionRadius, float detectionAngle);
        void UnregisterWarningMarker();

        void OnParryContractEnter(ParryContractClip clip);
        void OnParryContractExit();
    }
}