using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using ATEditor;

namespace ATEditor.Editor
{
    /// <summary>
    /// 动画相机片段抽屉 (CameraAnimationClip)
    /// </summary>
    [CustomDrawer(typeof(CameraAnimationClip))]
    public class CameraAnimationClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showCamera = true;
        private static bool _showOverride = true;

        public override void DrawInspector(ClipBase clip)
        {
            var camClip = clip as CameraAnimationClip;
            if (camClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 相机预制体与骨骼绑定卡片
            _showCamera = EditorGUILayout.Foldout(_showCamera, "动画相机与轨道绑定", true, EditorStyles.foldoutHeader);
            if (_showCamera)
            {
                EditorGUILayout.BeginVertical("box");
                camClip.cameraPrefab = (GameObject)EditorGUILayout.ObjectField("相机预制体", camClip.cameraPrefab, typeof(GameObject), false);
                camClip.timelineAsset = (PlayableAsset)EditorGUILayout.ObjectField("Timeline 资产", camClip.timelineAsset, typeof(PlayableAsset), false);
                camClip.followBoneName = EditorGUILayout.TextField("跟拍骨骼名", camClip.followBoneName);
                camClip.lookAtBoneName = EditorGUILayout.TextField("看向骨骼名", camClip.lookAtBoneName);
                EditorGUILayout.EndVertical();
            }

            // 3. 渲染环境覆盖卡片
            _showOverride = EditorGUILayout.Foldout(_showOverride, "渲染设置覆盖", true, EditorStyles.foldoutHeader);
            if (_showOverride)
            {
                EditorGUILayout.BeginVertical("box");
                camClip.overrideSettings = EditorGUILayout.Toggle("启用渲染覆盖", camClip.overrideSettings);
                if (camClip.overrideSettings)
                {
                    camClip.backgroundColor = EditorGUILayout.ColorField("背景颜色", camClip.backgroundColor);
                    int currentMask = UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(camClip.cullingMask);
                    int newMask = EditorGUILayout.MaskField("渲染层级", currentMask, UnityEditorInternal.InternalEditorUtility.layers);
                    camClip.cullingMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newMask);
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Camera Animation Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Camera Animation Clip");
            }
        }
    }

    /// <summary>
    /// 常规相机控制片段抽屉 (CameraControlClip)
    /// </summary>
    [CustomDrawer(typeof(CameraControlClip))]
    public class CameraControlClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showModeParams = true;

