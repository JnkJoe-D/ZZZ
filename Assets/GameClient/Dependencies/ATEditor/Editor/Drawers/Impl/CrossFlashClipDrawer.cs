using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 十字预警闪光片段专属抽屉，支持在 Scene 视口中可视化编辑挂点偏移
    /// </summary>
    [CustomDrawer(typeof(CrossFlashClip))]
    public class CrossFlashClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            var flashClip = clip as CrossFlashClip;
            if (flashClip == null) return;

            base.DrawInspector(clip);
        }

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var flashClip = clip as CrossFlashClip;
            if (flashClip == null) return;

            Transform rootTrans = null;
            if (state != null && state.PreviewContext != null && state.PreviewContext.Owner != null)
            {
                rootTrans = state.PreviewContext.Owner.transform;
            }
            else if (state != null && state.previewTarget != null)
            {
                rootTrans = state.previewTarget.transform;
            }

            if (rootTrans == null) return;

            var boneGetter = new ATBoneGetter(rootTrans.gameObject);
            Transform bindTrans = boneGetter.GetBone(flashClip.bindPoint, flashClip.customBoneName) ?? rootTrans;

            // 核心约束：仅同步绑定点的世界坐标，不同步旋转
            Vector3 refPos = bindTrans.position;
            Vector3 worldPos = refPos + flashClip.positionOffset;

            // 绘制挂点到目标点的黄色预警图元
            Handles.color = new Color(1f, 0.8f, 0.1f, 0.75f);
            Handles.DrawDottedLine(refPos, worldPos, 4f);
            Handles.DrawWireDisc(worldPos, Vector3.up, 0.15f);
            Handles.DrawLine(worldPos - Vector3.right * 0.25f, worldPos + Vector3.right * 0.25f);
            Handles.DrawLine(worldPos - Vector3.up * 0.25f, worldPos + Vector3.up * 0.25f);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = new GUIStyleState { textColor = new Color(1f, 0.85f, 0.2f) }
            };
            Handles.Label(worldPos + Vector3.up * 0.2f, $"十字中心: {flashClip.clipName}", labelStyle);

            // 核心约束：句柄轴向为世界坐标 (Quaternion.identity)
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                flashClip.positionOffset = newWorldPos - refPos;
                MarkTimelineDirty("Adjust CrossFlash Position Offset");
            }
        }
    }
}
