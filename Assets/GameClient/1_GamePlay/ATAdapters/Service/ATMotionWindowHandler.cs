using ATEditor;
using UnityEngine;

namespace Game.GamePlay
{
    public sealed class ATMotionWindowHandler : IMotionWindowHandler
    {
        private readonly CharacterEntity _entity;

        public ATMotionWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode)
        {
            _entity.MovementComponent?.SetFilterMode(filterMode);
        }
        public void DisableLocalDeltaFilter()
        {
            _entity.MovementComponent?.SetFilterMode(MotionWindowLocalDeltaFilterMode.None);
        }

        public void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask)
        {
            _entity.MovementComponent?.SetCollisionMode(mode);
            _entity.MovementComponent?.SetObstacleMask(obstacleMask);
        }

        public void DisableCollisionMode()
        {
            _entity.MovementComponent?.SetCollisionMode(RootMotionCollisionMode.DefaultSlide);
        }

        public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode)
        {
            _entity.MovementComponent?.SetVisualOffsetMode(offsetMode);
        }

        public void DisableVisualOffset()
        {
            _entity.MovementComponent?.SetVisualOffsetMode(MotionWindowVisualOffsetMode.None);
        }

        public void EnableVisualOffsetRecover(float speed)
        {
            _entity.MovementComponent?.SetVisualRecover(true, speed);
        }

        public void DisableVisualOffsetRecover()
        {
            _entity.MovementComponent?.SetVisualRecover(false);
            _entity.MovementComponent?.ResetVisualOffset();
        }
    }
}
