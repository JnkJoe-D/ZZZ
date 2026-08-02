using UnityEngine;

namespace ATEditor.Editor
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
            if (clip.detectFrequency == Frequency.Once)
            {
                var detect = clip.SelectedDetect;
                Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered!</color> HitEffectId: {detect?.hitEffectId ?? 0}, Time: OnEnter");
            }
            lastCheckTime = -1f;
            timesChecked = 0;

            GetMatrix(out fixedHitBoxPosition, out fixedHitBoxRotation);

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

            var selectedDetect = clip.SelectedDetect;
            if (selectedDetect != null && selectedDetect.hitVFXPrefab != null)
            {
                Vector3 spawnPos = GetPreviewPosition();
                Quaternion spawnRot = GetPreviewRotation();
                vfxInstance = EditorVFXManager.Instance.Spawn(selectedDetect.hitVFXPrefab, spawnPos, spawnRot);
                if (vfxInstance != null)
                {
                    vfxInstance.transform.localScale = selectedDetect.hitVFXScale;
                }
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (clip.detectFrequency == Frequency.Times)
            {
                if (clip.times <= 0 || timesChecked >= clip.times) return;
                
                float dynamicInterval = clip.times > 1 ? clip.Duration / clip.times : clip.Duration; 

                if (lastCheckTime < 0 || currentTime - lastCheckTime >= dynamicInterval)
                {
                    var detect = clip.SelectedDetect;
                    Debug.Log($"[SkillEditor Preview] <color=orange>Damage Triggered (Times)!</color> HitEffectId: {detect?.hitEffectId ?? 0}, Time: {currentTime:F2}, Checks: {timesChecked + 1}/{clip.times}");
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
            var detect = clip.SelectedDetect;
            if (detect == null) return spawnTargetPosition;
            Vector3 localOffset = new Vector3(detect.hitVFXPreviewOffsetXZ.x, detect.hitVFXHeight, detect.hitVFXPreviewOffsetXZ.y);
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
            var detect = clip.SelectedDetect;
            if (detect != null)
            {
                vfxInstance.transform.localScale = detect.hitVFXScale;
            }
            vfxInstance.transform.SetPositionAndRotation(GetPreviewPosition(), GetPreviewRotation());
        }

        public void GetCurrentRelativeOffset(out float height, out Vector2 offsetXZ)
        {
            var detect = clip.SelectedDetect;
            height = detect != null ? detect.hitVFXHeight : 1.0f;
            offsetXZ = detect != null ? detect.hitVFXPreviewOffsetXZ : Vector2.zero;

            if (vfxInstance == null) return;

            Vector3 localPos = Quaternion.Inverse(spawnTargetRotation) * (vfxInstance.transform.position - spawnTargetPosition);
            height = localPos.y;
            offsetXZ = new Vector2(localPos.x, localPos.z);
        }

        public void GetHitBoxMatrix(out Vector3 pos, out Quaternion rot)
        {
            GetMatrix(out Vector3 currentPos, out Quaternion currentRot);
            switch (clip.hitBoxFollowMode)
            {
                case HitBoxFollowMode.None:
                    pos = fixedHitBoxPosition;
                    rot = fixedHitBoxRotation;
                    break;
                case HitBoxFollowMode.PositionOnly:
                    pos = currentPos;
                    rot = fixedHitBoxRotation;
                    break;
                case HitBoxFollowMode.RotationOnly:
                    pos = fixedHitBoxPosition;
                    rot = currentRot;
                    break;
                case HitBoxFollowMode.Both:
                default:
                    pos = currentPos;
                    rot = currentRot;
                    break;
            }
        }

        public void GetMatrix(out Vector3 pos, out Quaternion rot)
        {
            Transform rootTrans = context != null ? context.OwnerTransform : null;
            Quaternion rootRot = rootTrans != null ? rootTrans.rotation : Quaternion.identity;
            Vector3 rootPos = rootTrans != null ? rootTrans.position : Vector3.zero;

            Transform bindTrans = null;
            if (context != null)
            {
                var actor = context.GetService<ISkillBoneGetter>();
                if (actor != null)
                {
                    bindTrans = actor.GetBone(clip.bindPoint, clip.customBoneName);
                }
            }
            if (bindTrans == null) bindTrans = rootTrans;

            switch (clip.hitBoxFollowMode)
            {
                case HitBoxFollowMode.PositionOnly:
                    if (bindTrans != null)
                        pos = bindTrans.position + rootRot * clip.positionOffset;
                    else
                        pos = rootPos + rootRot * clip.positionOffset;
                    rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.RotationOnly:
                    pos = rootPos + rootRot * clip.positionOffset;
                    if (bindTrans != null)
                        rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
                    else
                        rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.None:
                    if (bindTrans != null && clip.bindPoint != BindPoint.LogicRoot)
                        pos = bindTrans.position + rootRot * clip.positionOffset;
                    else
                        pos = rootPos + rootRot * clip.positionOffset;
                    rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    break;

                case HitBoxFollowMode.Both:
                default:
                    if (bindTrans != null)
                    {
                        pos = bindTrans.position + bindTrans.rotation * clip.positionOffset;
                        rot = bindTrans.rotation * Quaternion.Euler(clip.rotationOffset);
                    }
                    else
                    {
                        pos = rootPos + rootRot * clip.positionOffset;
                        rot = rootRot * Quaternion.Euler(clip.rotationOffset);
                    }
                    break;
            }
        }
    }
}
