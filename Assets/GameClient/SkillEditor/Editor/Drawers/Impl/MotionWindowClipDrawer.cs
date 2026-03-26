using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(MotionWindowClip))]
    public class MotionWindowClipDrawer : ClipDrawer
    {

        public override void DrawInspector(ClipBase clip)
        {
            MotionWindowClip motionClip = clip as MotionWindowClip;
            if (motionClip == null)
            {
                return;
            }

            base.DrawInspector(clip);
        }

    }
}
