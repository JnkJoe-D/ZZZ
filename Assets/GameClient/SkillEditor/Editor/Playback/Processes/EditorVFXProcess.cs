using UnityEngine;
using UnityEditor;

namespace SkillEditor.Editor
{
    [ProcessBinding(typeof(VFXClip), PlayMode.EditorPreview)]
    public class EditorVFXProcess : ProcessBase<VFXClip>
    {
        private GameObject vfxInstance;
        public GameObject Instance => vfxInstance;
        
        // 缓存第一帧的挂点世界坐标和旋转
        private Vector3 spawnTargetPosition;
        private Quaternion spawnTargetRotation;

        public override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnEnter()
        {
            if (clip.effectPrefab == null) return;

            // 1. 获取挂点
            Transform targetTransform = null;
            // 尝试获取 ISkillActor (如果预览模型挂了脚本)
            var actor = context.GetService<ISkillBoneGetter>();
            if (actor != null)
            {
                targetTransform = actor.GetBone(clip.bindPoint, clip.customBoneName);
            }
            // 尝试获取 Animator (用于通用人形骨骼)
            else 
            {
                var animator = context.Owner.GetComponent<Animator>();
                if (animator != null && animator.isHuman)
                {
                    targetTransform = GetHumanBone(animator, clip.bindPoint);
                }
            }
            
            // 降级: Owner Transform
            if (targetTransform == null)
            {
                targetTransform = context.OwnerTransform;
            }

            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (targetTransform != null)
            {
                spawnPos = targetTransform.position;
                spawnRot = targetTransform.rotation;
            }

            // 2. 编辑器生成 (使用 EditorVFXManager)
            // 编辑器下始终 World Space 生成，手动更新位置以支持预览拖拽/Seek
            vfxInstance = EditorVFXManager.Instance.Spawn(clip.effectPrefab, spawnPos, spawnRot);

            if (vfxInstance != null)
            {
                // 缓存生成时的挂点位置
                if (targetTransform != null)
                {
                    spawnTargetPosition = targetTransform.position;
                    spawnTargetRotation = targetTransform.rotation;
                }
                else
                {
                    spawnTargetPosition = spawnPos;
                    spawnTargetRotation = spawnRot;
                }
                
                UpdateTransform(targetTransform);
            }
        }

        public override void OnUpdate(float currentTime, float deltaTime)
        {
            if (vfxInstance == null) return;

            // 1. 更新位置 (因为是 World Space 生成，需手动跟随)
            Transform targetTransform = GetTargetTransform();
            if (targetTransform != null)
            {
                UpdateTransform(targetTransform);
            }

            // 2. 采样粒子
            // 计算当前 Clip 内部时间
            float clipTime = currentTime - clip.StartTime;
            EditorVFXManager.Instance.Sample(vfxInstance, clipTime);
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
        public Transform GetBindTransform()
        {
            return GetTargetTransform();
        }

        private Transform GetTargetTransform()
        {
            if (context.Owner == null) return null;

            var actor = context.Owner.GetComponent<ISkillBoneGetter>();
            if (actor != null)
            {
                return actor.GetBone(clip.bindPoint, clip.customBoneName);
            }
            
            var animator = context.Owner.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                var bone = GetHumanBone(animator, clip.bindPoint);
                if (bone != null) return bone;
            }

            return context.OwnerTransform;
        }

        private Transform GetHumanBone(Animator animator, BindPoint point)
        {
            switch (point)
            {
                case BindPoint.Head: return animator.GetBoneTransform(HumanBodyBones.Head);
                case BindPoint.LeftHand: return animator.GetBoneTransform(HumanBodyBones.LeftHand);
                case BindPoint.RightHand: return animator.GetBoneTransform(HumanBodyBones.RightHand);
                case BindPoint.Body: return animator.GetBoneTransform(HumanBodyBones.Spine); // or Hips
                case BindPoint.Root: return animator.transform;
                // Weapon 无法直接从 Animator 获取，需自行约定或降级
                default: return null;
            }
        }

        public void ForceUpdateTransform()
        {
            if (vfxInstance == null) return;
            Transform target = GetTargetTransform();
            if (target != null)
            {
                UpdateTransform(target);
            }
        }

        private void UpdateTransform(Transform target)
        {
            if (vfxInstance == null || target == null) return;

            vfxInstance.transform.localScale = clip.scale;

            if (clip.followTarget)
            {
                Vector3 finalPos = target.position + target.rotation * clip.positionOffset;
                Quaternion finalRot = target.rotation * Quaternion.Euler(clip.rotationOffset);
                vfxInstance.transform.SetPositionAndRotation(finalPos, finalRot);
            }
            else
            {
                 // 不跟随目标：使用进入时第一帧保存的挂点基准计算
                 Vector3 finalPos = spawnTargetPosition + spawnTargetRotation * clip.positionOffset;
                 Quaternion finalRot = spawnTargetRotation * Quaternion.Euler(clip.rotationOffset);
                 vfxInstance.transform.SetPositionAndRotation(finalPos, finalRot);
            }
        }

        /// <summary>
        /// 获取当前实例相对于挂点的偏移（供编辑器同步使用）
        /// </summary>
        public void GetCurrentRelativeOffset(out Vector3 posOffset, out Vector3 rotOffset, out Vector3 scale)
        {
            posOffset = clip.positionOffset;
            rotOffset = clip.rotationOffset;
            scale = clip.scale;

            if (vfxInstance == null) return;

            Transform target = GetTargetTransform();
            if (target == null) return;

            // 选择用于反推的基准坐标系：
            // 如果跟随，则用实时的挂载点位置反算
            // 如果不跟随，则用第一帧生成时的挂载点位置（即不会随着人物后续移动改变的地方）反算
            Vector3 refPos = clip.followTarget ? target.position : spawnTargetPosition;
            Quaternion refRot = clip.followTarget ? target.rotation : spawnTargetRotation;

            posOffset = Quaternion.Inverse(refRot) * (vfxInstance.transform.position - refPos);
            
            Quaternion localRot = Quaternion.Inverse(refRot) * vfxInstance.transform.rotation;
            rotOffset = localRot.eulerAngles;

            scale = vfxInstance.transform.localScale;
        }
    }
}
