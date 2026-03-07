using UnityEngine;
using SkillEditor.Runtime;
using Game.Resource;
using System.Threading.Tasks;

namespace Game.Adapters
{
    /// <summary>
    /// 桥接 SkillEditor 的反序列化需求与主工程的 YooAsset/ResourceManager 体系
    /// </summary>
    public class SkillAssetLoaderAdapter : ISkillAssetLoader
    {
        public T LoadAsset<T>(string guid, string address) where T : Object
        {
            // 如果 ResourceManager 已经准备好，使用业务内置方案加载
            // YooAsset 如果是以 AssetPath 为 Address 键，现在这里传进来的会是完整的 assetPath。
            // 某些老数据可能传进来的是 assetName。此处直接使用 address 作为 Address。
            
            if (ResourceManager.Instance != null && ResourceManager.Instance.IsInitialized)
            {
                // 这里将 address 传给 ResourceManager：
                T asset = ResourceManager.Instance.LoadAsset<T>(address);
                if (asset != null) return asset;
            }

            // 【兜底处理】如果没跑通热更流程或者是纯本地测试且 ResourceManager 尚未截管，或报错
            // 可以尝试采用原生的 Resources 兜底：
            return Resources.Load<T>(address);
        }
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (ResourceManager.Instance != null && ResourceManager.Instance.IsInitialized)
            {
                // 这里将 address 传给 ResourceManager：
                T asset = await ResourceManager.Instance.LoadAssetAsync<T>(address);
                if (asset != null) return asset;
            }

            // 【兜底处理】如果没跑通热更流程或者是纯本地测试且 ResourceManager 尚未截管，或报错
            // 可以尝试采用原生的 Resources 兜底：
            return Resources.Load<T>(address);
        }

        public T LoadSubAsset<T>(string address, string subAssetName) where T : Object
        {
            if (ResourceManager.Instance != null && ResourceManager.Instance.IsInitialized)
            {
                T asset = ResourceManager.Instance.LoadSubAsset<T>(address, subAssetName);
                if (asset != null) return asset;
            }

#if UNITY_EDITOR
            // 兜底：如果是纯本地测试且在 Editor 下，通过 LoadAllAssetsAtPath 获取
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.AssetPathToGUID(address)));
            if(allAssets != null)
            {
                foreach (var obj in allAssets)
                {
                    if (obj is T targetType && obj.name == subAssetName)
                    {
                        return targetType;
                    }
                }
            }
#endif
            return null;
        }

        public async Task<T> LoadSubAssetAsync<T>(string address, string subAssetName) where T : Object
        {
            if (ResourceManager.Instance != null && ResourceManager.Instance.IsInitialized)
            {
                T asset = await ResourceManager.Instance.LoadSubAssetAsync<T>(address, subAssetName);
                if (asset != null) return asset;
            }

#if UNITY_EDITOR
            // 兜底直接走同步就行
            return LoadSubAsset<T>(address, subAssetName);
#else
            return null;
#endif
        }
    }
}
