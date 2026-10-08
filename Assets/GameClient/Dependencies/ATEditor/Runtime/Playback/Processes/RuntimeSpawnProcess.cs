using UnityEngine;

namespace ATEditor
{
    [ProcessBinding(typeof(SpawnClip), PlayMode.Runtime)]
    public class RuntimeSpawnProcess : ProcessBase<SpawnClip>
    {
        private ISpawnHandler spawnHandler;
        private ISpawnObject spawnedProjectile;

        public override void OnEnable()
        {
            spawnHandler = context.GetService<ISpawnHandler>();
        }

        public override void OnEnter()
        {
            if (spawnHandler == null || clip.prefab == null) return;

            GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent);

            bool detach = clip.bindConfig != null ? !clip.bindConfig.followTarget : true;

            var spawnData = new SpawnData
            {
                configPrefab = clip.prefab,
                position = pos,
                rotation = rot,
                detach = detach,
                parent = detach ? null : parent,
                eventTag = clip.eventTag,
                targetTags = clip.targetTags,
                deployer = context.Owner,

                bindConfig = clip.bindConfig,
                lifecycleConfig = clip.lifecycleConfig,
                movementConfig = clip.movementConfig,
                enableAttackDetection = clip.enableAttackDetection,
                hitBoxScope = clip.hitBoxScope,
                attackPolicy = clip.attackPolicy,
                hitHandler = context.GetService<IHitHandler>()
            };

            spawnedProjectile = spawnHandler.Spawn(spawnData);
            
            // 下发上下文初始化信息给业务端
            spawnedProjectile?.Initialize(spawnData, spawnHandler);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            // 生成物实体自主接管移动、判定与生命周期
        }

        public override void OnExit()
        {
            bool destroyOnInterrupt = clip.lifecycleConfig != null && clip.lifecycleConfig.destroyOnInterrupt;
            if (destroyOnInterrupt && spawnedProjectile != null && context != null && context.IsInterrupted)
            {
                spawnedProjectile.Recycle();
            }
            spawnedProjectile = null;
        }

        public override void OnStop()
        {
            if (context != null && context.Owner != null && !context.Owner.gameObject.scene.isLoaded)
            {
                spawnedProjectile = null;
                return;
            }

            bool destroyOnInterrupt = clip.lifecycleConfig != null && clip.lifecycleConfig.destroyOnInterrupt;
            if (destroyOnInterrupt && spawnedProjectile != null && context != null && context.IsInterrupted)
            {
                spawnedProjectile.Recycle();
            }
            spawnedProjectile = null;
        }

        private void GetMatrix(out Vector3 pos, out Quaternion rot, out Transform parent)
        {
            parent = null;
            var bindConfig = clip.bindConfig ?? new TransformBindConfig();

            if (context != null)
            {
                var actor = context.GetService<IBoneGetter>();
                parent = actor?.GetBone(bindConfig.bindPoint, bindConfig.customBoneName);
            }

            if (parent != null)
            {
                pos = parent.position + parent.rotation * bindConfig.positionOffset;
                rot = parent.rotation * Quaternion.Euler(bindConfig.rotationOffset);
            }
            else if (context?.Owner != null)
            {
                pos = context.Owner.transform.position + context.Owner.transform.rotation * bindConfig.positionOffset;
                rot = context.Owner.transform.rotation * Quaternion.Euler(bindConfig.rotationOffset);
            }
            else
            {
                pos = bindConfig.positionOffset;
                rot = Quaternion.Euler(bindConfig.rotationOffset);
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
