using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(PhysicsClip), PlayMode.Runtime)]
    [ProcessBinding(typeof(PhysicsClip), PlayMode.EditorPreview)]
    public class RuntimePhysicsProcess : ProcessBase<PhysicsClip>
    {
        private IPhysicsHandler physicsHandler;

        private LayerMask originalExcludeLayers;
        private bool originalCollisionEnabled = true;
        private float originalGravityScale = 1.0f;
        private float originalPushResistance = 0f;

        private bool hasModifiedExcludeLayers = false;
        private bool hasModifiedCollision = false;
        private bool hasModifiedGravity = false;
        private bool hasModifiedPushResistance = false;

        public override void OnEnable()
        {
            physicsHandler = context.GetService<IPhysicsHandler>();
        }

        public override void OnEnter()
        {
            if (physicsHandler == null)
            {
                physicsHandler = context.GetService<IPhysicsHandler>();
            }
            if (physicsHandler == null) return;

            // 1. 忽略碰撞层级控制
            if (clip.modifyExcludeLayers)
            {
                originalExcludeLayers = physicsHandler.GetExcludeLayers();
                physicsHandler.SetExcludeLayers(clip.excludeLayers);
                hasModifiedExcludeLayers = true;
            }

            // 2. 碰撞体启用状态控制
            if (clip.modifyCollisionEnabled)
            {
                originalCollisionEnabled = physicsHandler.GetCollisionEnabled();
                physicsHandler.SetCollisionEnabled(clip.isCollisionEnabled);
                hasModifiedCollision = true;
            }

            // 3. 重力与滞空控制
            if (clip.modifyGravity)
            {
                originalGravityScale = physicsHandler.GetGravityScale();
                physicsHandler.SetGravityScale(clip.gravityScale);
                if (clip.resetVerticalVelocityOnEnter)
                {
                    physicsHandler.ResetVerticalVelocity();
                }
                hasModifiedGravity = true;
            }

            // 4. 推挤抗性控制
            if (clip.modifyPushResistance)
            {
                originalPushResistance = physicsHandler.GetPushResistance();
                physicsHandler.SetPushResistance(clip.pushResistance);
                hasModifiedPushResistance = true;
            }

            // 注册系统级兜底清理，确保技能被打断或强制结束时物理设置必然被还原
            string cleanupKey = "Physics_" + (clip != null ? clip.clipId : GetHashCode().ToString());
            context?.RegisterCleanup(cleanupKey, RestorePhysicsState);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 物理片段主要为状态切换，若需持续控制逻辑可在此扩展
        }

        public override void OnExit()
        {
            RestorePhysicsState();
        }

        public override void OnDisable()
        {
            RestorePhysicsState();
        }

        public override void Reset()
        {
            base.Reset();
            physicsHandler = null;
            hasModifiedExcludeLayers = false;
            hasModifiedCollision = false;
            hasModifiedGravity = false;
            hasModifiedPushResistance = false;
        }

        private void RestorePhysicsState()
        {
            if (physicsHandler == null) return;

            // 还原碰撞忽略层级
            if (hasModifiedExcludeLayers && (clip == null || clip.restoreExcludeLayersOnExit))
            {
                physicsHandler.SetExcludeLayers(originalExcludeLayers);
                hasModifiedExcludeLayers = false;
            }

            // 还原碰撞体开关
            if (hasModifiedCollision && (clip == null || clip.restoreCollisionOnExit))
            {
                physicsHandler.SetCollisionEnabled(originalCollisionEnabled);
                hasModifiedCollision = false;
            }

            // 还原重力倍率
            if (hasModifiedGravity && (clip == null || clip.restoreGravityOnExit))
            {
                physicsHandler.SetGravityScale(originalGravityScale);
                hasModifiedGravity = false;
            }

            // 还原推挤抗性
            if (hasModifiedPushResistance && (clip == null || clip.restorePushResistanceOnExit))
            {
                physicsHandler.SetPushResistance(originalPushResistance);
                hasModifiedPushResistance = false;
            }
        }
    }
}
