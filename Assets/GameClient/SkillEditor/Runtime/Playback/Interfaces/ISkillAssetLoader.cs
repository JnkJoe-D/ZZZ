using System.Threading.Tasks;
using UnityEngine;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// SkillEditor 供外部（主工程）注入的资源加载器接口
    /// 解决技能序列化模块与主工程游戏底层加载 API 的解耦
    /// </summary>
    public interface ISkillAssetLoader
    {
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="guid">Unity 资源的 GUID (Editor下产生)</param>
        /// <param name="address">资源的加载地址（优先使用AssetPath，兼容旧的AssetName）</param>
        /// <returns>返回反序列化的真实资源引用</returns>
        T LoadAsset<T>(string guid, string address) where T : UnityEngine.Object;
        Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;

        /// <summary>
        /// 同步加载子资源（用于加载 FBX 内嵌的 AnimationClip 等）
        /// </summary>
        T LoadSubAsset<T>(string address, string subAssetName) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载子资源
        /// </summary>
        Task<T> LoadSubAssetAsync<T>(string address, string subAssetName) where T : UnityEngine.Object;
    }
}
