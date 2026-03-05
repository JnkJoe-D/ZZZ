using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SkillEditor.Editor
{
    public static class AnimationUtility
    {
        /// <summary>
        /// 核心方法：让 targetClip 这个片段第一帧的 Root 状态，完美重合 previousClip 在那个时刻的 Root 状态。
        /// </summary>
        public static void MatchOffsetsToPreviousClip(SkillTimeline timeline, SkillAnimationClip targetClip, SkillAnimationClip previousClip, GameObject previewAvatar)
        {
            if (targetClip == null || previousClip == null || previewAvatar == null) return;
            if (targetClip.animationClip == null || previousClip.animationClip == null) return;

            // 1. 获取交界时间点 (即 TargetClip 的起点)
            float matchTime = targetClip.startTime> previousClip.EndTime? previousClip.EndTime : targetClip.startTime;

            // 2. 采样 前一个片段(A) 
            float localTimeA = matchTime;
            // -- 获取 A 在交界点的 Root Transform --
            var rootA = ExtractRootTransformAtTime(previousClip.animationClip, previewAvatar, localTimeA);
            
            // 【关键】：将 A 的 Root Transform 转换到其作为 Clip 层级的坐标系下
            // 前一个片段自身可能也有被匹配出来的 Offset
            Vector3 rootPositionAInTrack = previousClip.positionOffset + (Quaternion.Euler(previousClip.rotationOffset) * rootA.position);
            Quaternion rootRotationAInTrack = Quaternion.Euler(previousClip.rotationOffset) * rootA.rotation;

            // // 3. 采样 目标片段(B) 的第一帧
            // var rootB = ExtractRootTransformAtTime(targetClip.animationClip, previewAvatar, 0f);

            // // 4. 计算 差值矩阵 Offset
            // // MatrixB_offset * MatrixB_local = MatrixA_global (在这个局部的体系内)
            // // 故：OffsetMatrix = MatrixA * Inverse(MatrixB)
            // Matrix4x4 matrixA = Matrix4x4.TRS(rootPositionAInTrack, rootRotationAInTrack, Vector3.one);
            // Matrix4x4 matrixB = Matrix4x4.TRS(rootB.position, rootB.rotation, Vector3.one);
            // Matrix4x4 offsetMatrix = matrixA * matrixB.inverse;

            // 5. 提取并赋值
            targetClip.positionOffset = rootPositionAInTrack;
            targetClip.rotationOffset = rootRotationAInTrack.eulerAngles;
            targetClip.useMatchOffset = true;

            // 保存脏数据
            EditorUtility.SetDirty(timeline);
        }

        /// <summary>
        /// 使用隔离采样体提取某个时刻最终的 Root Transform。
        /// 不直接采样预览角色，避免写回骨骼姿态导致 T-Pose 丢失。
        /// </summary>
        private static (Vector3 position, Quaternion rotation) ExtractRootTransformAtTime(AnimationClip clip, GameObject avatar, float time)
        {
            if (clip == null || avatar == null)
            {
                return (Vector3.zero, Quaternion.identity);
            }

            GameObject sampleAvatar = null;
            try
            {
                sampleAvatar = BuildSamplingAvatar(avatar);
                if (sampleAvatar == null)
                {
                    return (Vector3.zero, Quaternion.identity);
                }

                // 采样从统一原点开始，得到 clip 在该时刻的根位姿
                sampleAvatar.transform.position = Vector3.zero;
                sampleAvatar.transform.rotation = Quaternion.identity;
                float clampedTime = Mathf.Clamp(time, 0f, clip.length);
                clip.SampleAnimation(sampleAvatar, clampedTime);

                return (sampleAvatar.transform.position, sampleAvatar.transform.rotation);
            }
            finally
            {
                if (sampleAvatar != null)
                {
                    Object.DestroyImmediate(sampleAvatar);
                }
            }
        }

        /// <summary>
        /// 构建仅用于采样的临时层级，避免触碰真实预览角色。
        /// </summary>
        private static GameObject BuildSamplingAvatar(GameObject sourceAvatar)
        {
            if (sourceAvatar == null)
            {
                return null;
            }

            GameObject root = CloneTransformHierarchy(sourceAvatar.transform, null);
            if (root == null)
            {
                return null;
            }

            root.hideFlags = HideFlags.HideAndDontSave;

            Animator sourceAnimator = sourceAvatar.GetComponent<Animator>();
            if (sourceAnimator != null)
            {
                Animator sampleAnimator = root.AddComponent<Animator>();
                sampleAnimator.avatar = sourceAnimator.avatar;
                sampleAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                sampleAnimator.updateMode = AnimatorUpdateMode.Normal;
                sampleAnimator.applyRootMotion = true;
                sampleAnimator.Rebind();
                sampleAnimator.Update(0f);
            }

            return root;
        }

        private static GameObject CloneTransformHierarchy(Transform source, Transform parent)
        {
            GameObject node = new GameObject(source.name);
            node.hideFlags = HideFlags.HideAndDontSave;

            Transform nodeTransform = node.transform;
            nodeTransform.SetParent(parent, false);
            nodeTransform.localPosition = source.localPosition;
            nodeTransform.localRotation = source.localRotation;
            nodeTransform.localScale = source.localScale;

            foreach (Transform child in source)
            {
                CloneTransformHierarchy(child, nodeTransform);
            }

            return node;
        }

        /// <summary>
        /// 查找时间轴上位于当前 Clip 之前，且处于同一轨道的上一个动画片段
        /// </summary>
        public static SkillAnimationClip FindPreviousClipOnSameTrack(SkillTimeline timeline, SkillAnimationClip currentClip)
        {
            // 找到包含这个 Clip 的 Track
            var track = timeline.AllTracks.FirstOrDefault(t => t.clips.Contains(currentClip));
            if (track == null) return null;

            // 按时间排序，找到他的前一个
            var sortedClips = track.clips.OrderBy(c => c.startTime).ToList();
            int index = sortedClips.IndexOf(currentClip);
            
            if (index > 0)
            {
                return sortedClips[index - 1] as SkillAnimationClip;
            }
            return null;
        }
    }
}
