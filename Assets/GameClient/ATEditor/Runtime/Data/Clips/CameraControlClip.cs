using System;
using UnityEngine;

namespace ATEditor
{
    /// <summary>
    /// 相机常规控制模式枚举（单选）
    /// </summary>
    public enum CameraControlMode
    {
        [InspectorName("锁定旋转")]
        LockRotation = 0,

        [InspectorName("相机回正")]
        Recenter = 1,

        [InspectorName("注视目标")]
        LookAtTarget = 2,

        [InspectorName("镜头特写与变焦")]
        FovTransition = 3
    }

    /// <summary>
    /// 相机回正基准方向预设
    /// </summary>
    public enum CameraRecenterTarget
    {
        [InspectorName("智能战斗对峙")]
        CombatFraming = 0,

        [InspectorName("角色背后主视角")]
        CharacterBack = 1,

        [InspectorName("移动突进方向")]
        MovementDirection = 2,

        [InspectorName("直接正对目标")]
        TargetDirection = 3
    }

    /// <summary>
    /// 常规相机控制片段：用于技能期间控制主虚拟相机的旋转锁定、视角平滑回正、注视目标以及镜头拉近/变焦
    /// </summary>
    [Serializable]
    [ClipDefinition(typeof(CameraTrack), "相机控制")]
    public class CameraControlClip : ClipBase
    {
        [Header("核心控制模式 (单选)")]
        [SkillProperty("控制模式")]
        public CameraControlMode controlMode = CameraControlMode.LockRotation;

        #region 1. 锁定旋转参数
        [Header("锁定旋转参数")]
        [SkillProperty("退出片段时自动解锁")]
        [ShowIf("controlMode", CameraControlMode.LockRotation)]
        public bool unlockOnExit = true;

        [SkillProperty("锁定水平偏航")]
        [ShowIf("controlMode", CameraControlMode.LockRotation)]
        public bool lockYaw = true;

        [SkillProperty("锁定垂直俯仰")]
        [ShowIf("controlMode", CameraControlMode.LockRotation)]
        public bool lockPitch = true;
        #endregion

        #region 2. 视角回正参数
        [Header("视角回正参数")]
        [SkillProperty("回正基准方向")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        public CameraRecenterTarget recenterTarget = CameraRecenterTarget.CombatFraming;

        [SkillProperty("平滑阻尼时间 (秒)")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        [Tooltip("二阶临界阻尼时间，数值越小回正越快越紧凑，推荐 0.2~0.35s")]
        public float smoothTime = 0.25f;

        [SkillProperty("目标俯仰角")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        [Tooltip("目标俯仰角（度），绝区零推荐黄金俯角 10°~14°。如设置为 -999 则保持当前相机的俯仰角不变。")]
        public float targetPitch = 12.0f;

        [SkillProperty("对峙构图侧向偏角")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        [Tooltip("仅在 CombatFraming 模式下生效。负数使角色偏左下，正数偏右下，0为居中。推荐 -8°")]
        public float framingBiasAngle = -8.0f;

        [SkillProperty("角度死区")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        [Tooltip("小于该角度偏差时不触发微调，消除视觉抖动。推荐 1.5°")]
        public float deadzoneAngle = 1.5f;

        [SkillProperty("允许玩家输入软打断/融合")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        public bool allowSoftInputInterrupt = true;

        [SkillProperty("回正期间禁用旋转输入")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        [Tooltip("若为 true 则完全禁用玩家输入；若为 false 配合软输入融合可在玩家滑动时自适应让权")]
        public bool disableInputDuringRecenter = false;

        [SkillProperty("退出片段时解锁输入")]
        [ShowIf("controlMode", CameraControlMode.Recenter)]
        public bool unlockInputOnExit = true;
        #endregion

        #region 3. 注视目标参数 (LookAtTarget)
        [Header("注视目标参数")]
        [SkillProperty("注视点局部偏移")]
        [ShowIf("controlMode", CameraControlMode.LookAtTarget)]
        public Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);

        [SkillProperty("追踪平滑速度")]
        [ShowIf("controlMode", CameraControlMode.LookAtTarget)]
        public float trackSmoothSpeed = 8.0f;

        [SkillProperty("无目标时回正角色")]
        [ShowIf("controlMode", CameraControlMode.LookAtTarget)]
        public bool fallbackToCharacter = true;

        [SkillProperty("退出时恢复控制")]
        [ShowIf("controlMode", CameraControlMode.LookAtTarget)]
        public bool restoreLookAtOnExit = true;
        #endregion

        #region 4. FOV/距离特写参数 (FovTransition)
        [Header("FOV 与距离特写参数")]
        [SkillProperty("目标 FOV")]
        [ShowIf("controlMode", CameraControlMode.FovTransition)]
        public float targetFOV = 45.0f;

        [SkillProperty("目标相机距离")]
        [ShowIf("controlMode", CameraControlMode.FovTransition)]
        [Tooltip("若 <= 0 则保持当前距离不变")]
        public float targetDistance = 3.5f;

        [SkillProperty("进入过渡速度")]
        [ShowIf("controlMode", CameraControlMode.FovTransition)]
        public float blendInSpeed = 5.0f;

        [SkillProperty("退出还原速度")]
        [ShowIf("controlMode", CameraControlMode.FovTransition)]
        public float blendOutSpeed = 5.0f;

        [SkillProperty("退出时还原初始状态")]
        [ShowIf("controlMode", CameraControlMode.FovTransition)]
        public bool restoreOnExit = true;
        #endregion

        public CameraControlClip()
        {
            clipName = "Camera Control Clip";
            duration = 0.5f;
        }

        public override ClipBase Clone()
        {
            return new CameraControlClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                controlMode = this.controlMode,
                // Lock
                unlockOnExit = this.unlockOnExit,
                lockYaw = this.lockYaw,
                lockPitch = this.lockPitch,
                // Recenter
                recenterTarget = this.recenterTarget,
                smoothTime = this.smoothTime,
                targetPitch = this.targetPitch,
                framingBiasAngle = this.framingBiasAngle,
                deadzoneAngle = this.deadzoneAngle,
                allowSoftInputInterrupt = this.allowSoftInputInterrupt,
                disableInputDuringRecenter = this.disableInputDuringRecenter,
                unlockInputOnExit = this.unlockInputOnExit,
                // LookAt
                lookAtOffset = this.lookAtOffset,
                trackSmoothSpeed = this.trackSmoothSpeed,
                fallbackToCharacter = this.fallbackToCharacter,
                restoreLookAtOnExit = this.restoreLookAtOnExit,
                // FOV
                targetFOV = this.targetFOV,
                targetDistance = this.targetDistance,
                blendInSpeed = this.blendInSpeed,
                blendOutSpeed = this.blendOutSpeed,
                restoreOnExit = this.restoreOnExit
            };
        }
    }
}
