using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(MotionFilterClip))]
    public class MotionFilterClipDrawer : ClipDrawer
    {

        public override void DrawInspector(ClipBase clip)
        {
            MotionFilterClip motionClip = clip as MotionFilterClip;
            if (motionClip == null)
            {
                return;
            }

            base.DrawInspector(clip);
        }

    }
}
