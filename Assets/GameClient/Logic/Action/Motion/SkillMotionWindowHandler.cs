using ATEditor;
using UnityEngine;

namespace Game.Logic
{
    public sealed class SkillMotionWindowHandler : IMotionWindowHandler
    {
        private readonly CharacterEntity _entity;

        public SkillMotionWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode)
        {
            _entity.CharacterMotor?.SetFilterMode(filterMode);
        }
        public void DisableLocalDeltaFilter()
        {
            _entity.CharacterMotor?.SetFilterMode(MotionWindowLocalDeltaFilterMode.None);
        }

        public void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask)
        {
            _entity.CharacterMotor?.SetCollisionMode(mode);
            _entity.CharacterMotor?.SetObstacleMask(obstacleMask);
        }

        public void DisableCollisionMode()
        {
            _entity.CharacterMotor?.SetCollisionMode(RootMotionCollisionMode.DefaultSlide);
        }

        public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode)
        {
            _entity.CharacterMotor?.SetVisualOffsetMode(offsetMode);
        }

        public void DisableVisualOffset()
        {
            _entity.CharacterMotor?.SetVisualOffsetMode(MotionWindowVisualOffsetMode.None);
        }

        public void EnableVisualOffsetRecover(float speed)
        {
            _entity.CharacterMotor?.SetVisualRecover(true, speed);
        }

        public void DisableVisualOffsetRecover()
        {
            _entity.CharacterMotor?.SetVisualRecover(false);
            _entity.CharacterMotor?.ResetVisualOffset();
        }
    }
}
