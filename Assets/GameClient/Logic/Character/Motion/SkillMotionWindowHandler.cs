using System.Collections.Generic;
using SkillEditor;

namespace Game.Logic.Character.Motion
{
    public sealed class SkillMotionWindowHandler : ISkillMotionWindowHandler
    {
        private readonly CharacterEntity _entity;

        public SkillMotionWindowHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode)
        {
            _entity.MovementController?.SetFilterMode(filterMode);
        }
        public void DisableLocalDeltaFilter()
        {
            _entity.MovementController?.SetFilterMode(MotionWindowLocalDeltaFilterMode.None);
        }

        public void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode)
        {
            _entity.MovementController?.SetVisualOffsetMode(offsetMode);
        }

        public void DisableVisualOffset()
        {
            _entity.MovementController?.SetVisualOffsetMode(MotionWindowVisualOffsetMode.None);
        }

        public void EnableVisualOffsetRecover(float speed)
        {
            _entity.MovementController?.SetVisualRecover(true, speed);
        }

        public void DisableVisualOffsetRecover()
        {
            _entity.MovementController?.SetVisualRecover(false);
            _entity.MovementController?.ResetVisualOffset();
        }
    }
}
