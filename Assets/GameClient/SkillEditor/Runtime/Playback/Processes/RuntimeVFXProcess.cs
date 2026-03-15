using System.Collections;
using UnityEngine;

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
        private ISkillBoneGetter boneGetter;
        private bool _returnQueued;

        public override void OnEnable()
        {
            vfxHanlder = context.GetService<ISkillVFXHandler>();
            boneGetter = context.GetService<ISkillBoneGetter>();
        }

        public override void OnEnter()
        {
            if (clip.effectPrefab == null) return;

            _returnQueued = false;

            Transform targetTransform = null;
            if (boneGetter != null)
            {
                targetTransform = boneGetter.GetBone(clip.bindPoint, clip.customBoneName);
            }

            if (targetTransform == null && context.OwnerTransform != null)
            {
                targetTransform = context.OwnerTransform;
            }

            Vector3 spawnPos = targetTransform != null ? targetTransform.position : Vector3.zero;
            Quaternion spawnRot = targetTransform != null ? targetTransform.rotation : Quaternion.identity;
            Transform parent = clip.followTarget ? targetTransform : null;

            if (vfxHanlder != null)
            {
                vfxInstance = vfxHanlder.Spawn(clip.effectPrefab, spawnPos, spawnRot, parent);
            }

            if (vfxInstance == null) return;

            vfxInstance.transform.localScale = clip.scale;

            if (clip.followTarget)
            {
                vfxInstance.transform.localPosition += clip.positionOffset;
                vfxInstance.transform.localRotation *= Quaternion.Euler(clip.rotationOffset);
            }
            else if (targetTransform != null)
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

            SyncSpeed(context.GlobalPlaySpeed);
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            SyncSpeed(context.GlobalPlaySpeed);
        }

        private void SyncSpeed(float speed)
        {
            if (speed < 0f || particleInfos == null) return;

            for (int i = 0; i < particleInfos.Length; i++)
            {
                ParticleSpeedInfo info = particleInfos[i];
                if (info.ps == null) continue;

                var main = info.ps.main;
                main.simulationSpeed = info.initialSpeed * speed;
            }
        }

        public override void OnExit()
        {
            HandleVFXRelease();
        }

        public override void OnDisable()
        {
            HandleVFXRelease();
        }

        public override void Reset()
        {
            base.Reset();
            particleInfos = null;
            vfxInstance = null;
            vfxHanlder = null;
            boneGetter = null;
            _returnQueued = false;
        }

        private void HandleVFXRelease()
        {
            if (vfxInstance == null || _returnQueued) return;

            _returnQueued = true;
            SyncSpeed(1f);

            if (clip.destroyOnEnd)
            {
                if (clip.stopEmissionOnEnd)
                {
                    ReturnVFXDelay(true);
                }
                else
                {
                    ReturnVFX();
                }
            }
            else
            {
                ReturnVFXDelay(false);
            }
        }

        private void ReturnVFX()
        {
            if (vfxInstance == null) return;

            if (vfxHanlder != null)
            {
                vfxHanlder.Return(vfxInstance);
            }
            else
            {
                Object.Destroy(vfxInstance);
            }

            vfxInstance = null;
            particleInfos = null;
        }

        private void ReturnVFXDelay(bool stopEmissionOnEnd)
        {
            if (vfxInstance == null || vfxHanlder == null)
            {
                ReturnVFX();
                return;
            }

            var particles = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            float maxLifetime = 0f;
            foreach (var ps in particles)
            {
                if (stopEmissionOnEnd)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                if (ps.main.startLifetime.constantMax > maxLifetime)
                {
                    maxLifetime = ps.main.startLifetime.constantMax;
                }
            }

            GameObject inst = vfxInstance;
            vfxInstance = null;
            particleInfos = null;
            vfxHanlder.ReturnVFXDelay(inst, maxLifetime);
        }
    }
}
