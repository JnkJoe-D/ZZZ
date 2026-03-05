using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SkillEditor.Editor
{
    /// <summary>
    /// Editor-only animation preview runtime based on Playables.
    /// This runtime bypasses AnimComponent/ISkillAnimationHandler and evaluates clips directly.
    /// </summary>
    public static class AnimationUtils
    {
        private const float Epsilon = 0.0001f;

        private sealed class PreviewClipNode
        {
            public string clipId;
            public SkillAnimationClip clip;
            public int layerIndex;
            public int mixerInputIndex;
            public AnimationClipPlayable playable;
            public float rawWeight;
            public float finalWeight;
        }

        private sealed class PreviewGraphContext
        {
            public int ownerId;
            public GameObject owner;
            public Animator animator;
            public SkillTimeline timeline;
            public AnimationTrack masterTrack;
            public PlayableGraph graph;
            public AnimationPlayableOutput output;
            public AnimationLayerMixerPlayable rootMixer;
            public readonly Dictionary<int, AnimationMixerPlayable> layerMixers = new Dictionary<int, AnimationMixerPlayable>();
            public readonly Dictionary<string, SkillAnimationClip> registeredClips = new Dictionary<string, SkillAnimationClip>();
            public readonly Dictionary<string, PreviewClipNode> clipNodes = new Dictionary<string, PreviewClipNode>();
            public Vector3 appliedOffsetPosition;
            public Quaternion appliedOffsetRotation = Quaternion.identity;
            public bool hasAppliedOffset;
            public bool samplingMode;
            public bool trackBaseApplied;
            public bool dirty = true;
        }

        private static readonly Dictionary<int, PreviewGraphContext> Contexts = new Dictionary<int, PreviewGraphContext>();

        #region Public API

        public static void RegisterClip(GameObject owner, SkillAnimationClip clip)
        {
            if (owner == null || clip == null || string.IsNullOrEmpty(clip.clipId))
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            ctx.registeredClips[clip.clipId] = clip;
            ctx.dirty = true;
        }

        public static void UnregisterClip(GameObject owner, string clipId)
        {
            if (owner == null || string.IsNullOrEmpty(clipId))
            {
                return;
            }

            if (!Contexts.TryGetValue(owner.GetInstanceID(), out PreviewGraphContext ctx))
            {
                return;
            }

            if (ctx.registeredClips.Remove(clipId))
            {
                ctx.dirty = true;
            }
        }

        public static void EnsureInitialized(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            EnsureGraph(ctx);
            RebuildIfDirty(ctx);
        }

        public static void SetTimeline(GameObject owner, SkillTimeline timeline)
        {
            if (owner == null)
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            ctx.timeline = timeline;
            ctx.masterTrack = null;
            ctx.trackBaseApplied = false;
            ctx.dirty = true;
        }

        public static void SetSamplingMode(GameObject owner, bool samplingMode)
        {
            if (owner == null)
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            if (ctx.samplingMode == samplingMode)
            {
                return;
            }

            // Leaving sampling mode: remove temporary per-sample clip offset.
            if (!samplingMode)
            {
                RevertOwnerOffsets(ctx);
            }

            ctx.samplingMode = samplingMode;
        }

        public static void ApplyTrackBasePose(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            RevertOwnerOffsets(ctx);
            RebuildTrackOffsets(ctx);
            ApplyTrackBasePoseInternal(ctx);
        }

        public static void Tick(GameObject owner, float currentTime, float deltaTime, float globalSpeed)
        {
            if (owner == null)
            {
                return;
            }

            PreviewGraphContext ctx = GetOrCreateContext(owner);
            if (ctx == null)
            {
                return;
            }

            EnsureGraph(ctx);
            RebuildIfDirty(ctx);

            if (!ctx.graph.IsValid())
            {
                return;
            }

            if (ctx.samplingMode)
            {
                SampleAtTime(ctx, Mathf.Max(0f, currentTime), deltaTime, globalSpeed);
                return;
            }

            RevertOwnerOffsets(ctx);
            if (!ctx.trackBaseApplied)
            {
                ApplyTrackBasePoseInternal(ctx);
            }

            if (ctx.clipNodes.Count == 0)
            {
                ctx.graph.Evaluate(0f);
                return;
            }

            EvaluateInputsForTime(ctx, currentTime, globalSpeed);

            ctx.graph.Evaluate(0f);
        }

        public static void MarkDirty(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            if (Contexts.TryGetValue(owner.GetInstanceID(), out PreviewGraphContext ctx))
            {
                ctx.dirty = true;
            }
        }

        public static void Dispose(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            int ownerId = owner.GetInstanceID();
            if (!Contexts.TryGetValue(ownerId, out PreviewGraphContext ctx))
            {
                return;
            }

            DestroyContext(ctx);
            Contexts.Remove(ownerId);
        }

        public static void DisposeAll()
        {
            foreach (PreviewGraphContext ctx in Contexts.Values)
            {
                DestroyContext(ctx);
            }

            Contexts.Clear();
        }

        #endregion

        #region Context / Graph

        private static PreviewGraphContext GetOrCreateContext(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            int ownerId = owner.GetInstanceID();
            if (Contexts.TryGetValue(ownerId, out PreviewGraphContext existing))
            {
                existing.owner = owner;
                return existing;
            }

            var ctx = new PreviewGraphContext
            {
                ownerId = ownerId,
                owner = owner
            };

            Contexts.Add(ownerId, ctx);
            return ctx;
        }

        private static void EnsureGraph(PreviewGraphContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            if (ctx.graph.IsValid() && ctx.rootMixer.IsValid() && ctx.animator != null)
            {
                return;
            }

            ctx.animator = ctx.owner != null ? ctx.owner.GetComponentInChildren<Animator>() : null;
            if (ctx.animator == null)
            {
                return;
            }

            if (ctx.graph.IsValid())
            {
                ctx.graph.Destroy();
            }

            ctx.graph = PlayableGraph.Create($"SkillEditorPreview_{ctx.ownerId}");
            ctx.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            ctx.rootMixer = AnimationLayerMixerPlayable.Create(ctx.graph, 1);
            ctx.rootMixer.SetInputCount(1);

            ctx.output = AnimationPlayableOutput.Create(ctx.graph, "Animation", ctx.animator);
            ctx.output.SetSourcePlayable(ctx.rootMixer);

            ctx.graph.Play();
            ctx.dirty = true;
        }

        private static void RebuildIfDirty(PreviewGraphContext ctx)
        {
            if (ctx == null || !ctx.dirty)
            {
                return;
            }

            if (!ctx.graph.IsValid() || !ctx.rootMixer.IsValid())
            {
                return;
            }

            DestroyClipNodes(ctx);
            DestroyLayerMixers(ctx);
            RebuildTrackOffsets(ctx);

            int maxLayer = 0;
            List<SkillAnimationClip> clips = new List<SkillAnimationClip>(ctx.registeredClips.Values);
            clips.Sort(CompareClipOrder);

            foreach (SkillAnimationClip clip in clips)
            {
                if (clip == null || clip.animationClip == null)
                {
                    continue;
                }

                maxLayer = Mathf.Max(maxLayer, (int)clip.layer);
            }

            int rootInputCount = Mathf.Max(1, maxLayer + 1);
            ctx.rootMixer.SetInputCount(rootInputCount);

            for (int layer = 0; layer < rootInputCount; layer++)
            {
                AnimationMixerPlayable layerMixer = AnimationMixerPlayable.Create(ctx.graph, 0, true);
                layerMixer.SetInputCount(0);
                ctx.graph.Connect(layerMixer, 0, ctx.rootMixer, layer);
                ctx.rootMixer.SetInputWeight(layer, 1f);
                ctx.layerMixers[layer] = layerMixer;
            }

            foreach (SkillAnimationClip clip in clips)
            {
                if (clip == null || clip.animationClip == null || string.IsNullOrEmpty(clip.clipId))
                {
                    continue;
                }

                int layerIndex = (int)clip.layer;
                if (!ctx.layerMixers.TryGetValue(layerIndex, out AnimationMixerPlayable layerMixer) || !layerMixer.IsValid())
                {
                    continue;
                }

                var playable = AnimationClipPlayable.Create(ctx.graph, clip.animationClip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(0d);
                playable.SetTime(0d);
                playable.SetDone(false);

                int inputPort = layerMixer.GetInputCount();
                layerMixer.SetInputCount(inputPort + 1);
                ctx.graph.Connect(playable, 0, layerMixer, inputPort);
                layerMixer.SetInputWeight(inputPort, 0f);

                var node = new PreviewClipNode
                {
                    clipId = clip.clipId,
                    clip = clip,
                    layerIndex = layerIndex,
                    mixerInputIndex = inputPort,
                    playable = playable,
                    rawWeight = 0f,
                    finalWeight = 0f
                };

                ctx.clipNodes[clip.clipId] = node;
            }

            ctx.dirty = false;
        }

        private static int CompareClipOrder(SkillAnimationClip a, SkillAnimationClip b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            int byLayer = ((int)a.layer).CompareTo((int)b.layer);
            if (byLayer != 0)
            {
                return byLayer;
            }

            int byStart = a.startTime.CompareTo(b.startTime);
            if (byStart != 0)
            {
                return byStart;
            }

            return string.CompareOrdinal(a.clipId, b.clipId);
        }

        private static void DestroyClipNodes(PreviewGraphContext ctx)
        {
            foreach (PreviewClipNode node in ctx.clipNodes.Values)
            {
                if (node.playable.IsValid())
                {
                    node.playable.Destroy();
                }
            }

            ctx.clipNodes.Clear();
        }

        private static void DestroyLayerMixers(PreviewGraphContext ctx)
        {
            foreach (AnimationMixerPlayable mixer in ctx.layerMixers.Values)
            {
                if (mixer.IsValid())
                {
                    mixer.Destroy();
                }
            }

            ctx.layerMixers.Clear();
        }

        private static void DestroyContext(PreviewGraphContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            Animator animator = ctx.animator;
            DestroyClipNodes(ctx);
            DestroyLayerMixers(ctx);
            ctx.registeredClips.Clear();
            ctx.masterTrack = null;

            if (ctx.graph.IsValid())
            {
                ctx.graph.Destroy();
            }

            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            ctx.hasAppliedOffset = false;
            ctx.appliedOffsetPosition = Vector3.zero;
            ctx.appliedOffsetRotation = Quaternion.identity;
            ctx.trackBaseApplied = false;
            ctx.samplingMode = false;
        }

        #endregion

        #region Sampling / Weight

        private static float ComputeRawWeight(SkillAnimationClip clip, float currentTime)
        {
            if (clip == null)
            {
                return 0f;
            }

            float start = clip.startTime;
            float end = clip.EndTime;
            if (currentTime < start || currentTime >= end)
            {
                return 0f;
            }

            float inWeight = 1f;
            if (clip.blendInDuration > Epsilon)
            {
                inWeight = Mathf.Clamp01((currentTime - start) / clip.blendInDuration);
            }

            float outWeight = 1f;
            if (clip.blendOutDuration > Epsilon)
            {
                outWeight = Mathf.Clamp01((end - currentTime) / clip.blendOutDuration);
            }

            return Mathf.Clamp01(Mathf.Min(inWeight, outWeight));
        }

        private static void RebuildTrackOffsets(PreviewGraphContext ctx)
        {
            ctx.masterTrack = ctx.timeline != null ? ctx.timeline.GetMasterAnimationTrack() : null;
        }

        private static void SampleAtTime(PreviewGraphContext ctx, float targetTime, float deltaTime, float globalSpeed)
        {
            if (ctx == null || !ctx.graph.IsValid())
            {
                return;
            }

            RevertOwnerOffsets(ctx);
            bool oldApplyRootMotion = false;
            
            if (ctx.animator != null && ctx.animator.isActiveAndEnabled)
            {
                oldApplyRootMotion = ctx.animator.applyRootMotion;
                // --- 【核心修复】强制清理上次采样留下的 Graph 时间跳跃 ---
                // 先让动画图的时间回到起点，算出一个可能会有瞬间回跳的无效位移
                EvaluateInputsForTime(ctx, 0f, globalSpeed);
                ctx.graph.Evaluate(0f);
                
                // 再开启累积开关并强行清理骨骼，把刚才回跳产生的脏位移彻底抹除
                ctx.animator.applyRootMotion = true; 
                ctx.animator.Rebind();
                ctx.animator.Update(0f);
            }

            ApplyTrackBasePoseInternal(ctx);

            float step = Mathf.Abs(deltaTime);
            if (step <= Epsilon)
            {
                step = 1f / 30f;
            }
            step = Mathf.Clamp(step, 1f / 120f, 0.1f);

            float simTime = 0f;

            if (targetTime <= Epsilon)
            {
                ApplyClipOffsetsByCurrentWeights(ctx);
                
                if (ctx.animator != null && ctx.animator.isActiveAndEnabled)
                {
                    ctx.animator.applyRootMotion = oldApplyRootMotion;
                }
                return;
            }

            while (simTime < targetTime - Epsilon)
            {
                simTime = Mathf.Min(simTime + step, targetTime);
                EvaluateInputsForTime(ctx, simTime, globalSpeed);
                ctx.graph.Evaluate(step);
            }
            ApplyClipOffsetsByCurrentWeights(ctx);
            
            if (ctx.animator != null && ctx.animator.isActiveAndEnabled)
            {
                ctx.animator.applyRootMotion = oldApplyRootMotion;
            }
        }

        private static void EvaluateInputsForTime(PreviewGraphContext ctx, float sampleTime, float globalSpeed)
        {
            if (ctx == null)
            {
                return;
            }

            var layerRawSums = new Dictionary<int, float>();
            foreach (PreviewClipNode node in ctx.clipNodes.Values)
            {
                node.rawWeight = ComputeRawWeight(node.clip, sampleTime);
                node.finalWeight = 0f;
                if (node.rawWeight <= Epsilon)
                {
                    continue;
                }

                if (layerRawSums.TryGetValue(node.layerIndex, out float sum))
                {
                    layerRawSums[node.layerIndex] = sum + node.rawWeight;
                }
                else
                {
                    layerRawSums[node.layerIndex] = node.rawWeight;
                }
            }

            foreach (PreviewClipNode node in ctx.clipNodes.Values)
            {
                if (!ctx.layerMixers.TryGetValue(node.layerIndex, out AnimationMixerPlayable layerMixer) || !layerMixer.IsValid())
                {
                    continue;
                }

                float finalWeight = 0f;
                if (node.rawWeight > Epsilon &&
                    layerRawSums.TryGetValue(node.layerIndex, out float layerSum) &&
                    layerSum > Epsilon)
                {
                    finalWeight = node.rawWeight / layerSum;
                    SetClipTime(node, sampleTime, globalSpeed);
                }

                node.finalWeight = finalWeight;
                layerMixer.SetInputWeight(node.mixerInputIndex, finalWeight);
            }
        }

        private static void ApplyClipOffsetsByCurrentWeights(PreviewGraphContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            Vector3 finalOffsetPos = Vector3.zero;
            Vector3 finalOffsetEuler = Vector3.zero;
            float finalOffsetWeight = 0f;
            foreach (PreviewClipNode node in ctx.clipNodes.Values)
            {
                if (node.finalWeight <= Epsilon)
                {
                    continue;
                }

                GetClipOffsets(node, out Vector3 clipOffsetPos, out Quaternion clipOffsetRot);
                finalOffsetPos += clipOffsetPos * node.finalWeight;
                finalOffsetEuler += clipOffsetRot.eulerAngles * node.finalWeight;
                finalOffsetWeight += node.finalWeight;
            }

            ApplyOwnerOffsets(ctx, finalOffsetPos, finalOffsetEuler, finalOffsetWeight);
        }

        private static void GetClipOffsets(PreviewClipNode node, out Vector3 clipOffsetPos, out Quaternion clipOffsetRot)
        {
            clipOffsetPos = Vector3.zero;
            clipOffsetRot = Quaternion.identity;
            if (node == null || node.clip == null)
            {
                return;
            }

            clipOffsetPos = node.clip.positionOffset;
            clipOffsetRot = Quaternion.Euler(node.clip.rotationOffset);
        }

        private static void ApplyTrackBasePoseInternal(PreviewGraphContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            if (ctx.timeline != null)
            {
                ctx.masterTrack = ctx.timeline.GetMasterAnimationTrack();
            }

            if (ctx.masterTrack == null)
            {
                return;
            }

            Transform motionTransform = GetMotionTransform(ctx);
            if (motionTransform == null)
            {
                return;
            }

            motionTransform.position = ctx.masterTrack.offsetPos;
            motionTransform.rotation = Quaternion.Euler(ctx.masterTrack.offsetRot);
            ctx.trackBaseApplied = true;
        }

        private static void RevertOwnerOffsets(PreviewGraphContext ctx)
        {
            if (ctx == null || !ctx.hasAppliedOffset)
            {
                return;
            }

            Transform motionTransform = GetMotionTransform(ctx);
            if (motionTransform == null)
            {
                return;
            }

            motionTransform.position -= ctx.appliedOffsetPosition;
            motionTransform.rotation = motionTransform.rotation * Quaternion.Inverse(ctx.appliedOffsetRotation);

            ctx.hasAppliedOffset = false;
            ctx.appliedOffsetPosition = Vector3.zero;
            ctx.appliedOffsetRotation = Quaternion.identity;
        }

        private static void ApplyOwnerOffsets(PreviewGraphContext ctx, Vector3 weightedOffsetPosition, Vector3 weightedOffsetEuler, float totalOffsetWeight)
        {
            if (ctx == null)
            {
                return;
            }

            if (totalOffsetWeight <= Epsilon)
            {
                return;
            }

            Vector3 offsetPos = weightedOffsetPosition / totalOffsetWeight;
            Quaternion offsetRot = Quaternion.Euler(weightedOffsetEuler / totalOffsetWeight);

            Transform motionTransform = GetMotionTransform(ctx);
            if (motionTransform == null)
            {
                return;
            }

            motionTransform.position += offsetPos;
            motionTransform.rotation = motionTransform.rotation * offsetRot;

            ctx.hasAppliedOffset = true;
            ctx.appliedOffsetPosition = offsetPos;
            ctx.appliedOffsetRotation = offsetRot;
        }

        private static Transform GetMotionTransform(PreviewGraphContext ctx)
        {
            if (ctx == null)
            {
                return null;
            }

            if (ctx.animator != null)
            {
                return ctx.animator.transform;
            }

            return ctx.owner != null ? ctx.owner.transform : null;
        }

        private static void SetClipTime(PreviewClipNode node, float currentTime, float globalSpeed)
        {
            if (node == null || node.clip == null || node.clip.animationClip == null || !node.playable.IsValid())
            {
                return;
            }

            _ = globalSpeed;

            float length = node.clip.animationClip.length;
            if (length <= Epsilon)
            {
                node.playable.SetTime(0d);
                return;
            }

            float speed = node.clip.playbackSpeed;
            if (Mathf.Abs(speed) <= Epsilon)
            {
                speed = 1f;
            }

            float localTime = (currentTime - node.clip.startTime) * speed;

            if (node.clip.animationClip.isLooping)
            {
                localTime = Mathf.Repeat(localTime, length);
            }
            else
            {
                localTime = Mathf.Clamp(localTime, 0f, length);
            }

            node.playable.SetTime(localTime);
            node.playable.SetDone(false);
        }

        #endregion
    }
}
