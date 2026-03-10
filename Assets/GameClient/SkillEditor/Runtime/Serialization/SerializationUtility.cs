using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;

namespace SkillEditor
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
            ResolveAllAssets(timeline);
            timeline.RecalculateDuration();

            return timeline;
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
            ResolveAllAssets(timeline);
            timeline.RecalculateDuration();
            return timeline;
        }
        /// <summary>
        /// 刷新所有片段的 GUID（遍历 groups → tracks → clips）
        /// </summary>
        private static void RefreshAllGuids(SkillTimeline timeline)
        {
            foreach (var track in timeline.AllTracks)
            {
                foreach (var clip in track.clips)
                {
                    if (clip is SkillAnimationClip animClip)
                    {
                        if(animClip.animationClip != null)
                        {
                            animClip.clipGuid = GetAssetGuid(animClip.animationClip);
                            animClip.clipAssetName = animClip.animationClip.name;
                            animClip.clipAssetPath = GetAssetPath(animClip.animationClip);
                        }
                        if(animClip.overrideMask != null)
                        {
                            animClip.maskGuid = GetAssetGuid(animClip.overrideMask);
                            animClip.maskAssetName = animClip.overrideMask.name;
                            animClip.maskAssetPath = GetAssetPath(animClip.overrideMask);
                        }
                    }
                    else if (clip is VFXClip vfxClip && vfxClip.effectPrefab != null)
                    {
                        vfxClip.prefabGuid = GetAssetGuid(vfxClip.effectPrefab);
                        vfxClip.prefabAssetName = vfxClip.effectPrefab.name;
                        vfxClip.prefabAssetPath = GetAssetPath(vfxClip.effectPrefab);
                    }
                    else if (clip is SkillAudioClip audioClip)
                    {
                        audioClip.clipGuids.Clear();
                        audioClip.clipAssetNames.Clear();
                        audioClip.clipAssetPaths.Clear();
                        if (audioClip.audioClips != null)
                        {
                            foreach (var ac in audioClip.audioClips)
                            {
                                if (ac != null)
                                {
                                    audioClip.clipGuids.Add(GetAssetGuid(ac));
                                    audioClip.clipAssetNames.Add(ac.name);
                                    audioClip.clipAssetPaths.Add(GetAssetPath(ac));
                                }
                                else
                                {
                                    audioClip.clipGuids.Add("");
                                    audioClip.clipAssetNames.Add("");
                                    audioClip.clipAssetPaths.Add("");
                                }
                            }
                        }
                    }
                }
            }
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
                    if (clip is SkillAnimationClip animClip)
                    {
                        if(!string.IsNullOrEmpty(animClip.clipGuid))
                        {
                            animClip.animationClip = await ResolveAsset<AnimationClip>(animClip.clipGuid, animClip.clipAssetPath, animClip.clipAssetName);
                        }
                        if(!string.IsNullOrEmpty(animClip.maskGuid))
                        {
                            animClip.overrideMask = await ResolveAsset<AvatarMask>(animClip.maskGuid, animClip.maskAssetPath, animClip.maskAssetName);
                        }
                    }
                    else if (clip is VFXClip vfxClip && !string.IsNullOrEmpty(vfxClip.prefabGuid))
                    {
                        vfxClip.effectPrefab = await ResolveAsset<GameObject>(vfxClip.prefabGuid, vfxClip.prefabAssetPath, vfxClip.prefabAssetName);
                    }
                    else if (clip is SkillAudioClip audioClip)
                    {
                        audioClip.audioClips.Clear();
                        if (audioClip.clipGuids != null)
                        {
                            for (int i = 0; i < audioClip.clipGuids.Count; i++)
                            {
                                string guid = audioClip.clipGuids[i];
                                string pth = (audioClip.clipAssetPaths != null && i < audioClip.clipAssetPaths.Count) ? audioClip.clipAssetPaths[i] : "";
                                string nme = (audioClip.clipAssetNames != null && i < audioClip.clipAssetNames.Count) ? audioClip.clipAssetNames[i] : "";
                                
                                if (!string.IsNullOrEmpty(guid) || !string.IsNullOrEmpty(pth) || !string.IsNullOrEmpty(nme))
                                {
                                    audioClip.audioClips.Add(await ResolveAsset<AudioClip>(guid, pth, nme));
                                }
                                else
                                {
                                    audioClip.audioClips.Add(null);
                                }
                            }
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
                    (typeof(T) == typeof(AnimationClip) || typeof(T) == typeof(AvatarMask)))
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
    }
}
