using ATEditor;
using UnityEngine;

namespace ATEditor
{
    public interface IMotionWindowHandler
    {
        void EnableLocalDeltaFilter(MotionWindowLocalDeltaFilterMode filterMode);
        void DisableLocalDeltaFilter();

        void EnableCollisionMode(RootMotionCollisionMode mode, LayerMask obstacleMask);
        void DisableCollisionMode();

        void EnableVisualOffset(MotionWindowVisualOffsetMode offsetMode);
        void DisableVisualOffset();

        void EnableVisualOffsetRecover(float speed);
        void DisableVisualOffsetRecover();
    }
}
