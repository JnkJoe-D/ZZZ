using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

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
                        }
                        if(animClip.overrideMask != null)
                        {
                            animClip.maskGuid = GetAssetGuid(animClip.overrideMask);
                            animClip.maskAssetName = animClip.overrideMask.name;
                        }
                    }
                    else if (clip is VFXClip vfxClip && vfxClip.effectPrefab != null)
                    {
                        vfxClip.prefabGuid = GetAssetGuid(vfxClip.effectPrefab);
                        vfxClip.prefabAssetName = vfxClip.effectPrefab.name;
                    }
                    else if (clip is SkillAudioClip audioClip && audioClip.audioClip != null)
                    {
                        audioClip.clipGuid = GetAssetGuid(audioClip.audioClip);
                        audioClip.clipAssetName = audioClip.audioClip.name;
                    }
                }
            }
        }

        /// <summary>
        /// 根据 GUID 还原所有资源（遍历 groups → tracks → clips）
        /// </summary>
        public static void ResolveAllAssets(SkillTimeline timeline)
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
                            animClip.animationClip = ResolveAsset<AnimationClip>(animClip.clipGuid, animClip.clipAssetName);
                        }
                        if(!string.IsNullOrEmpty(animClip.maskGuid))
                        {
                            animClip.overrideMask = ResolveAsset<AvatarMask>(animClip.maskGuid, animClip.maskAssetName);
                        }
                    }
                    else if (clip is VFXClip vfxClip && !string.IsNullOrEmpty(vfxClip.prefabGuid))
                    {
                        vfxClip.effectPrefab = ResolveAsset<GameObject>(vfxClip.prefabGuid, vfxClip.prefabAssetName);
                    }
                    else if (clip is SkillAudioClip audioClip && !string.IsNullOrEmpty(audioClip.clipGuid))
                    {
                        audioClip.audioClip = ResolveAsset<AudioClip>(audioClip.clipGuid, audioClip.clipAssetName);
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
        private static T ResolveAsset<T>(string guid, string assetName = null) where T:Object
        {
#if UNITY_EDITOR
            string assetPath =  AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (!string.IsNullOrEmpty(assetName))
            {
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
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
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return asset;
#else
            return null;
#endif
        }
    }
}
