using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.GamePlay;
using MAnimSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ATEditor.Test
{
    /// <summary>Plan 02-A：先验证同图恢复逻辑采样，不切换正式驱动。</summary>
    public sealed class ActionPoseClockProbeTests
    {
        private readonly List<Object> objects = new List<Object>();
        private readonly List<GameObject> actors = new List<GameObject>();
        private readonly List<Vector3[]> baselinePoses = new List<Vector3[]>();
        private readonly List<Quaternion[]> baselineRotations = new List<Quaternion[]>();
        private readonly List<Vector3> baselineDeltas = new List<Vector3>();
        private readonly List<Quaternion> baselineRootRotations = new List<Quaternion>();

        [Serializable]
        private sealed class Evidence
        {
            public string source;
            public int logicSteps;
            public int presentationSamples;
            public float maxBonePositionError, maxBoneAngleError, maxRootDeltaError, maxRootAngleError;
            public float rootDistance;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var actor in actors) if (actor != null) Object.DestroyImmediate(actor);
            actors.Clear();
            foreach (var asset in objects) if (asset != null) Object.DestroyImmediate(asset);
            objects.Clear();
            baselinePoses.Clear(); baselineRotations.Clear(); baselineDeltas.Clear(); baselineRootRotations.Clear();
        }
        [UnityTearDown]
        public IEnumerator ExitRuntime() { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [UnityTest]
        public IEnumerator RealEllenSingleGraph_RestoresLogicPoseAndRootMotionAcrossPresentationAndBlend()
        {
            yield return new EnterPlayMode();
            RunProbe(true);
        }

        [UnityTest]
        public IEnumerator NativeRootCurve_RestoresIncrementWithoutSubmittingPresentationMotion()
        {
            yield return new EnterPlayMode();
            RunProbe(false);
        }

        private void RunProbe(bool ellen)
        {
            UnityEngine.AnimationClip a, b;
            if (ellen)
            {
                var source = AssetDatabase.LoadAssetAtPath<RoleActionConfigAsset>(
                    "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/Ellen_Action_Attack_Normal_01_01.asset");
                var next = AssetDatabase.LoadAssetAtPath<RoleActionConfigAsset>(
                    "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/Ellen/Action/Ellen_Action_Attack_Normal_01_02.asset");
                Assert.IsNotNull(source); Assert.IsNotNull(next);
                a = source.actionTimelineSO.GetMasterAnimationTrack().clips.OfType<AnimationClip>().First().animationClip;
                b = next.actionTimelineSO.GetMasterAnimationTrack().clips.OfType<AnimationClip>().First().animationClip;
            }
            else
            {
                a = RootCurve(1f); b = RootCurve(2f);
            }
            const float step = 1f / 60f;
            var evidence = new Evidence { source = ellen ? "Ellen original clips/avatar" : "Native linear root curves", logicSteps = 36 };
            Simulate(ellen, a, b, step, false, evidence);
            Simulate(ellen, a, b, step, true, evidence);
            string folder = Path.GetFullPath("docs/实施证据/Plan02");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, ellen ? "ellen-pose-probe.json" : "native-pose-probe.json"), JsonUtility.ToJson(evidence, true));
            Assert.That(evidence.maxBonePositionError, Is.LessThan(0.0002f), "显示求值不能改变下一逻辑采样的骨骼位置");
            Assert.That(evidence.maxBoneAngleError, Is.LessThan(0.05f), "显示求值不能改变逻辑骨骼旋转");
            Assert.That(evidence.maxRootDeltaError, Is.LessThan(0.0002f), "恢复显示采样后根增量必须与纯逻辑基线一致");
            Assert.That(evidence.maxRootAngleError, Is.LessThan(0.05f), "混合中的根旋转增量也必须一致");
            if (!ellen) Assert.That(evidence.rootDistance, Is.GreaterThan(0.1f), "不能用始终为零的根运动伪通过");
        }

        private void Simulate(bool ellen, UnityEngine.AnimationClip a, UnityEngine.AnimationClip b,
            float step, bool withPresentation, Evidence evidence)
        {
            var staging = new GameObject("Plan02Probe"); staging.SetActive(false); actors.Add(staging);
            string path = ellen ? "Assets/Resources/Prefab/Role/Ellen/Avatar_Female_Size02_Ellen.prefab" :
                "Assets/GameClient/Dependencies/ATEditor/Editor/Resources/DefaultPreviewCharacter.prefab";
            var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), staging.transform);
            foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
                if (script != null) Object.DestroyImmediate(script);
            var animator = visual.GetComponentInChildren<Animator>(true);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = true;
            var receiver = animator.gameObject.AddComponent<Plan02PoseProbeReceiver>();
            var component = staging.AddComponent<AnimComponent>();
            component.SetManuallyDriven(true); staging.SetActive(true); component.Initialize(); component.InitializeGraph();
            var bones = animator.GetComponentsInChildren<Transform>(true);
            component.Play(a, 0, 0f); component.EvaluateManually(0f);
            for (int i = 0; i < evidence.logicSteps; i++)
            {
                if (i == 15) component.Play(b, 0, 0.15f);
                receiver.Delta = Vector3.zero; receiver.Rotation = Quaternion.identity; receiver.Capture = true;
                component.EvaluateManually(step);
                receiver.Capture = false;
                var positions = bones.Select(t => t.localPosition).ToArray();
                var rotations = bones.Select(t => t.localRotation).ToArray();
                if (!withPresentation)
                {
                    baselinePoses.Add(positions); baselineRotations.Add(rotations);
                    baselineDeltas.Add(receiver.Delta); baselineRootRotations.Add(receiver.Rotation);
                    evidence.rootDistance += receiver.Delta.magnitude;
                }
                else
                {
                    for (int bone = 0; bone < bones.Length; bone++)
                    {
                        evidence.maxBonePositionError = Mathf.Max(evidence.maxBonePositionError, Vector3.Distance(positions[bone], baselinePoses[i][bone]));
                        evidence.maxBoneAngleError = Mathf.Max(evidence.maxBoneAngleError, Quaternion.Angle(rotations[bone], baselineRotations[i][bone]));
                    }
                    evidence.maxRootDeltaError = Mathf.Max(evidence.maxRootDeltaError, Vector3.Distance(receiver.Delta, baselineDeltas[i]));
                    evidence.maxRootAngleError = Mathf.Max(evidence.maxRootAngleError, Quaternion.Angle(receiver.Rotation, baselineRootRotations[i]));
                    var times = new List<(Playable node, double time)>();
                    for (int root = 0; root < component.Graph.GetRootPlayableCount(); root++) CaptureTimes(component.Graph.GetRootPlayable(root), times);
                    bool fireEvents = animator.fireEvents; animator.fireEvents = false;
                    for (int sample = 0; sample < 3; sample++) { component.Graph.Evaluate(step / 4f); evidence.presentationSamples++; }
                    foreach (var item in times) item.node.SetTime(item.time);
                    component.Graph.Evaluate(0f);
                    animator.fireEvents = fireEvents;
                }
            }
        }

        private static void CaptureTimes(Playable node, List<(Playable node, double time)> times)
        {
            if (!node.IsValid() || times.Any(item => item.node.Equals(node))) return;
            times.Add((node, node.GetTime()));
            for (int i = 0; i < node.GetInputCount(); i++) CaptureTimes(node.GetInput(i), times);
        }

        private UnityEngine.AnimationClip RootCurve(float speed)
        {
            var clip = new UnityEngine.AnimationClip { name = "Plan02RootCurve" }; objects.Add(clip);
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0f, 0f, 2f, speed * 2f));
            foreach (string axis in new[] { "y", "z" }) clip.SetCurve("", typeof(Animator), "RootT." + axis, AnimationCurve.Constant(0f, 2f, 0f));
            foreach (string axis in new[] { "x", "y", "z", "w" }) clip.SetCurve("", typeof(Animator), "RootQ." + axis, AnimationCurve.Constant(0f, 2f, axis == "w" ? 1f : 0f));
            return clip;
        }
    }
}
