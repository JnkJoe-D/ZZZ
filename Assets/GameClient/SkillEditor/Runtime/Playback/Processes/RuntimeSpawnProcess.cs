using UnityEngine;

namespace SkillEditor
{
    [ProcessBinding(typeof(SpawnClip), PlayMode.Runtime)]
    public class RuntimeSpawnProcess : ProcessBase<SpawnClip>
    {
        private ISkillSpawnHandler spawnHandler;
        private ISkillProjectileHandler spawnedProjectile;

        public override void OnEnable()
        {
            spawnHandler = context.GetService<ISkillSpawnHandler>();
        }

        public override void OnEnter()
        {
            if (spawnHandler == null || clip.prefab == null) return;

            GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent);

            var spawnData = new SpawnData
            {
                configPrefab = clip.prefab,
                position = pos,
                rotation = rot,
                detach = clip.detach,
                parent = clip.detach ? null : parent,
                eventTag = clip.eventTag,
                targetTags = clip.targetTags,
                deployer = context.Owner
            };

            spawnedProjectile = spawnHandler.Spawn(spawnData);
            
            // 下发上下文初始化信息给业务端
            spawnedProjectile?.Initialize(spawnData, spawnHandler);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // SpawnProcess 作为纯种的"产出器"，在这里不负责强行接管投射物的位移
            // 实体投射物的运动应该由生成的实体自身(或被注入的组件如Rigidbody/Dotween/ProjectileController)完全接管
        }

        public override void OnExit()
        {
            // 如果技能被打断，且要求打断时连带销毁产生物
            // 依赖于 SkillRunner 触发的 InterruptInternal 和 IsInterrupted 标记
            if (clip.destroyOnInterrupt && spawnedProjectile != null && context != null && context.IsInterrupted)
            {
                spawnedProjectile.Recycle();
            }
            spawnedProjectile = null;
        }

        private void GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent)
        {
            parent = null;
            if (context != null)
            {
                var actor = context.GetService<ISkillBoneGetter>();
                parent = actor.GetBone(clip.bindPoint);
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

        public override void Reset()
        {
            base.Reset();
            spawnHandler = null;
            spawnedProjectile = null;
        }
    }
}
