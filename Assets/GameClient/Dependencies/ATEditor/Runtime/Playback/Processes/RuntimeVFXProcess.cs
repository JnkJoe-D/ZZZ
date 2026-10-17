using System.Collections;
using UnityEngine;

namespace ATEditor
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
        private IVFXHandler vfxHanlder;
        private IBoneGetter boneGetter;
        private bool _returnQueued;

        public override void OnEnable()
        {
            vfxHanlder = context.GetService<IVFXHandler>();
            boneGetter = context.GetService<IBoneGetter>();
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
            spawnPos = parent != null ? parent.TransformPoint(clip.positionOffset) : spawnPos + spawnRot * clip.positionOffset;
            spawnRot *= Quaternion.Euler(clip.rotationOffset);

            if (vfxHanlder != null)
            {
                vfxInstance = vfxHanlder.Spawn(clip.effectPrefab, spawnPos, spawnRot, parent, clip.scale);
            }

            if (vfxInstance == null) return;

            vfxInstance.transform.localScale = clip.scale;

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

            SyncSpeed(context.PresentationPlaySpeed);
            vfxHanlder?.SetTime(vfxInstance, Mathf.Max(0f, context.CurrentTime - clip.StartTime));

            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
                context.OnPresentationSpeedChanged += HandlePresentationSpeedChanged;
            }
        }

        private void HandlePresentationSpeedChanged(float newSpeed)
        {
            SyncSpeed(newSpeed);
        }

        public override void OnSeek(float targetTime)
        {
            if (vfxInstance != null)
                vfxHanlder?.SetTime(vfxInstance, Mathf.Max(0f, targetTime - clip.StartTime));
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            SyncSpeed(context.PresentationPlaySpeed);
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

        public override void OnStop()
        {
            HandleVFXRelease();
        }

        public override void Reset()
        {
            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
            }
            base.Reset();
            particleInfos = null;
            vfxInstance = null;
            vfxHanlder = null;
            boneGetter = null;
            _returnQueued = false;
        }

        private void HandleVFXRelease()
        {
            if (context != null)
            {
                context.OnPresentationSpeedChanged -= HandlePresentationSpeedChanged;
            }

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

            GameObject instance = vfxInstance;
            vfxInstance = null;
            particleInfos = null;
            vfxHanlder.ReleaseWhenFinished(instance, stopEmissionOnEnd);
        }
    }
}
