using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(VFXTrack))]
    public class VFXTrackDrawer : TrackDrawer
    {
        public override void DrawInspector(TrackBase track)
        {
            var vfxTrack = track as VFXTrack;
            if (vfxTrack == null) return;

            EditorGUILayout.LabelField("特效轨道", EditorStyles.boldLabel);
            base.DrawInspector(track);
        }
    }
}
