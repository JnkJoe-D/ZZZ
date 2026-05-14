using ATEditor;
using UnityEngine;

namespace ATEditor
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
