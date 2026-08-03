using UnityEditor;
using UnityEngine;
using ATEditor;

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
                    GUILayout.BeginHorizontal();

                    bool isPos = vfxClip.activeHandleType == VFXClip.VFXHandleType.Position;
                    bool isRot = vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation;
                    bool isSca = vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale;

                    GUIStyle leftStyle = new GUIStyle(EditorStyles.miniButtonLeft) { fontSize = 12, fixedHeight = 24 };
                    GUIStyle midStyle = new GUIStyle(EditorStyles.miniButtonMid) { fontSize = 12, fixedHeight = 24 };
                    GUIStyle rightStyle = new GUIStyle(EditorStyles.miniButtonRight) { fontSize = 12, fixedHeight = 24 };

                    bool newPos = GUILayout.Toggle(isPos, "位置句柄", leftStyle);
                    bool newRot = GUILayout.Toggle(isRot, "旋转句柄", midStyle);
                    bool newSca = GUILayout.Toggle(isSca, "缩放句柄", rightStyle);

                    GUILayout.EndHorizontal();

                    VFXClip.VFXHandleType newType = VFXClip.VFXHandleType.None;
                    if (newPos && !isPos) newType = VFXClip.VFXHandleType.Position;
                    else if (newRot && !isRot) newType = VFXClip.VFXHandleType.Rotation;
                    else if (newSca && !isSca) newType = VFXClip.VFXHandleType.Scale;

                    if (isPos && !newPos) newType = VFXClip.VFXHandleType.None;
                    if (isRot && !newRot) newType = VFXClip.VFXHandleType.None;
                    if (isSca && !newSca) newType = VFXClip.VFXHandleType.None;

                    if (newType != vfxClip.activeHandleType)
                    {
                        vfxClip.activeHandleType = newType;
                        if (newType == VFXClip.VFXHandleType.Position) Tools.current = Tool.Move;
                        else if (newType == VFXClip.VFXHandleType.Rotation) Tools.current = Tool.Rotate;
                        else if (newType == VFXClip.VFXHandleType.Scale) Tools.current = Tool.Scale;
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
            if (vfxClip.activeHandleType == VFXClip.VFXHandleType.None) return;

            bool isActive = !state.isStopped && state.timeIndicator >= clip.StartTime && state.timeIndicator <= clip.StartTime + clip.Duration;
            if (!isActive) return;

            Editor.EditorVFXProcess activeProcess = null;
            ATEditorWindow window = null;
            if (EditorWindow.HasOpenInstances<ATEditorWindow>())
            {
                window = EditorWindow.GetWindow<ATEditorWindow>(false, "技能编辑器", false);
                if (window != null && window.PreviewRunner != null)
                {
                    foreach (var p in window.PreviewRunner.ActiveProcesses)
                    {
                        if (p.clip == vfxClip && p.isActive && p.process is Editor.EditorVFXProcess process)
                        {
                            activeProcess = process;
                            break;
                        }
                    }
                }
            }

            if (activeProcess != null && activeProcess.Instance != null && window != null)
            {
                // 获取当前实例的世界坐标和旋转
                Vector3 currentPos = activeProcess.Instance.transform.position;
                Quaternion currentRot = activeProcess.Instance.transform.rotation;
                Vector3 currentScale = activeProcess.Instance.transform.localScale;

                EditorGUI.BeginChangeCheck();
                
                Vector3 newPos = currentPos;
                Quaternion newRot = currentRot;
                Vector3 newScale = currentScale;

                switch (vfxClip.activeHandleType)
                {
                    case VFXClip.VFXHandleType.Position:
                        Quaternion pHandleRot = (Tools.pivotRotation == PivotRotation.Global) ? Quaternion.identity : currentRot;
                        newPos = Handles.PositionHandle(currentPos, pHandleRot);
                        Handles.Label(newPos + Vector3.up * 0.2f, "  特效预览位置", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                        break;
                    case VFXClip.VFXHandleType.Rotation:
                        // Keep editing from the instance's latest world rotation so multi-axis drags accumulate correctly.
                        newRot = Handles.RotationHandle(currentRot, currentPos); 
                        Handles.Label(currentPos + Vector3.up * 0.2f, "  特效预览旋转", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                        break;
                    case VFXClip.VFXHandleType.Scale:
                        newScale = Handles.ScaleHandle(currentScale, currentPos, currentRot, HandleUtility.GetHandleSize(currentPos));
                        Handles.Label(currentPos + Vector3.up * 0.2f, "  特效预览缩放", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    // 将当前的变换信息交由 activeProcess 进行反推计算
                    if (window != null)
                    {
                        var timeline = window.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            Undo.RecordObject(timeline, "Sync VFX Transform");
                            
                            // 将把手拖动的新变动立即赋给临时的 Instance 
                            if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Position) activeProcess.Instance.transform.position = newPos;
                            else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation) activeProcess.Instance.transform.rotation = newRot;
                            else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale) activeProcess.Instance.transform.localScale = newScale;

                            // 调取现成皀GetCurrentRelativeOffset 
                            // 里面处理了是否跟随第一帧缓存坐标系的全部反算逻辑　
                            activeProcess.GetCurrentRelativeOffset(out Vector3 pOffset, out Vector3 rOffset, out Vector3 sOffset);
                            
                            if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Position)
                            {
                                vfxClip.positionOffset = pOffset;
                            }
                            else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation)
                            {
                                vfxClip.rotationOffset = rOffset;
                            }
                            else if (vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale)
                            {
                                vfxClip.scale = sOffset;
                            }

                            EditorUtility.SetDirty(timeline);
                            activeProcess.ForceUpdateTransform();
                            window.Repaint();
                        }
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
