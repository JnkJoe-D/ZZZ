using UnityEngine;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 编辑器模式下的伤害检测预览。
    /// 由于编辑器预览环境不具备真实的怪物实体和数值组件，这里仅提供核心的时间轴触发日志提示，
    /// 帮助开发者确认伤害判定逻辑是否按期执行。
    /// </summary>
    [ProcessBinding(typeof(HitClip), PlayMode.EditorPreview)]
    public class EditorHitProcess : ProcessBase<HitClip>
    {
        private float lastCheckTime;
        private int timesChecked = 0;
        private GameObject vfxInstance;
        public GameObject Instance => vfxInstance;
        
        public Vector3 fixedHitBoxPosition;
        public Quaternion fixedHitBoxRotation;

        private Vector3 spawnTargetPosition;
        private Quaternion spawnTargetRotation;

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnEnter()
        {
            if (clip.hitFrequency == HitFrequency.Once)
            {
                Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered!</color> HitEffects: {clip.hitEffects?.Length ?? 0}, Time: OnEnter");
            }
            lastCheckTime = -1f;
            timesChecked = 0;

            if (!clip.isHitBoxFollowBindPoint)
            {
                GetMatrix(out fixedHitBoxPosition, out fixedHitBoxRotation);
            }

            if (context.OwnerTransform != null)
            {
                spawnTargetPosition = context.OwnerTransform.position;
                spawnTargetRotation = context.OwnerTransform.rotation;
            }
            else
            {
                spawnTargetPosition = Vector3.zero;
                spawnTargetRotation = Quaternion.identity;
            }

            if (clip.hitVFXPrefab != null)
            {
                Vector3 spawnPos = GetPreviewPosition();
                Quaternion spawnRot = GetPreviewRotation();
                vfxInstance = EditorVFXManager.Instance.Spawn(clip.hitVFXPrefab, spawnPos, spawnRot);
                if (vfxInstance != null)
                {
                    vfxInstance.transform.localScale = clip.hitVFXScale;
                }
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.hitFrequency == HitFrequency.Times)
            {
                if (clip.times <= 0 || timesChecked >= clip.times) return;
                
                float dynamicInterval = clip.times > 1 ? clip.Duration / clip.times : clip.Duration; 

                if (lastCheckTime < 0 || currentTime - lastCheckTime >= dynamicInterval)
                {
                    Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered (Times)!</color> HitEffects: {clip.hitEffects?.Length ?? 0}, Time: {currentTime:F2}, Checks: {timesChecked + 1}/{clip.times}");
                    lastCheckTime = currentTime;
                    timesChecked++;
                }
            }

            if (vfxInstance != null)
            {
                float clipTime = currentTime - clip.StartTime;
                EditorVFXManager.Instance.Sample(vfxInstance, clipTime);
            }
        }

        public override void OnExit()
        {
            if (vfxInstance != null)
            {
                EditorVFXManager.Instance.Return(vfxInstance);
                vfxInstance = null;
            }
        }

        public override void OnDisable()
        {
            if (vfxInstance != null)
            {
                EditorVFXManager.Instance.Return(vfxInstance);
                vfxInstance = null;
            }
        }

        private Vector3 GetPreviewPosition()
        {
            Vector3 localOffset = new Vector3(clip.hitVFXPreviewOffsetXZ.x, clip.hitVFXHeight, clip.hitVFXPreviewOffsetXZ.y);
            return spawnTargetPosition + spawnTargetRotation * localOffset;
        }

        private Quaternion GetPreviewRotation()
        {
            return spawnTargetRotation;
        }

        public void ForceUpdateTransform()
        {
            if (vfxInstance != null)
            {
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            if (vfxInstance == null) return;
            vfxInstance.transform.localScale = clip.hitVFXScale;
            vfxInstance.transform.SetPositionAndRotation(GetPreviewPosition(), GetPreviewRotation());
        }

        public void GetCurrentRelativeOffset(out float height, out Vector2 offsetXZ)
        {
            height = clip.hitVFXHeight;
            offsetXZ = clip.hitVFXPreviewOffsetXZ;

            if (vfxInstance == null) return;

            Vector3 localPos = Quaternion.Inverse(spawnTargetRotation) * (vfxInstance.transform.position - spawnTargetPosition);
            height = localPos.y;
            offsetXZ = new Vector2(localPos.x, localPos.z);
        }

        public void GetMatrix(out Vector3 pos, out Quaternion rot)
        {
            Transform bindTrans = null;
            if (context != null)
            {
                var actor = context.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    bindTrans = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }

            if (bindTrans != null)
            {
                pos = bindTrans.position + bindTrans.rotation * clip.positionOffset;
                rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else if (context != null && context.OwnerTransform != null)
            {
                pos = context.OwnerTransform.position + context.OwnerTransform.rotation * clip.positionOffset;
                rot = context.OwnerTransform.rotation * Quaternion.Euler(clip.rotationOffset);
            }
            else
            {
                pos = clip.positionOffset;
                rot = Quaternion.Euler(clip.rotationOffset);
            }
        }
    }
}