        public override void DrawInspector(ClipBase clip)
        {
            var controlClip = clip as CameraControlClip;
            if (controlClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 控制模式与单选配置卡片
            _showModeParams = EditorGUILayout.Foldout(_showModeParams, "相机控制配置", true, EditorStyles.foldoutHeader);
            if (_showModeParams)
            {
                EditorGUILayout.BeginVertical("box");

                // 单选模式选择器
                controlClip.controlMode = (CameraControlMode)EditorGUILayout.EnumPopup("控制模式", controlClip.controlMode);
                EditorGUILayout.Space(4);

                switch (controlClip.controlMode)
                {
                    case CameraControlMode.LockRotation:
                        EditorGUILayout.HelpBox("【锁定旋转】：在技能期间冻结相机的自由旋转输入，防止玩家滑动视角破坏演出或造成朝向偏离。", MessageType.Info);
                        controlClip.lockYaw = EditorGUILayout.Toggle("锁定水平偏航", controlClip.lockYaw);
                        controlClip.lockPitch = EditorGUILayout.Toggle("锁定垂直俯仰", controlClip.lockPitch);
                        controlClip.unlockOnExit = EditorGUILayout.Toggle("退出片段时自动解锁", controlClip.unlockOnExit);
                        break;

                    case CameraControlMode.Recenter:
                        EditorGUILayout.HelpBox("【相机平滑回正】：基于二阶临界阻尼（SmoothDamp）与绝区零智能构图算法，平滑旋转主相机视角。", MessageType.Info);
                        controlClip.recenterTarget = (CameraRecenterTarget)EditorGUILayout.EnumPopup("回正基准方向", controlClip.recenterTarget);
                        controlClip.smoothTime = EditorGUILayout.Slider("平滑阻尼时间", controlClip.smoothTime, 0.05f, 1.0f);
                        controlClip.targetPitch = EditorGUILayout.FloatField(new GUIContent("目标俯仰角", "目标俯仰角（度）。如设置为 -999 则保持当前相机的俯仰角不变。绝区零黄金视角推荐 10°~14°。"), controlClip.targetPitch);
                        
                        if (controlClip.recenterTarget == CameraRecenterTarget.CombatFraming)
                        {
                            controlClip.framingBiasAngle = EditorGUILayout.Slider(new GUIContent("对峙构图偏角", "负数使角色偏左下，怪物偏中右（推荐 -8°）；正数偏右下；0为居中。"), controlClip.framingBiasAngle, -30.0f, 30.0f);
                        }

                        controlClip.deadzoneAngle = EditorGUILayout.Slider(new GUIContent("角度死区", "偏差小于该死区时不触发微调，防止视觉抖动。推荐 1.5°。"), controlClip.deadzoneAngle, 0.1f, 5.0f);
                        controlClip.allowSoftInputInterrupt = EditorGUILayout.Toggle(new GUIContent("允许玩家软输入让权", "玩家在回正中滑动鼠标/摇杆时，自适应平滑让权给玩家，避免硬拔河。"), controlClip.allowSoftInputInterrupt);
                        controlClip.disableInputDuringRecenter = EditorGUILayout.Toggle(new GUIContent("回正期间强制禁用输入", "若勾选则完全锁死旋转输入直到回正结束。"), controlClip.disableInputDuringRecenter);
                        controlClip.unlockInputOnExit = EditorGUILayout.Toggle("退出片段时解锁输入", controlClip.unlockInputOnExit);
                        break;

                    case CameraControlMode.LookAtTarget:
                        EditorGUILayout.HelpBox("【注视目标】：将相机视角平滑对齐并注视当前战斗锁定目标。", MessageType.Info);
                        controlClip.lookAtOffset = EditorGUILayout.Vector3Field("注视点局部偏移", controlClip.lookAtOffset);
                        controlClip.trackSmoothSpeed = EditorGUILayout.Slider("追踪平滑速度", controlClip.trackSmoothSpeed, 1.0f, 25.0f);
                        controlClip.fallbackToCharacter = EditorGUILayout.Toggle("无目标时回正角色", controlClip.fallbackToCharacter);
                        controlClip.restoreLookAtOnExit = EditorGUILayout.Toggle("退出时恢复注视", controlClip.restoreLookAtOnExit);
                        break;

                    case CameraControlMode.FovTransition:
                        EditorGUILayout.HelpBox("【镜头变焦/FOV特写】：动态平滑拉近或拉远镜头 FOV 与镜头距离。", MessageType.Info);
                        controlClip.targetFOV = EditorGUILayout.Slider("目标 FOV", controlClip.targetFOV, 20.0f, 100.0f);
                        controlClip.targetDistance = EditorGUILayout.FloatField("目标相机距离", controlClip.targetDistance);
                        controlClip.blendInSpeed = EditorGUILayout.Slider("进入过渡速度", controlClip.blendInSpeed, 0.5f, 20.0f);
                        controlClip.blendOutSpeed = EditorGUILayout.Slider("退出还原速度", controlClip.blendOutSpeed, 0.5f, 20.0f);
                        controlClip.restoreOnExit = EditorGUILayout.Toggle("退出时还原初始状态", controlClip.restoreOnExit);
                        break;
                }

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Camera Control Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Camera Control Clip");
            }
        }
    }

    /// <summary>
    /// 震屏脉冲片段抽屉 (CameraImpluseClip)
    /// </summary>
    [CustomDrawer(typeof(CameraImpluseClip))]
    public class CameraImpulseClipDrawer : ClipDrawer
    {
        private static bool _showBase = true;
        private static bool _showImpulse = true;

        public override void DrawInspector(ClipBase clip)
        {
            var impulseClip = clip as CameraImpluseClip;
            if (impulseClip == null) return;

            EditorGUI.BeginChangeCheck();

            // 1. 基础信息卡片
            DrawBaseClipCard(clip, ref _showBase, "基础信息");

            // 2. 脉冲参数卡片
            _showImpulse = EditorGUILayout.Foldout(_showImpulse, "震屏与冲力参数", true, EditorStyles.foldoutHeader);
            if (_showImpulse)
            {
                EditorGUILayout.BeginVertical("box");
                impulseClip.velocity = EditorGUILayout.Vector3Field("冲击方向速度", impulseClip.velocity);
                impulseClip.force = EditorGUILayout.FloatField("强度系数", impulseClip.force);
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Modify Camera Impulse Clip");
                    foreach (var ctx in UndoContext) EditorUtility.SetDirty(ctx);
                }
                MarkTimelineDirty("Modify Camera Impulse Clip");
            }
        }
    }
}
