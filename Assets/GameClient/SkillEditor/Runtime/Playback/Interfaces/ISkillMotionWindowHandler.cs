using SkillEditor;
using UnityEngine;

namespace SkillEditor
{
    public interface ISkillMotionWindowHandler
    {
        void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode);
        void DisableLocalDeltaFilter();

        void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode);
        void DisableVisualOffset();

        void EnableVisualOffsetRecover(float speed);
        void DisableVisualOffsetRecover();
    }
}
