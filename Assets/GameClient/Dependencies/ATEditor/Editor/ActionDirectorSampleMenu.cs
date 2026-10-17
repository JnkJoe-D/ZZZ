using UnityEditor;
using UnityEngine;

namespace ATEditor
{
    /// <summary>不使用项目角色、配表或场景的最小复用样例。</summary>
    public static class ActionDirectorSampleMenu
    {
        [MenuItem("Tools/ATEditor/创建独立动画示例")]
        public static void Create() => CreateSample(false);

        [MenuItem("Tools/ATEditor/创建独立视听示例")]
        public static void CreatePresentation() => CreateSample(true);

        [MenuItem("Tools/ATEditor/创建独立运动示例")]
        public static void CreateMotion() => CreateSample(true, true);

        [MenuItem("Tools/ATEditor/创建独立攻击检测示例")]
        public static void CreateAttack()
        {
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/ATEditorAttackSample");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            CreateAttackAssets(folder);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/IndependentAttack.prefab"));
        }

        public static void CreateAttackAssets(string folder)
        {
            var timeline = ScriptableObject.CreateInstance<ActionTimeline>();
            timeline.AddGroup("Independent Attack");
            var movement = new TransformTrack();
            movement.clips.Add(new MovementClip { Duration = 2f, targetPosition = Vector3.forward * 3f, faceTargetOnArrival = false });
            var combat = new CombatTrack();
            var attack = new AttackClip { StartTime = 0.5f, Duration = 1f, sampleCount = 5 };
            attack.scope.shape.radius = 0.8f;
            combat.clips.Add(attack);
            timeline.Groups[0].tracks.Add(movement);
            timeline.Groups[0].tracks.Add(combat);
            timeline.RecalculateDuration();
            AssetDatabase.CreateAsset(timeline, folder + "/IndependentAttack.asset");
            var root = new GameObject("IndependentAttack");
            try
            {
                var actor = new GameObject("Attacker"); actor.transform.SetParent(root.transform, false);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual"; visual.transform.SetParent(actor.transform, false);
                visual.transform.localScale = Vector3.one * 0.4f;
                var director = actor.AddComponent<ActionDirector>(); director.Timeline = timeline;
                var serialized = new SerializedObject(director);
                serialized.FindProperty("_playOnEnable").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                for (int i = 0; i < 2; i++)
                {
                    var target = new GameObject("Target " + (i + 1));
                    target.transform.SetParent(root.transform, false);
                    target.transform.localPosition = new Vector3(0.5f, 0f, 1.3f + i);
                    var rb = target.AddComponent<Rigidbody>(); rb.isKinematic = true;
                    for (int j = 0; j < 2; j++)
                    {
                        var hurtbox = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        hurtbox.name = "HurtBox " + j; hurtbox.transform.SetParent(target.transform, false);
                        hurtbox.transform.localPosition = Vector3.up * (j * 0.3f);
                        hurtbox.transform.localScale = Vector3.one * 0.3f;
                        hurtbox.GetComponent<Collider>().isTrigger = true;
                    }
                }
                root.AddComponent<AttackDetectionSample>();
                PrefabUtility.SaveAsPrefabAsset(root, folder + "/IndependentAttack.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        private static void CreateSample(bool presentation, bool motion = false)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath(motion ? "Assets/ATEditorMotionSample" : presentation ? "Assets/ATEditorPresentationSample" : "Assets/ATEditorAnimationSample");
            string guid = AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(path));
            path = AssetDatabase.GUIDToAssetPath(guid);
            CreateAssets(path, presentation, motion);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path + "/IndependentAction.prefab"));
        }

        public static void CreateAssets(string folder, bool includePresentation = false, bool includeMotion = false)
        {
            var animation = new UnityEngine.AnimationClip { name = "MoveCube" };
            animation.SetCurve("Visual", typeof(Transform), "localPosition.x", AnimationCurve.EaseInOut(0f, -1f, 2f, 1f));
            AssetDatabase.CreateAsset(animation, folder + "/MoveCube.anim");
            var timeline = ScriptableObject.CreateInstance<ActionTimeline>();
            timeline.name = "IndependentAction";
            timeline.AddGroup("Animation");
            var track = new AnimationTrack { isMasterTrack = true };
            track.clips.Add(new AnimationClip { animationClip = animation, Duration = 2f, BlendInDuration = 0f });
            timeline.Groups[0].tracks.Add(track);
            if (includePresentation) AddPresentation(timeline, folder);
            if (includeMotion)
            {
                var movement = new TransformTrack();
                movement.clips.Add(new MovementClip { Duration = 2f, targetPosition = Vector3.forward * 3f, faceTargetOnArrival = false });
                timeline.Groups[0].tracks.Add(movement);
            }
            timeline.RecalculateDuration();
            AssetDatabase.CreateAsset(timeline, folder + "/IndependentAction.asset");

            var root = new GameObject("IndependentAction");
            try
            {
                root.AddComponent<Animator>();
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Visual";
                cube.transform.SetParent(root.transform, false);
                Object.DestroyImmediate(cube.GetComponent<Collider>());
                var director = root.AddComponent<ActionDirector>();
                director.Timeline = timeline;
                var serialized = new SerializedObject(director);
                serialized.FindProperty("_playOnEnable").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, folder + "/IndependentAction.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        private static void AddPresentation(ActionTimeline timeline, string folder)
        {
            var effect = new GameObject("Action Spark");
            effect.SetActive(false);
            GameObject prefab;
            try
            {
                var particles = effect.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.duration = 0.5f;
                main.loop = false;
                main.startLifetime = 0.5f;
                main.startSpeed = 2f;
                main.startSize = 0.12f;
                main.startColor = new Color(1f, 0.6f, 0.1f);
                var emission = particles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                if (shader != null)
                {
                    var material = new Material(shader);
                    AssetDatabase.CreateAsset(material, folder + "/Spark.mat");
                    effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(effect, folder + "/Spark.prefab");
            }
            finally { Object.DestroyImmediate(effect); }
            var vfx = new VFXTrack();
            vfx.clips.Add(new VFXClip { effectPrefab = prefab, StartTime = 0.25f, Duration = 0.75f, stopEmissionOnEnd = true });
            timeline.Groups[0].tracks.Add(vfx);

            string audioPath = folder + "/Tone.wav";
            const int sampleRate = 44100, sampleCount = sampleRate / 4;
            using (var stream = System.IO.File.Create(audioPath))
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + sampleCount * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(sampleCount * 2);
                for (int i = 0; i < sampleCount; i++)
                    writer.Write((short)(Mathf.Sin(2f * Mathf.PI * 660f * i / sampleRate) * 4000f * (1f - (float)i / sampleCount)));
            }
            AssetDatabase.ImportAsset(audioPath, ImportAssetOptions.ForceSynchronousImport);
            var audio = new AudioTrack();
            audio.clips.Add(new AudioClip
            {
                StartTime = 0.25f, Duration = 0.5f, spatialBlend = 0f, lifeControlByClip = true, isAffectSpeed = true,
                audioClips = new System.Collections.Generic.List<UnityEngine.AudioClip> { AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(audioPath) }
            });
            timeline.Groups[0].tracks.Add(audio);
        }
    }
}
