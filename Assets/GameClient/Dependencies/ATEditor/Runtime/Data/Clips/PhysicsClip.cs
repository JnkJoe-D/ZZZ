using System;
using UnityEngine;

namespace ATEditor
{
    [Serializable]
    [ClipDefinition(typeof(PhysicsTrack), "物理控制")]
    public class PhysicsClip : ClipBase, ISerializationCallbackReceiver
    {
        [Header("碰撞与层级控制")]
        [ActionProperty("修改忽略碰撞层级")]
        public bool modifyExcludeLayers = true;

        [ActionProperty("忽略的碰撞层级")]
        [ATShowIf("modifyExcludeLayers", true)]
        public LayerMask excludeLayers;

        [ActionProperty("退出时还原忽略层级")]
        [ATShowIf("modifyExcludeLayers", true)]
        public bool restoreExcludeLayersOnExit = true;

        [ActionProperty("修改碰撞体开关")]
        public bool modifyCollisionEnabled = false;

        [ActionProperty("碰撞体启用状态")]
        [ATShowIf("modifyCollisionEnabled", true)]
        public bool isCollisionEnabled = false;

        [ActionProperty("退出时还原碰撞体状态")]
        [ATShowIf("modifyCollisionEnabled", true)]
        public bool restoreCollisionOnExit = true;

        [Header("重力与空中滞空控制")]
        [ActionProperty("修改重力倍率")]
        public bool modifyGravity = false;

        [ActionProperty("重力倍率")]
        [ATShowIf("modifyGravity", true)]
        public float gravityScale = 0f; // 0 = 完全滞空无重力

        [ActionProperty("进入时清空垂直下落动量")]
        [ATShowIf("modifyGravity", true)]
        public bool resetVerticalVelocityOnEnter = true;

        [ActionProperty("退出时还原重力倍率")]
        [ATShowIf("modifyGravity", true)]
        public bool restoreGravityOnExit = true;

        [Header("推挤与霸体抗性控制")]
        [ActionProperty("修改推挤抗性")]
        public bool modifyPushResistance = false;

        [ActionProperty("推挤抗性 (0~1)")]
        [ATShowIf("modifyPushResistance", true)]
        public float pushResistance = 1.0f; // 1 = 完全免疫推挤

        [ActionProperty("退出时还原推挤抗性")]
        [ATShowIf("modifyPushResistance", true)]
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
