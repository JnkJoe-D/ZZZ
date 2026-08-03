using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Timeline;

namespace ATEditor
{
    /// <summary>
    /// 技能序列化工具类
    /// 遍历树状结构：groups → tracks → clips
    /// </summary>
    public static class SerializationUtility
    {
        /// <summary>
        /// 导出技能到 JSON 文件
        /// </summary>
        public static void ExportToJson(SkillTimeline timeline, string path)
        {
            if (timeline == null) return;

            // 1. 导出前置处理：确保所有 Clip 的 GUID 都是最新的
            RefreshAllGuids(timeline);

            // 2. 序列化
            string json = JsonUtility.ToJson(timeline, true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// 从 JSON 文件路径导入技能
        /// </summary>
        public static SkillTimeline ImportFromJsonPath(string path)
        {
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            SkillTimeline timeline = ScriptableObject.CreateInstance<SkillTimeline>();
            JsonUtility.FromJsonOverwrite(json, timeline);

            // 导入后置处理：根据 GUID 还原资源引用
            ResolveAllAssetsImmediate(timeline);
            timeline.RecalculateDuration();

            return timeline;
        }
        
        public static async Task<SkillTimeline> ImportFromJsonPathAsync(string path)
        {
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            SkillTimeline timeline = ScriptableObject.CreateInstance<SkillTimeline>();
            JsonUtility.FromJsonOverwrite(json, timeline);
            await ResolveAllAssets(timeline);
            timeline.RecalculateDuration();
            return timeline;
        }
        
        private static void ResolveAllAssetsImmediate(SkillTimeline timeline)
        {
            if (timeline == null) return;

            foreach (var track in timeline.AllTracks)
            {
                foreach (var clip in track.clips)
                {
                    if (clip is AnimationClip animClip)
                    {
                        if (animClip.clipRef.IsValid())
                            animClip.animationClip = ResolveAssetImmediate<UnityEngine.AnimationClip>(animClip.clipRef.guid, animClip.clipRef.assetPath, animClip.clipRef.assetName);
                        if (animClip.maskRef.IsValid())
                            animClip.overrideMask = ResolveAssetImmediate<AvatarMask>(animClip.maskRef.guid, animClip.maskRef.assetPath, animClip.maskRef.assetName);
                    }
                    else if (clip is VFXClip vfxClip && vfxClip.vfxRef.IsValid())
                    {
                        vfxClip.effectPrefab = ResolveAssetImmediate<GameObject>(vfxClip.vfxRef.guid, vfxClip.vfxRef.assetPath, vfxClip.vfxRef.assetName);
                    }
                    else if (clip is SpawnClip spawnClip && spawnClip.prefabRef.IsValid())
                    {
                        spawnClip.prefab = ResolveAssetImmediate<GameObject>(spawnClip.prefabRef.guid, spawnClip.prefabRef.assetPath, spawnClip.prefabRef.assetName);
                    }
                    else if (clip is HitClip hitClip && hitClip.detects != null)
                    {
                        foreach (var detect in hitClip.detects)
                        {
                            if (detect == null) continue;
                            if (detect.hitVFXRef.IsValid())
                                detect.hitVFXPrefab = ResolveAssetImmediate<GameObject>(detect.hitVFXRef.guid, detect.hitVFXRef.assetPath, detect.hitVFXRef.assetName);
                            if (detect.hitAudioRef.IsValid())
                                detect.hitAudioClip = ResolveAssetImmediate<UnityEngine.AudioClip>(detect.hitAudioRef.guid, detect.hitAudioRef.assetPath, detect.hitAudioRef.assetName);
                        }
                    }
                    else if (clip is AudioClip audioClip)
                    {
                        audioClip.audioClips.Clear();
                        if (audioClip.audioRefs != null)
                        {
                            foreach (var r in audioClip.audioRefs)
                            {
                                if (r.IsValid())
                                    audioClip.audioClips.Add(ResolveAssetImmediate<UnityEngine.AudioClip>(r.guid, r.assetPath, r.assetName));
                                else
                                    audioClip.audioClips.Add(null);
                            }
                        }
                    }
                    else if (clip is CameraAnimationClip cameraAnimClip)
                    {
                        if (cameraAnimClip.cameraRef.IsValid())
                            cameraAnimClip.cameraPrefab = ResolveAssetImmediate<GameObject>(cameraAnimClip.cameraRef.guid, cameraAnimClip.cameraRef.assetPath, cameraAnimClip.cameraRef.assetName);
                        if (cameraAnimClip.timelineRef.IsValid())
                            cameraAnimClip.timelineAsset = ResolveAssetImmediate<TimelineAsset>(cameraAnimClip.timelineRef.guid, cameraAnimClip.timelineRef.assetPath, cameraAnimClip.timelineRef.assetName);
                    }
                }
            }
        }
        /// <summary>
        /// 从 JSON 文件获取技能
        /// </summary>
        public static SkillTimeline OpenFromJson(TextAsset textAsset)
        {
            if(textAsset==null)return null;
            string json = textAsset.text;
            SkillTimeline timeline = ScriptableObject.CreateInstance<SkillTimeline>();
            JsonUtility.FromJsonOverwrite(json, timeline);

            // 导入后置处理：根据 GUID 还原资源引用
            ResolveAllAssetsImmediate(timeline);
            timeline.RecalculateDuration();
            return timeline;
        }
        
        public static async Task<SkillTimeline> OpenFromJsonAsync(TextAsset textAsset)
        {
            if(textAsset==null)return null;

            string json = textAsset.text;
            SkillTimeline timeline = ScriptableObject.CreateInstance<SkillTimeline>();
            JsonUtility.FromJsonOverwrite(json, timeline);
            await ResolveAllAssets(timeline);
            timeline.RecalculateDuration();
            return timeline;
        }
        /// <summary>
        /// 刷新所有片段的 GUID（遍历 groups → tracks → clips）
        /// 已通过 Inspector 自动同步，此处作为导出前的双重校验。
        /// </summary>
        private static void RefreshAllGuids(SkillTimeline timeline)
        {
            foreach (var track in timeline.AllTracks)
            {
                foreach (var clip in track.clips)
                {
                    if (clip is AnimationClip animClip)
                    {
                        SyncAssetReference(animClip.clipRef, animClip.animationClip);
                        SyncAssetReference(animClip.maskRef, animClip.overrideMask);
                    }
                    else if (clip is VFXClip vfxClip)
                    {
                        SyncAssetReference(vfxClip.vfxRef, vfxClip.effectPrefab);
                    }
                    else if (clip is SpawnClip spawnClip)
                    {
                        SyncAssetReference(spawnClip.prefabRef, spawnClip.prefab);
                    }
                    else if (clip is HitClip hitClip && hitClip.detects != null)
                    {
                        foreach (var detect in hitClip.detects)
                        {
                            if (detect == null) continue;
                            SyncAssetReference(detect.hitVFXRef, detect.hitVFXPrefab);
                            SyncAssetReference(detect.hitAudioRef, detect.hitAudioClip);
                        }
                    }
                    else if (clip is AudioClip audioClip)
                    {
                        if (audioClip.audioRefs == null) audioClip.audioRefs = new List<SkillAssetReference>();
                        while (audioClip.audioRefs.Count < audioClip.audioClips.Count) audioClip.audioRefs.Add(new SkillAssetReference());
                        while (audioClip.audioRefs.Count > audioClip.audioClips.Count) audioClip.audioRefs.RemoveAt(audioClip.audioRefs.Count - 1);

                        for (int i = 0; i < audioClip.audioClips.Count; i++)
                        {
                            SyncAssetReference(audioClip.audioRefs[i], audioClip.audioClips[i]);
                        }
                    }
                    else if (clip is CameraAnimationClip cameraAnimClip)
                    {
                        SyncAssetReference(cameraAnimClip.cameraRef, cameraAnimClip.cameraPrefab);
                        SyncAssetReference(cameraAnimClip.timelineRef, cameraAnimClip.timelineAsset);
                    }
                }
            }
        }

        private static void SyncAssetReference(SkillAssetReference r, Object asset)
        {
            if (asset == null)
            {
                r.Clear();
                return;
            }
            r.guid = GetAssetGuid(asset);
            r.assetName = asset.name;
            r.assetPath = GetAssetPath(asset);
        }

        /// <summary>
        /// 根据 GUID 还原所有资源（遍历 groups → tracks → clips）
        /// </summary>
        public static async Task ResolveAllAssets(SkillTimeline timeline)
        {
            if (timeline == null) return;

            foreach (var track in timeline.AllTracks)
            {
                foreach (var clip in track.clips)
                {
                    if (clip is AnimationClip animClip)
                    {
                        if (animClip.clipRef.IsValid())
                            animClip.animationClip = await ResolveAsset<UnityEngine.AnimationClip>(animClip.clipRef.guid, animClip.clipRef.assetPath, animClip.clipRef.assetName);
                        if (animClip.maskRef.IsValid())
                            animClip.overrideMask = await ResolveAsset<AvatarMask>(animClip.maskRef.guid, animClip.maskRef.assetPath, animClip.maskRef.assetName);
                    }
                    else if (clip is VFXClip vfxClip && vfxClip.vfxRef.IsValid())
                    {
                        vfxClip.effectPrefab = await ResolveAsset<GameObject>(vfxClip.vfxRef.guid, vfxClip.vfxRef.assetPath, vfxClip.vfxRef.assetName);
                    }
                    else if (clip is SpawnClip spawnClip && spawnClip.prefabRef.IsValid())
                    {
                        spawnClip.prefab = await ResolveAsset<GameObject>(spawnClip.prefabRef.guid, spawnClip.prefabRef.assetPath, spawnClip.prefabRef.assetName);
                    }
                    else if (clip is HitClip hitClip && hitClip.detects != null)
                    {
                        foreach (var detect in hitClip.detects)
                        {
                            if (detect == null) continue;
                            if (detect.hitVFXRef.IsValid())
                                detect.hitVFXPrefab = await ResolveAsset<GameObject>(detect.hitVFXRef.guid, detect.hitVFXRef.assetPath, detect.hitVFXRef.assetName);
                            if (detect.hitAudioRef.IsValid())
                                detect.hitAudioClip = await ResolveAsset<UnityEngine.AudioClip>(detect.hitAudioRef.guid, detect.hitAudioRef.assetPath, detect.hitAudioRef.assetName);
                        }
                    }
                    else if (clip is AudioClip audioClip)
                    {
                        audioClip.audioClips.Clear();
                        if (audioClip.audioRefs != null)
                        {
                            foreach (var r in audioClip.audioRefs)
                            {
                                if (r.IsValid())
                                    audioClip.audioClips.Add(await ResolveAsset<UnityEngine.AudioClip>(r.guid, r.assetPath, r.assetName));
                                else
                                    audioClip.audioClips.Add(null);
                            }
                        }
                    }
                    else if (clip is CameraAnimationClip cameraAnimClip)
                    {
                        if (cameraAnimClip.cameraRef.IsValid())
                        {
                            cameraAnimClip.cameraPrefab = await ResolveAsset<GameObject>(cameraAnimClip.cameraRef.guid, cameraAnimClip.cameraRef.assetPath, cameraAnimClip.cameraRef.assetName);
                        }
                        if (cameraAnimClip.timelineRef.IsValid())
                        {
                            cameraAnimClip.timelineAsset = await ResolveAsset<TimelineAsset>(cameraAnimClip.timelineRef.guid, cameraAnimClip.timelineRef.assetPath, cameraAnimClip.timelineRef.assetName);
                        }
                    }
                }
            }
        }
        private static string GetAssetGuid(Object asset)
        {
#if UNITY_EDITOR
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            return guid;
#else
            return "";
#endif
        }
        private static string GetAssetPath(Object asset)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPath(asset);
#else
            return "";
#endif
        }

        private static async Task<T> ResolveAsset<T>(string guid, string assetPath, string assetName = null) where T:Object
        {
#if UNITY_EDITOR
            // 如果是在 Editor 运行模式下，且已经注入了资源加载器，优先走运行时加载逻辑 (例如 Addressables/YooAsset 模拟)
            if (Application.isPlaying && Runtime.SkillSystemContext.AssetLoader != null)
            {
                // 使用 assetPath 或者回退使用 assetName
                string address = !string.IsNullOrEmpty(assetPath) ? assetPath : assetName;

                // 仅针对常见内嵌多资源的类型（如 FBX 里的 AnimationClip / AvatarMask）尝试异步读取 SubAsset
                if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(assetName) && 
                    (typeof(T) == typeof(UnityEngine.AnimationClip) || typeof(T) == typeof(AvatarMask)))
                {
                    T subAsset = await Runtime.SkillSystemContext.AssetLoader.LoadSubAssetAsync<T>(address, assetName);
                    if (subAsset != null) return subAsset;
                }
                
                // 否则直接加载主资产
                T runtimeAsset = await Runtime.SkillSystemContext.AssetLoader.LoadAssetAsync<T>(address);
                if (runtimeAsset != null) return runtimeAsset;
            }

            string realAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(realAssetPath)) return null;

