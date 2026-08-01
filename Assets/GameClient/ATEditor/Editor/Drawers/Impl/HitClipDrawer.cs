using UnityEditor;
using UnityEngine;
using ATEditor;

namespace ATEditor.Editor
{
    [CustomDrawer(typeof(HitClip))]
    public class HitClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showHitBox = true;
        private static bool _showDetect = true;
        private static bool _showVFX = true;

        public override void DrawInspector(ClipBase clip)
        {
            var hitClip = clip as HitClip;
            if (hitClip == null) return;
            
            EditorGUI.BeginChangeCheck();

            // 1. 基础时间轴控制
            _showBase = EditorGUILayout.Foldout(_showBase, "基础参数 (Base)", true, EditorStyles.foldoutHeader);
            if (_showBase)
            {
                EditorGUILayout.BeginVertical("box");
                hitClip.clipName = EditorGUILayout.TextField("片段名称", hitClip.clipName);
                hitClip.StartTime = Mathf.Max(0, EditorGUILayout.FloatField("开始时间", hitClip.StartTime));
                hitClip.Duration = Mathf.Max(0.1f, EditorGUILayout.FloatField("持续时间", hitClip.Duration));
                EditorGUILayout.EndVertical();
            }

            // 2. 检测盒与空间参数
            _showHitBox = EditorGUILayout.Foldout(_showHitBox, "碰撞盒参数 (HitBox)", true, EditorStyles.foldoutHeader);
            if (_showHitBox)
            {
                EditorGUILayout.BeginVertical("box");
                DrawFieldByName(hitClip, "shape");
                DrawFieldByName(hitClip, "isHitBoxFollowBindPoint");
                if (hitClip.isHitBoxFollowBindPoint)
                {
                    DrawFieldByName(hitClip, "bindPoint");
                    if (hitClip.bindPoint == BindPoint.CustomBone)
                        DrawFieldByName(hitClip, "customBoneName");
                }
                DrawFieldByName(hitClip, "positionOffset");
                DrawFieldByName(hitClip, "rotationOffset");
                DrawFieldByName(hitClip, "showHitBoxGizmos");
                EditorGUILayout.EndVertical();
            }

            // 3. 判定规则与表现
            _showDetect = EditorGUILayout.Foldout(_showDetect, "打击判定与表现 (Detection & Feedback)", true, EditorStyles.foldoutHeader);
            if (_showDetect)
            {
                EditorGUILayout.BeginVertical("box");
                DrawFieldByName(hitClip, "detectFrequency");
                if (hitClip.detectFrequency == Frequency.Times)
                    DrawFieldByName(hitClip, "times");
                DrawFieldByName(hitClip, "maxHitTargets");
                DrawFieldByName(hitClip, "targetSortMode");
                DrawFieldByName(hitClip, "hitLayerMask");
                DrawFieldByName(hitClip, "isSelfImpacted");
                EditorGUILayout.Space(5);
                DrawFieldByName(hitClip, "detects");
                EditorGUILayout.EndVertical();
            }

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

            // 4. VFX 场景句柄
            if (selectedDetect != null && selectedDetect.hitVFXPrefab != null)
            {
                _showVFX = EditorGUILayout.Foldout(_showVFX, "VFX 场景调试 (Scene Handles)", true, EditorStyles.foldoutHeader);
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

        private void DrawFieldByName(HitClip obj, string fieldName)
        {
            var field = typeof(HitClip).GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                DrawField(field, obj);
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
            // --- 结束同步逻辑 ---

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
                if (!damageClip.isHitBoxFollowBindPoint && activeProcess != null)
                {
                    pos = activeProcess.fixedHitBoxPosition;
                    rot = activeProcess.fixedHitBoxRotation;
                }
                else
                {
                    GetMatrix(damageClip, state, out pos, out rot);
                }

                // 绘制
                Color wireColor = isActive ? new Color(0, 1, 0, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                Color solidColor = isActive ? new Color(0, 1, 0, 0.2f) : new Color(0.5f, 0.5f, 0.5f, 0.1f);
            
            var shape = damageClip.shape;

            Handles.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Handles.color = wireColor;

            switch (shape.shapeType)
            {
                case HitBoxType.Sphere:
                    Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.up, 360f, shape.radius);
                    Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 360f, shape.radius);
                    Handles.color = solidColor;
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, shape.radius);
                    break;

                case HitBoxType.Box:
                    Handles.DrawWireCube(Vector3.zero, shape.size);
                    break;

                case HitBoxType.Capsule:
                    float extraHeight = Mathf.Max(0, shape.height - shape.radius * 2);
                    Vector3 upCap = Vector3.up * (extraHeight / 2f);
                    Vector3 downCap = Vector3.down * (extraHeight / 2f);
                    
                    Handles.DrawWireArc(upCap, Vector3.up, Vector3.forward, 360, shape.radius);
                    Handles.DrawWireArc(downCap, Vector3.up, Vector3.forward, 360, shape.radius);

                    Handles.DrawWireArc(upCap, Vector3.right, Vector3.back, 180, shape.radius);
                    Handles.DrawWireArc(upCap, Vector3.forward, Vector3.right, 180, shape.radius);

                    Handles.DrawWireArc(downCap, Vector3.right, Vector3.forward, 180, shape.radius);
                    Handles.DrawWireArc(downCap, Vector3.forward, Vector3.left, 180, shape.radius);

                    Handles.DrawLine(upCap + Vector3.forward * shape.radius, downCap + Vector3.forward * shape.radius);
                    Handles.DrawLine(upCap + Vector3.back * shape.radius, downCap + Vector3.back * shape.radius);
                    Handles.DrawLine(upCap + Vector3.right * shape.radius, downCap + Vector3.right * shape.radius);
                    Handles.DrawLine(upCap + Vector3.left * shape.radius, downCap + Vector3.left * shape.radius);
                    break;

                case HitBoxType.Sector:
                    float hS = shape.height / 2f;
                    Vector3 forwardDir = Vector3.forward;
                    Vector3 rightBoundary = Quaternion.Euler(0, shape.angle / 2, 0) * forwardDir;
                    Vector3 leftBoundary = Quaternion.Euler(0, -shape.angle / 2, 0) * forwardDir;
                    
                    Vector3 upCenter = Vector3.up * hS;
                    Vector3 downCenter = Vector3.down * hS;

                    Handles.DrawWireArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    Handles.DrawWireArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);

                    Handles.DrawLine(upCenter, upCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(upCenter, upCenter + leftBoundary * shape.radius);
                    Handles.DrawLine(downCenter, downCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(downCenter, downCenter + leftBoundary * shape.radius);

                    Handles.DrawLine(upCenter, downCenter);
                    Handles.DrawLine(upCenter + rightBoundary * shape.radius, downCenter + rightBoundary * shape.radius);
                    Handles.DrawLine(upCenter + leftBoundary * shape.radius, downCenter + leftBoundary * shape.radius);

                    Handles.color = solidColor;
                    Handles.DrawSolidArc(upCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    Handles.DrawSolidArc(downCenter, Vector3.up, leftBoundary, shape.angle, shape.radius);
                    break;

                case HitBoxType.Ring:
                    float hR = shape.height / 2f;
                    Vector3 upRing = Vector3.up * hR;
                    Vector3 downRing = Vector3.down * hR;

                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.radius);
                    Handles.DrawWireArc(upRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);
                    Handles.DrawWireArc(downRing, Vector3.up, Vector3.forward, 360f, shape.innerRadius);

                    Handles.DrawLine(upRing + Vector3.forward * shape.radius, downRing + Vector3.forward * shape.radius);
                    Handles.DrawLine(upRing - Vector3.forward * shape.radius, downRing - Vector3.forward * shape.radius);
                    Handles.DrawLine(upRing + Vector3.right * shape.radius, downRing + Vector3.right * shape.radius);
                    Handles.DrawLine(upRing - Vector3.right * shape.radius, downRing - Vector3.right * shape.radius);
                    
                    Handles.DrawLine(upRing + Vector3.forward * shape.innerRadius, downRing + Vector3.forward * shape.innerRadius);
                    Handles.DrawLine(upRing - Vector3.forward * shape.innerRadius, downRing - Vector3.forward * shape.innerRadius);
                    Handles.DrawLine(upRing + Vector3.right * shape.innerRadius, downRing + Vector3.right * shape.innerRadius);
                    Handles.DrawLine(upRing - Vector3.right * shape.innerRadius, downRing - Vector3.right * shape.innerRadius);
                    break;
            }

            Handles.matrix = Matrix4x4.identity;
            }
        }

        private void GetMatrix(HitClip clip, ATEditorState state, out Vector3 pos, out Quaternion rot)
        {
            Transform parent = null;
            if (state != null && state.PreviewContext != null)
            {
                var actor = state.PreviewContext.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    parent = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }
            
            if (parent != null)
            {
                pos = parent.position + parent.rotation * clip.positionOffset;
                rot = parent.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else
            {
                pos = clip.positionOffset;
                rot = Quaternion.Euler(clip.rotationOffset);
            }
        }
    }
}
