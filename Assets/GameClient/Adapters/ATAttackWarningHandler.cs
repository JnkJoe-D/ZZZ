using ATEditor;
namespace Game.Logic
{
    public class ATAttackWarningHandler : IAttackWarningHandler
    {
        private readonly CharacterEntity _entity;
        private AttackWarningMarker _marker;
        public ATAttackWarningHandler(CharacterEntity entity)
        {
            _entity = entity;
            _marker = new AttackWarningMarker();
        }
        public void RegisterWarningMarker(WarningSignalType signalType, AttackWeight weight, float detectionRadius, float detectionAngle)
        {
            UnityEngine.Debug.Log($"!!!!!!!!!!!!!!!!!!!RegisterWarningMarker: {signalType}, {weight}, {detectionRadius}, {detectionAngle}");
            AttackWarningMarker marker = new AttackWarningMarker
            {
                Attacker = _entity,
                SignalType = signalType,
                Weight = weight,
                DetectionRadius = detectionRadius,
                DetectionAngle = detectionAngle
            };
            if (CombatWarningManager.Register(_marker))
            {
                _marker = marker;
            }
        }

        public void UnregisterWarningMarker()
        {
            if(_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }
        }
    }
}