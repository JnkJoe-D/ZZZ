using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(HitClip))]
    public class HitClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showVFX = true;

        // 组件化攻击检测卡片 (HitClip 中为必要卡片，锁定打勾不可取消)
        private readonly CombatDetectionCardDrawer _combatCard = new CombatDetectionCardDrawer(isMandatory: true);

        public HitClipDrawer()
        {
            _combatCard.CardKey = "HitClip_CombatDetection";
        }

        public override void DrawInspector(ClipBase clip)
        {
            var hitClip = clip as HitClip;
            if (hitClip == null) return;
            
            EditorGUI.BeginChangeCheck();

            // 1. 构建卡片上下文
            CardDrawContext context = new CardDrawContext
            {
                Clip = hitClip,
                UndoContext = UndoContext,
                State = GetEditorState(),
                PreviewTarget = GetPreviewTarget()
            };

            // 2. 基础信息卡片 (粒子系统风格 Header，必需)
            bool baseEnabled = true;
            CardDrawerBase<object>.DrawParticleStyleHeader("基础信息", isMandatory: true, ref baseEnabled, ref _showBase);
            if (_showBase)
            {
                EditorGUILayout.BeginVertical("box");
                hitClip.clipName = EditorGUILayout.TextField("片段名称", hitClip.clipName);
                hitClip.isEnabled = EditorGUILayout.Toggle("启用", hitClip.isEnabled);
                hitClip.StartTime = Mathf.Max(0f, EditorGUILayout.FloatField("开始时间", hitClip.StartTime));
                hitClip.Duration = Mathf.Max(0.01f, EditorGUILayout.FloatField("持续时间", hitClip.Duration));
                EditorGUILayout.EndVertical();
            }

            // 3. 攻击检测设置卡片 (组件化抽屉，包含检测盒范围与判定规则表现，必需卡片)
            _combatCard.BindData(new CombatDetectionCardData(hitClip.hitBoxScope, hitClip.attackPolicy));
            _combatCard.DrawInspector(context);

            bool propertiesChanged = EditorGUI.EndChangeCheck();

            // 当前选中的 DetectConfig 的 VFX 字段变化时刷新预览
            var selectedDetect = hitClip.SelectedDetect;
            if (propertiesChanged && selectedDetect != null && selectedDetect.hitVFXPrefab != null)
            {
                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    var window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                    if (window != null && window.PreviewRunner != null)
                    {
                        foreach (var p in window.PreviewRunner.ActiveProcesses)
                        {
                            if (p.clip == hitClip && p.isActive && p.process is Editor.EditorHitProcess hitProcess)
                            {
                                hitProcess.ForceUpdateTransform();
                                break;
                            }
                        }
                    }
                }
            }

            // 4. VFX 场景句柄卡片
            if (selectedDetect != null && selectedDetect.hitVFXPrefab != null)
            {
                bool vfxEnabled = true;
                CardDrawerBase<object>.DrawParticleStyleHeader("受击特效场景调试", isMandatory: true, ref vfxEnabled, ref _showVFX);
                if (_showVFX)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    string[] handleNames = { "无", "位置", "缩放" };
                    HitClip.HitVFXHandleType[] handleValues = { 
                        HitClip.HitVFXHandleType.None, 
                        HitClip.HitVFXHandleType.Position, 
                        HitClip.HitVFXHandleType.Scale 
                    };

                    int currentIndex = 0;
                    for (int i = 0; i < handleValues.Length; i++)
                    {
                        if (hitClip.activeVFXHandleType == handleValues[i])
                        {
                            currentIndex = i;
                            break;
                        }
                    }

                    int newIndex = GUILayout.Toolbar(currentIndex, handleNames, GUILayout.Height(25), GUILayout.Width(200));

                    if (newIndex != currentIndex)
                    {
                        hitClip.activeVFXHandleType = handleValues[newIndex];
                        if (hitClip.activeVFXHandleType != HitClip.HitVFXHandleType.None)
                        {
                            if (Tools.current == Tool.None || Tools.current == Tool.View)
                                Tools.current = Tool.Move;
                        }
                        SceneView.RepaintAll();
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
        }

        public override void DrawSceneGUI(ClipBase clip, ATEditorState state)
        {
            var damageClip = clip as HitClip;
            if (damageClip == null) return;

            // 判断是否在时间范围内
            bool isActive = !state.isStopped && state.timeIndicator >= clip.StartTime && state.timeIndicator <= clip.StartTime + clip.Duration;

            // --- 实时同步特效位置逻辑 ---
            var selectedDetect = damageClip.SelectedDetect;
            if (isActive && selectedDetect != null && selectedDetect.hitVFXPrefab != null && damageClip.activeVFXHandleType != HitClip.HitVFXHandleType.None)
            {
                Editor.EditorHitProcess activeProcess = null;
                ATEditorWindow window = null;
                if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                {
                    window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                    if (window != null && window.PreviewRunner != null)
                    {
                        foreach (var p in window.PreviewRunner.ActiveProcesses)
                        {
                            if (p.clip == damageClip && p.isActive && p.process is Editor.EditorHitProcess hitProcess)
                            {
                                activeProcess = hitProcess;
                                break;
                            }
                        }
                    }
                }

                if (activeProcess != null && activeProcess.Instance != null && window != null)
                {
                    Vector3 currentPos = activeProcess.Instance.transform.position;
                    Quaternion currentRot = activeProcess.Instance.transform.rotation;
                    Vector3 currentScale = activeProcess.Instance.transform.localScale;
                    
                    EditorGUI.BeginChangeCheck();

                    Vector3 newPos = currentPos;
                    Vector3 newScale = currentScale;

                    if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Position)
                    {
                        Quaternion pHandleRot = (Tools.pivotRotation == PivotRotation.Global) ? Quaternion.identity : currentRot;
                        newPos = Handles.PositionHandle(currentPos, pHandleRot);
                        Handles.Label(newPos + Vector3.up * 0.2f, "受击特效预览位置", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                    }
                    else if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Scale)
                    {
                        newScale = Handles.ScaleHandle(currentScale, currentPos, currentRot, HandleUtility.GetHandleSize(currentPos));
                        Handles.Label(currentPos + Vector3.up * 0.2f, "受击特效预览缩放", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        var timeline = window.GetCurrentTimeline();
                        if (timeline != null)
                        {
                            Undo.RecordObject(timeline, "Sync Hit VFX Transform");

                            if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Position) activeProcess.Instance.transform.position = newPos;
                            else if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Scale) activeProcess.Instance.transform.localScale = newScale;

                            activeProcess.GetCurrentRelativeOffset(out float pHeight, out Vector2 pOffsetXZ);

                            if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Position)
                            {
                                selectedDetect.hitVFXHeight = pHeight;
                                selectedDetect.hitVFXPreviewOffsetXZ = pOffsetXZ;
                            }
                            else if (damageClip.activeVFXHandleType == HitClip.HitVFXHandleType.Scale)
                            {
                                selectedDetect.hitVFXScale = newScale;
                            }
                            
                            EditorUtility.SetDirty(timeline);
                            activeProcess.ForceUpdateTransform();
                            window.Repaint();
                        }
                    }
                }
            }
            // --- 结束特效同步逻辑 ---

            // --- 统一委托攻击检测卡片绘制检测盒 Gizmos ---
            if (damageClip.showHitBoxGizmos)
            {
                Editor.EditorHitProcess activeProcess = null;
                if (isActive)
                {
                    ATEditorWindow window = null;
                    if (EditorWindow.HasOpenInstances<ATEditorWindow>())
                    {
                        window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                        if (window != null && window.PreviewRunner != null)
                        {
                            foreach (var p in window.PreviewRunner.ActiveProcesses)
                            {
                                if (p.clip == damageClip && p.isActive && p.process is Editor.EditorHitProcess hitProcess)
                                {
                                    activeProcess = hitProcess;
                                    break;
                                }
                            }
                        }
                    }
                }

                Vector3 pos;
                Quaternion rot;
                if (activeProcess != null)
                {
                    activeProcess.GetHitBoxMatrix(out pos, out rot);
                }
                else
                {
                    GetMatrix(damageClip, state, out pos, out rot);
                }

                CardDrawContext context = new CardDrawContext
                {
                    Clip = damageClip,
                    State = state,
                    OriginPose = new Pose(pos, rot),
                    NeedsApplyCardOffset = false, // pos, rot 已经包含 offset
                    PreviewTarget = state?.previewTarget
                };

                _combatCard.BindData(new CombatDetectionCardData(damageClip.hitBoxScope, damageClip.attackPolicy));
                _combatCard.DrawSceneGUI(context);
            }
        }

        private void GetMatrix(HitClip clip, ATEditorState state, out Vector3 pos, out Quaternion rot)
        {
            Transform rootTrans = null;
            if (state != null && state.PreviewContext != null && state.PreviewContext.Owner != null)
            {
                rootTrans = state.PreviewContext.Owner.transform;
            }
            else if (state != null && state.previewTarget != null)
            {
                rootTrans = state.previewTarget.transform;
            }

            Quaternion rootRot = rootTrans != null ? rootTrans.rotation : Quaternion.identity;
            Vector3 rootPos = rootTrans != null ? rootTrans.position : Vector3.zero;

            Transform bindTrans = null;
            if (state != null && state.PreviewContext != null)
            {
                var actor = state.PreviewContext.GetService<IBoneGetter>();
                if (actor != null)
                {
                    bindTrans = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }

            if (bindTrans == null && state != null && state.previewTarget != null)
            {
                var getter = new ATBoneGetter(state.previewTarget);
                bindTrans = getter.GetBone(clip.bindPoint, clip.customBoneName);
            }

            if (bindTrans == null) bindTrans = rootTrans;

            switch (clip.hitBoxFollowMode)
            {
                case HitBoxFollowMode.PositionOnly:
                    if (bindTrans != null)
                        pos = bindTrans.position + rootRot * clip.positionOffset;
                    else
                        pos = rootPos + rootRot * clip.positionOffset;
                    rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.RotationOnly:
                    pos = rootPos + rootRot * clip.positionOffset;
                    if (bindTrans != null)
                        rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
                    else
                        rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.None:
                    if (bindTrans != null && clip.bindPoint != BindPoint.LogicRoot)
                        pos = bindTrans.position + rootRot * clip.positionOffset;
                    else
                        pos = rootPos + rootRot * clip.positionOffset;
                    rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.Both:
                default:
                    if (bindTrans != null)
                    {
                        pos = bindTrans.position + bindTrans.rotation * clip.positionOffset;
                        rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
                    }
                    else
                    {
                        pos = rootPos + rootRot * clip.positionOffset;
                        rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    }
                    break;
            }
        }

        private static ATEditorState GetEditorState()
        {
            if (EditorWindow.HasOpenInstances<ATEditorWindow>())
            {
                var window = EditorWindow.GetWindow<ATEditorWindow>(false, "动作时间轴编辑器", false);
                return window != null ? window.GetState() : null;
            }
            return null;
        }

        private static GameObject GetPreviewTarget()
        {
            var state = GetEditorState();
            if (state != null)
            {
                if (state.PreviewContext != null && state.PreviewContext.Owner != null) return state.PreviewContext.Owner;
                return state.previewTarget;
            }
            return null;
        }
    }
}