            if (!string.IsNullOrEmpty(assetName))
            {
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(realAssetPath);
                foreach (var obj in allAssets)
                {
                    // 仅当类型匹配且名称匹配时返回，实现对嵌套 FBX 等多对象的精确抓取
                    if (obj is T targetType && obj.name == assetName)
                    {
                        return targetType;
                    }
                }
            }

            // 回退兼容：如果没配置名称或没找到，就直接加载该路径的主返回资产
            T asset = AssetDatabase.LoadAssetAtPath<T>(realAssetPath);
            return asset;
#else
            // 非 Editor 的真机运行环境下，全部走适配器注入的业务管线加载
            if (Runtime.SkillSystemContext.AssetLoader != null)
            {
                string address = !string.IsNullOrEmpty(assetPath) ? assetPath : assetName;

                if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(assetName) && 
                    (typeof(T) == typeof(AnimationClip) || typeof(T) == typeof(AvatarMask)))
                {
                    T subAsset = await Runtime.SkillSystemContext.AssetLoader.LoadSubAssetAsync<T>(address, assetName);
                    if (subAsset != null) return subAsset;
                }

                return await Runtime.SkillSystemContext.AssetLoader.LoadAssetAsync<T>(address);
            }
            return null;
#endif
        }
        private static T ResolveAssetImmediate<T>(string guid, string assetPath, string assetName = null) where T : Object
        {
#if UNITY_EDITOR
            if (Application.isPlaying && Runtime.SkillSystemContext.AssetLoader != null)
            {
                string address = !string.IsNullOrEmpty(assetPath) ? assetPath : assetName;

                if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(assetName) &&
                    (typeof(T) == typeof(UnityEngine.AnimationClip) || typeof(T) == typeof(AvatarMask)))
                {
                    T subAsset = Runtime.SkillSystemContext.AssetLoader.LoadSubAsset<T>(address, assetName);
                    if (subAsset != null) return subAsset;
                }

                T runtimeAsset = Runtime.SkillSystemContext.AssetLoader.LoadAsset<T>(guid, address);
                if (runtimeAsset != null) return runtimeAsset;
            }

            string realAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(realAssetPath)) return null;

            if (!string.IsNullOrEmpty(assetName))
            {
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(realAssetPath);
                foreach (var obj in allAssets)
                {
                    if (obj is T targetType && obj.name == assetName)
                    {
                        return targetType;
                    }
                }
            }

            return AssetDatabase.LoadAssetAtPath<T>(realAssetPath);
#else
            if (Runtime.SkillSystemContext.AssetLoader != null)
            {
                string address = !string.IsNullOrEmpty(assetPath) ? assetPath : assetName;

                if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(assetName) &&
                    (typeof(T) == typeof(AnimationClip) || typeof(T) == typeof(AvatarMask)))
                {
                    T subAsset = Runtime.SkillSystemContext.AssetLoader.LoadSubAsset<T>(address, assetName);
                    if (subAsset != null) return subAsset;
                }

                return Runtime.SkillSystemContext.AssetLoader.LoadAsset<T>(guid, address);
            }
            return null;
#endif
        }
    }
}
