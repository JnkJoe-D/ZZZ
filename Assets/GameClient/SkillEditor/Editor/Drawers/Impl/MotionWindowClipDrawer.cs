using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(MotionWindowClip))]
    public class MotionWindowClipDrawer : ClipDrawer
    {
        private readonly BoxBoundsHandle _boxBoundsHandle = new BoxBoundsHandle();

        public override void DrawInspector(ClipBase clip)
        {
            MotionWindowClip motionClip = clip as MotionWindowClip;
            if (motionClip == null)
            {
                return;
            }

            base.DrawInspector(clip);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "MotionWindow 现在支持两类常用能力：本地轴过滤与约束盒。前者可直接清除 local X/Z 位移，后者用于限制角色活动范围。",
                MessageType.Info);

            if (!motionClip.UsesConstraintBox())
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            bool showInScene = GUILayout.Toggle(motionClip.showConstraintBoxInScene, "在场景中编辑约束盒", "Button", GUILayout.Height(26f));
            if (EditorGUI.EndChangeCheck())
            {
                motionClip.showConstraintBoxInScene = showInScene;
                SceneView.RepaintAll();
            }

            EditorGUILayout.HelpBox("约束盒尺寸使用本地坐标：X=左右范围，Y=高度，Z=前后范围。", MessageType.None);
        }

        public override void DrawSceneGUI(ClipBase clip, SkillEditorState state)
        {
            MotionWindowClip motionClip = clip as MotionWindowClip;
            if (motionClip == null || !motionClip.UsesConstraintBox() || !motionClip.showConstraintBoxInScene)
            {
                return;
            }

            if (state != null && (state.selectedClips == null || !state.selectedClips.Contains(clip)))
            {
                return;
            }

            GetPreviewAnchor(state, out Vector3 anchorPosition, out Quaternion anchorRotation);
            MotionConstraintBoxData boxData = MotionConstraintBoxUtility.BuildPreviewConstraintBox(motionClip, anchorPosition, anchorRotation);
            Vector3 handleAnchorPosition = boxData.Center - boxData.Rotation * motionClip.constraintBoxCenterOffset;

            using (new Handles.DrawingScope(motionClip.debugColor, Matrix4x4.TRS(handleAnchorPosition, boxData.Rotation, Vector3.one)))
            {
                _boxBoundsHandle.center = motionClip.constraintBoxCenterOffset;
                _boxBoundsHandle.size = SanitizeSize(motionClip.constraintBoxSize);

                EditorGUI.BeginChangeCheck();
                _boxBoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    if (state?.currentTimeline != null)
                    {
                        Undo.RecordObject(state.currentTimeline, "Edit MotionWindow Constraint Box");
                    }

                    motionClip.constraintBoxCenterOffset = _boxBoundsHandle.center;
                    motionClip.constraintBoxSize = SanitizeSize(_boxBoundsHandle.size);

                    if (state?.currentTimeline != null)
                    {
                        EditorUtility.SetDirty(state.currentTimeline);
                    }

                    SceneView.RepaintAll();
                }

                Color fillColor = motionClip.debugColor;
                fillColor.a *= 0.08f;
                Handles.color = fillColor;
                Handles.DrawSolidRectangleWithOutline(CreateBoxFace(motionClip.constraintBoxCenterOffset, boxData.Size), fillColor, motionClip.debugColor);
                Handles.color = motionClip.debugColor;
                Handles.DrawWireCube(motionClip.constraintBoxCenterOffset, SanitizeSize(motionClip.constraintBoxSize));
            }
        }

        public override void DrawTimelineGUI(ClipBase clip, Rect clipRect, SkillEditorState state, Color clipColor, string displayName)
        {
            if (clip is MotionWindowClip motionClip)
            {
                string filterName = motionClip.localDeltaFilterMode != MotionWindowLocalDeltaFilterMode.None
                    ? motionClip.localDeltaFilterMode.ToString()
                    : "NoFilter";
                string modeName = motionClip.constraintMode.ToString();
                displayName = $"{modeName} / {filterName}";
            }

            base.DrawTimelineGUI(clip, clipRect, state, clipColor, displayName);
        }

        private void GetPreviewAnchor(SkillEditorState state, out Vector3 anchorPosition, out Quaternion anchorRotation)
        {
            anchorPosition = Vector3.zero;
            anchorRotation = Quaternion.identity;

            if (state != null && state.hasPreviewOriginPose)
            {
                anchorPosition = state.previewOriginPos;
                anchorRotation = GetHorizontalRotation(state.previewOriginRot);
                return;
            }

            if (state?.previewTarget != null)
            {
                anchorPosition = state.previewTarget.transform.position;
                anchorRotation = GetHorizontalRotation(state.previewTarget.transform.rotation);
            }
        }

        private Quaternion GetHorizontalRotation(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private Vector3[] CreateBoxFace(Vector3 center, Vector3 size)
        {
            Vector3 half = SanitizeSize(size) * 0.5f;
            return new[]
            {
                new Vector3(center.x - half.x, center.y, center.z - half.z),
                new Vector3(center.x - half.x, center.y, center.z + half.z),
                new Vector3(center.x + half.x, center.y, center.z + half.z),
                new Vector3(center.x + half.x, center.y, center.z - half.z)
            };
        }

        private Vector3 SanitizeSize(Vector3 size)
        {
            size.x = Mathf.Max(0.01f, Mathf.Abs(size.x));
            size.y = Mathf.Max(0.01f, Mathf.Abs(size.y));
            size.z = Mathf.Max(0.01f, Mathf.Abs(size.z));
            return size;
        }
    }
}
