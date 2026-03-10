using UnityEngine;
using System.Collections;

namespace SkillEditor
{
    [ProcessBinding(typeof(VFXClip), PlayMode.Runtime)]
    public class RuntimeVFXProcess : ProcessBase<VFXClip>
    {
        private struct ParticleSpeedInfo
        {
            public ParticleSystem ps;
            public float initialSpeed;
        }
        private ParticleSpeedInfo[] particleInfos;
        private GameObject vfxInstance;
        private ISkillVFXHandler vfxHanlder;
        public override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnEnter()
        {
            Debug.Log($"[RuntimeVFXProcess] OnEnter at time: {UnityEngine.Time.time}");
            if (clip.effectPrefab == null) return;

            // 1. 获取挂点
            Transform targetTransform = null;
            // 懒加载获取 actor 服务
            var actor = context.GetService<ISkillBoneGetter>();
            if (actor != null)
            {
                targetTransform = actor.GetBone(clip.bindPoint, clip.customBoneName);
            }
            
            // 降级处理
            if (targetTransform == null)
            {
                if (context.OwnerTransform != null) targetTransform = context.OwnerTransform;
            }

            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (targetTransform != null)
            {
                spawnPos = targetTransform.position;
                spawnRot = targetTransform.rotation;
            }

            // 2. 实例化
            Transform parent = clip.followTarget ? targetTransform : null;
            vfxHanlder = context.GetService<ISkillVFXHandler>();
            if (vfxHanlder != null)
            {
                vfxInstance = vfxHanlder.Spawn(clip.effectPrefab, spawnPos, spawnRot, parent);
            }
            else
            {
                // 降级处理：无池服务时直接实例化
                // vfxInstance = Object.Instantiate(clip.effectPrefab, spawnPos, spawnRot, parent);
            }

            if (vfxInstance != null)
            {
                // 3. 应用变换
                vfxInstance.transform.localScale = clip.scale;

                if (clip.followTarget)
                {
                    vfxInstance.transform.localPosition += clip.positionOffset;
                    vfxInstance.transform.localRotation *= Quaternion.Euler(clip.rotationOffset);
                }
                else
                {
                    if (targetTransform != null)
                    {
                        Vector3 finalPos = targetTransform.position + targetTransform.rotation * clip.positionOffset;
                        Quaternion finalRot = targetTransform.rotation * Quaternion.Euler(clip.rotationOffset);
                        vfxInstance.transform.SetPositionAndRotation(finalPos, finalRot);
                    }
                    else
                    {
                        vfxInstance.transform.position += clip.positionOffset;
                        vfxInstance.transform.rotation *= Quaternion.Euler(clip.rotationOffset);
                    }
                }

                // 4. 缓存粒子信息用于速度同步
                var systems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
                particleInfos = new ParticleSpeedInfo[systems.Length];
                for (int i = 0; i < systems.Length; i++)
                {
                    particleInfos[i] = new ParticleSpeedInfo
                    {
                        ps = systems[i],
                        initialSpeed = systems[i].main.simulationSpeed
                    };
                }

                // 立即同步一次速度
                SyncSpeed(context.GlobalPlaySpeed);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            SyncSpeed(context.GlobalPlaySpeed);
        }

        private void SyncSpeed(float speed)
        {
            if(speed<0)return;
            if (particleInfos == null) return;

            for (int i = 0; i < particleInfos.Length; i++)
            {
                var info = particleInfos[i];
                if (info.ps != null)
                {
                    var main = info.ps.main;
                    main.simulationSpeed = info.initialSpeed * speed;
                }
            }
        }

        public override void OnExit()
        {
            Debug.Log($"[RuntimeVFXProcess] OnExit at time: {UnityEngine.Time.time}");
            if (vfxInstance == null) return;

            if (clip.destroyOnEnd) //跟随片段结束
            {
                //重置速度
                SyncSpeed(1f);
                if (clip.stopEmissionOnEnd)
                {
                    ReturnVFXDelay(clip.stopEmissionOnEnd);
                }
                else
                {
                    // 硬结束
                    ReturnVFX();
                }
            }
            else
            {
                ReturnVFXDelay(false);
            }
        }
        public override void OnDisable() 
        {
            
        }
        public override void Reset()
         {
            base.Reset();
            particleInfos = null;
            vfxInstance = null;
            vfxHanlder = null;
        }

        /// <summary>
        /// 归还 VFX 实例（优先通过池服务，降级为直接销毁）
        /// </summary>
        private void ReturnVFX()
        {
            if (vfxHanlder != null)
            {
                vfxHanlder.Return(vfxInstance);
            }
            else
            {
                Object.Destroy(vfxInstance);
            }
        }
            
        private void ReturnVFXDelay(bool stopEmissionOnEnd)
        {
            // 软结束
            var particles = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            float maxLifetime = 0f;
            foreach (var ps in particles)
            {
                if(stopEmissionOnEnd) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (ps.main.startLifetime.constantMax > maxLifetime)
                    maxLifetime = ps.main.startLifetime.constantMax;
            }

            vfxHanlder.ReturnVFXDelay(vfxInstance, maxLifetime);
        }
    }
}
