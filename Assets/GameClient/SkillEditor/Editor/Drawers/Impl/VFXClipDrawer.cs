using UnityEditor;
using UnityEngine;
using SkillEditor;

namespace SkillEditor.Editor
{
    [CustomDrawer(typeof(VFXClip))]
    public class VFXClipDrawer : ClipDrawer
    {
        public override void DrawInspector(ClipBase clip)
        {
            var vfxClip = clip as VFXClip;
            if (vfxClip == null) return;
            
            EditorGUILayout.LabelField("特效片段设置", EditorStyles.boldLabel);
            
            // 使用基类的反射绘制
            EditorGUI.BeginChangeCheck();
            base.DrawInspector(clip);
            bool propertiesChanged = EditorGUI.EndChangeCheck();

            // 绘制控制是否显示场景句柄的按钮（类签页样式Toolbar）
            if (vfxClip.effectPrefab != null)
            {
                GUILayout.Space(10);
                
                string[] toolbarOptions = new string[] { "位置", "旋转", "缩放" };
                
                EditorGUI.BeginChangeCheck();
                
                // Toolbar的选中索引。如果当前是None，则不应该有选中项。
                // 我们可以用一个额外的变量或者巧妙地映射来处理 None 状态。
                // 为了让 Toolbar 能取消选中，这里我们自己画一组连在一起的 Toggle 按钮
                
                GUILayout.BeginHorizontal();
                
                bool isPos = vfxClip.activeHandleType == VFXClip.VFXHandleType.Position;
                bool isRot = vfxClip.activeHandleType == VFXClip.VFXHandleType.Rotation;
                bool isSca = vfxClip.activeHandleType == VFXClip.VFXHandleType.Scale;
                
                GUIStyle leftStyle = new GUIStyle(EditorStyles.miniButtonLeft) { fontSize = 12, fixedHeight = 24 };
                GUIStyle midStyle = new GUIStyle(EditorStyles.miniButtonMid) { fontSize = 12, fixedHeight = 24 };
                GUIStyle rightStyle = new GUIStyle(EditorStyles.miniButtonRight) { fontSize = 12, fixedHeight = 24 };
                
                bool newPos = GUILayout.Toggle(isPos, "位置", leftStyle);
                bool newRot = GUILayout.Toggle(isRot, "旋转", midStyle);
                bool newSca = GUILayout.Toggle(isSca, "缩放", rightStyle);
                
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    VFXClip.VFXHandleType newType = VFXClip.VFXHandleType.None;
                    
                    // 逻辑：如果点击了已经激活的，则取消激活（变成None）。
                    // 否则切换到新点击的类型。
                    if (newPos && !isPos) newType = VFXClip.VFXHandleType.Position;
                    else if (newRot && !isRot) newType = VFXClip.VFXHandleType.Rotation;
                    else if (newSca && !isSca) newType = VFXClip.VFXHandleType.Scale;
                    // 如果原本是激活的，由于使用了Toggle组的互斥和点击反选，我们可以判断：
                    // 新选中的其实就是触发事件的，如果它是 false 且原本是 true，说明是被点掉的
                    
                    if(isPos && !newPos) newType = VFXClip.VFXHandleType.None;
                    if(isRot && !newRot) newType = VFXClip.VFXHandleType.None;
                    if(isSca && !newSca) newType = VFXClip.VFXHandleType.None;
                    
                    vfxClip.activeHandleType = newType;
                    
                    if (newType == VFXClip.VFXHandleType.Position) Tools.current = Tool.Move;
                    else if (newType == VFXClip.VFXHandleType.Rotation) Tools.current = Tool.Rotate;
                    else if (newType == VFXClip.VFXHandleType.Scale) Tools.current = Tool.Scale;
                    
                    SceneView.RepaintAll();
                }
            }
        }

        public override void DrawSceneGUI(ClipBase clip, SkillEditorState state)
        {
            var vfxClip = clip as VFXClip;
            if (vfxClip == null || vfxClip.effectPrefab == null) return;
            if (vfxClip.activeHandleType == VFXClip.VFXHandleType.None) return;

            bool isActive = !state.isStopped && state.timeIndicator >= clip.StartTime && state.timeIndicator <= clip.StartTime + clip.Duration;
            if (!isActive) return;

            Editor.EditorVFXProcess activeProcess = null;
            SkillEditorWindow window = null;
            if (EditorWindow.HasOpenInstances<SkillEditorWindow>())
            {
                window = EditorWindow.GetWindow<SkillEditorWindow>(false, "技能编辑器", false);
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

                            // 调取现成的 GetCurrentRelativeOffset 
                            // 里面处理了是否跟随第一帧缓存坐标系的全部反算逻辑。
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
            
            // 简单的硬编码 ShowIf 逻辑
            if (field.Name == "blendInDuration" || field.Name == "blendOutDuration")
            {
                if (obj is ClipBase c && !c.SupportsBlending) return false;
            }

            // 自定义骨骼名仅在 bindPoint == CustomBone 时显示
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
