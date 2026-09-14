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
            _marker = null;
        }
        public void RegisterWarningMarker(AttackWarningClip clip)
        {
            if (clip == null) return;
            if (_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }

            AttackWarningMarker marker = new AttackWarningMarker
            {
                Attacker = _entity,
                SignalType = clip.SignalType,
                ParryWeight = clip.ParryWeight,
                DetectionRadius = clip.DetectionRadius > 0 ? clip.DetectionRadius : 10.0f,
                DetectionAngle = clip.DetectionAngle > 0 ? clip.DetectionAngle : 180.0f,
                CoverageShape = clip.CoverageShape,
                CoverageCenterOffset = clip.CoverageCenterOffset,
                ClashPositionOffset = clip.ClashPositionOffset,
                AllowInPlaceParry = clip.AllowInPlaceParry
            };
            if (CombatWarningManager.Register(marker))
            {
                _marker = marker;
            }
        }

        public void RegisterWarningMarker(WarningSignalType signalType, float detectionRadius, float detectionAngle)
        {
            if (_marker != null)
            {
                CombatWarningManager.Unregister(_marker);
                _marker = null;
            }

            AttackWarningMarker marker = new AttackWarningMarker
            {
                Attacker = _entity,
                SignalType = signalType,
                DetectionRadius = detectionRadius,
                DetectionAngle = detectionAngle
            };
            if (CombatWarningManager.Register(marker))
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