using ATEditor;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(VFXClip))]
    public class VFXClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showVFXConfig = true;
        private static bool _showLifecycle = true;
        private static bool _showSceneHandles = true;

        public override void DrawInspector(ClipBase clip)
        {
            var vfxClip = clip as VFXClip;
            if (vfxClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 特效资源与挂载卡片
            _showVFXConfig = EditorGUILayout.Foldout(_showVFXConfig, "特效资源与挂载", true, EditorStyles.foldoutHeader);
            if (_showVFXConfig)
            {
                EditorGUILayout.BeginVertical("box");
                vfxClip.effectPrefab = (GameObject)EditorGUILayout.ObjectField("特效预制体", vfxClip.effectPrefab, typeof(GameObject), false);
                vfxClip.bindPoint = (BindPoint)EditorGUILayout.EnumPopup("挂载点", vfxClip.bindPoint);
                if (vfxClip.bindPoint == BindPoint.CustomBone)
                {
                    vfxClip.customBoneName = EditorGUILayout.TextField("自定义骨骼名", vfxClip.customBoneName);
                }
                vfxClip.followTarget = EditorGUILayout.Toggle("跟随挂载点移动", vfxClip.followTarget);
                vfxClip.positionOffset = EditorGUILayout.Vector3Field("位置偏移", vfxClip.positionOffset);
                vfxClip.rotationOffset = EditorGUILayout.Vector3Field("旋转偏移", vfxClip.rotationOffset);
                vfxClip.scale = EditorGUILayout.Vector3Field("缩放比例", vfxClip.scale);
                EditorGUILayout.EndVertical();
            }

            // 3. 生命周期卡片
            _showLifecycle = EditorGUILayout.Foldout(_showLifecycle, "生命周期控制", true, EditorStyles.foldoutHeader);
            if (_showLifecycle)
            {
                EditorGUILayout.BeginVertical("box");
                vfxClip.destroyOnEnd = EditorGUILayout.Toggle("片段结束时销毁", vfxClip.destroyOnEnd);
                vfxClip.stopEmissionOnEnd = EditorGUILayout.Toggle("结束时停止发射粒子", vfxClip.stopEmissionOnEnd);
                EditorGUILayout.EndVertical();
            }

            // 4. 场景编辑句柄工具
            if (vfxClip.effectPrefab != null)
            {
                _showSceneHandles = EditorGUILayout.Foldout(_showSceneHandles, "场景交互工具", true, EditorStyles.foldoutHeader);
                if (_showSceneHandles)
                {
                    EditorGUILayout.BeginVertical("box");

                    // 默认选中移动句柄 (Position)
                    if (vfxClip.activeHandleType == VFXClip.VFXHandleType.None)
                    {
                        vfxClip.activeHandleType = VFXClip.VFXHandleType.Position;
                    }

                    int currentHandleIndex = vfxClip.activeHandleType switch
                    {
                        VFXClip.VFXHandleType.Rotation => 1,
                        VFXClip.VFXHandleType.Scale => 2,
                        _ => 0
                    };

                    string[] handleLabels = new string[] { "移动句柄", "旋转句柄", "缩放句柄" };
                    int newHandleIndex = GUILayout.Toolbar(currentHandleIndex, handleLabels, GUILayout.Height(24));
                    if (newHandleIndex != currentHandleIndex)
                    {
                        vfxClip.activeHandleType = newHandleIndex switch
                        {
                            1 => VFXClip.VFXHandleType.Rotation,
                            2 => VFXClip.VFXHandleType.Scale,
                            _ => VFXClip.VFXHandleType.Position
                        };

                        if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Position) Tools.current = Tool.Move;
                        else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation) Tools.current = Tool.Rotate;
                        else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale) Tools.current = Tool.Scale;

                        SceneView.RepaintAll();
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify VFX Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify VFX Clip");
            }
        }

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var vfxClip = clip as VFXClip;
            if (vfxClip == null || vfxClip.effectPrefab == null) return;

            // 默认选中移动句柄，常驻显示
            if (vfxClip.activeHandleType == VFXClip.VFXHandleType.None)
            {
                vfxClip.activeHandleType = VFXClip.VFXHandleType.Position;
            }

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

            // 获取特效挂载点 Transform
            var boneGetter = new ATBoneGetter(rootTrans.gameObject);
            Transform bindTrans = boneGetter.GetBone(vfxClip.bindPoint, vfxClip.customBoneName) ?? rootTrans;

            // 计算基准世界位置与旋转
            Vector3 refPos = bindTrans.position;
            Quaternion refRot = bindTrans.rotation;

            Vector3 vfxWorldPos = refPos + refRot * vfxClip.positionOffset;
            Quaternion vfxWorldRot = refRot * Quaternion.Euler(vfxClip.rotationOffset);
            Vector3 vfxScale = vfxClip.scale;

            // 绘制与挂点的辅助连线与圆环
            Handles.color = new Color(0.2f, 0.7f, 1f, 0.6f);
            Handles.DrawLine(refPos, vfxWorldPos);
            Handles.DrawWireDisc(vfxWorldPos, Vector3.up, 0.2f);

            var labelStyle = new GUIStyle
            {
                normal = new GUIStyleState { textColor = new Color(0.3f, 0.85f, 1f) },
                fontStyle = FontStyle.Bold
            };
            string handleName = vfxClip.activeHandleType switch
            {
                VFXClip.VFXHandleType.Rotation => "🌀 特效旋转",
                VFXClip.VFXHandleType.Scale => "🔍 特效缩放",
                _ => "✨ 特效位置"
            };
            Handles.Label(vfxWorldPos + Vector3.up * 0.25f, $"{handleName} ({vfxClip.clipName})", labelStyle);

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldPos = vfxWorldPos;
            Quaternion newWorldRot = vfxWorldRot;
            Vector3 newScale = vfxScale;

            switch (vfxClip.activeHandleType)
            {
                case VFXClip.VFXHandleType.Rotation:
                    newWorldRot = Handles.RotationHandle(vfxWorldRot, vfxWorldPos);
                    break;
                case VFXClip.VFXHandleType.Scale:
                    newScale = Handles.ScaleHandle(vfxScale, vfxWorldPos, vfxWorldRot, HandleUtility.GetHandleSize(vfxWorldPos));
                    break;
                case VFXClip.VFXHandleType.Position:
                default:
                    Quaternion pHandleRot = (Tools.pivotRotation == PivotRotation.Global) ? Quaternion.identity : vfxWorldRot;
                    newWorldPos = Handles.PositionHandle(vfxWorldPos, pHandleRot);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                // 反算相对偏移
                if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation)
                {
                    Quaternion localRot = Quaternion.Inverse(refRot) * newWorldRot;
                    vfxClip.rotationOffset = localRot.eulerAngles;
                }
                else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale)
                {
                    vfxClip.scale = newScale;
                }
                else
                {
                    Vector3 localPos = Quaternion.Inverse(refRot) * (newWorldPos - refPos);
                    vfxClip.positionOffset = localPos;
                }

                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    var window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                    if (window != null)
                    {
                        var timeline = window.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            Undo.RecordObject(timeline, "Sync VFX Transform");
                            EditorUtility.SetDirty(timeline);
                        }

                        // 若当前恰好处于播放中且存在活动特效实例，同步其实例位置
                        if (window.PreviewRunner != null)
                        {
                            foreach (var p in window.PreviewRunner.ActiveProcesses)
                            {
                                if (p.clip == vfxClip && p.isActive && p.process is Editor.EditorVFXProcess process)
                                {
                                    process.ForceUpdateTransform();
                                    break;
                                }
                            }
                        }
                        window.Repaint();
                    }
                }
            }
        }

        protected override bool ShouldShow(System.Reflection.FieldInfo field, object obj)
        {
            if (!base.ShouldShow(field, obj)) return false;

            // 简单的硬编砀ShowIf 逻辑
            if (field.Name == "blendInDuration" || field.Name == "blendOutDuration")
            {
                if (obj is ClipBase c && !c.SupportsBlending) return false;
            }

            // 自定义骨骼名仅在 bindPoint == CustomBone 时显礀
            if (field.Name == "customBoneName")
            {
                if (obj is VFXClip vfx && vfx.bindPoint != BindPoint.CustomBone)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
