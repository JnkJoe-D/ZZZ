using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(PhysicsTrack), "物理控制")]
    public class PhysicsClip : ClipBase, ISerializationCallbackReceiver
    {
        [Header("碰撞与层级控制")]
        [SkillProperty("修改忽略碰撞层级")]
        public bool modifyExcludeLayers = true;

        [SkillProperty("忽略的碰撞层级")]
        [ShowIf("modifyExcludeLayers", true)]
        public LayerMask excludeLayers;

        [SkillProperty("退出时还原忽略层级")]
        [ShowIf("modifyExcludeLayers", true)]
        public bool restoreExcludeLayersOnExit = true;

        [SkillProperty("修改碰撞体开关")]
        public bool modifyCollisionEnabled = false;

        [SkillProperty("碰撞体启用状态")]
        [ShowIf("modifyCollisionEnabled", true)]
        public bool isCollisionEnabled = false;

        [SkillProperty("退出时还原碰撞体状态")]
        [ShowIf("modifyCollisionEnabled", true)]
        public bool restoreCollisionOnExit = true;

        [Header("重力与空中滞空控制")]
        [SkillProperty("修改重力倍率")]
        public bool modifyGravity = false;

        [SkillProperty("重力倍率")]
        [ShowIf("modifyGravity", true)]
        public float gravityScale = 0f; // 0 = 完全滞空无重力

        [SkillProperty("进入时清空垂直下落动量")]
        [ShowIf("modifyGravity", true)]
        public bool resetVerticalVelocityOnEnter = true;

        [SkillProperty("退出时还原重力倍率")]
        [ShowIf("modifyGravity", true)]
        public bool restoreGravityOnExit = true;

        [Header("推挤与霸体抗性控制")]
        [SkillProperty("修改推挤抗性")]
        public bool modifyPushResistance = false;

        [SkillProperty("推挤抗性 (0~1)")]
        [ShowIf("modifyPushResistance", true)]
        public float pushResistance = 1.0f; // 1 = 完全免疫推挤

        [SkillProperty("退出时还原推挤抗性")]
        [ShowIf("modifyPushResistance", true)]
        public bool restorePushResistanceOnExit = true;

        [SerializeField, HideInInspector]
        private int serializedExcludeLayers;

        public PhysicsClip()
        {
            clipName = "Physics Clip";
            duration = 0.5f;
            excludeLayers = 0;
        }

        public override ClipBase Clone()
        {
            return new PhysicsClip
            {
                clipId = Guid.NewGuid().ToString(),
                clipName = this.clipName,
                startTime = this.startTime,
                duration = this.duration,
                isEnabled = this.isEnabled,
                modifyExcludeLayers = this.modifyExcludeLayers,
                excludeLayers = this.excludeLayers,
                restoreExcludeLayersOnExit = this.restoreExcludeLayersOnExit,
                modifyCollisionEnabled = this.modifyCollisionEnabled,
                isCollisionEnabled = this.isCollisionEnabled,
                restoreCollisionOnExit = this.restoreCollisionOnExit,
                modifyGravity = this.modifyGravity,
                gravityScale = this.gravityScale,
                resetVerticalVelocityOnEnter = this.resetVerticalVelocityOnEnter,
                restoreGravityOnExit = this.restoreGravityOnExit,
                modifyPushResistance = this.modifyPushResistance,
                pushResistance = this.pushResistance,
                restorePushResistanceOnExit = this.restorePushResistanceOnExit
            };
        }

        public void OnBeforeSerialize()
        {
            serializedExcludeLayers = excludeLayers.value;
        }

        public void OnAfterDeserialize()
        {
            excludeLayers = serializedExcludeLayers;
        }
    }
}
