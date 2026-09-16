using UnityEngine;
using YooAsset;

namespace Game.Framework
{
    /// <summary>
    /// 资源管理器配置
    /// 挂在 GameRoot 同层次的 ScriptableObject 或直接作为常量使用
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "Game/Resource Config")]
    public class ResourceConfig : ScriptableObject
    {
        [Header("包名配置")]
        [Tooltip("默认资源包名，对应 YooAsset 打包时配置的 Package Name")]
        public string defaultPackageName = "DefaultPackage";

        [Space]
        [Header("运行模式")]
        [Tooltip("EditorSimulate：编辑器模拟（无需打Bundle，开发首选）\n" +
                 "Offline：单机离线（Bundle已内置到包内，无热更）\n" +
                 "HostPlay：联机热更（从远端CDN下载更新）")]
        public EPlayMode playMode = EPlayMode.EditorSimulateMode;

        [Space]
        [Header("远端服务器（仅 HostPlay 模式生效）")]
        [Tooltip("主CDN服务器地址，格式：http://ip:port")]
        public string hostServerURL = "http://127.0.0.1:8080";
        [Tooltip("备用CDN服务器地址（主服务器失败时使用）")]
        public string fallbackServerURL = "http://127.0.0.1:8080";

        [Space]
        [Header("热更设置（仅 HostPlay 模式生效）")]
        [Tooltip("是否在 URL 中追加 App 编译版本号目录 (例如 /CDN/PC/1.0/)\n" +
                 "注意：资源版本子目录 (例如 /CDN/PC/2026-xx-xx/) 已在架构层自动处理，无需开启此项。")]
        public bool appendVersionToURL = false;
        [Tooltip("是否在启动时自动检查并下载更新")]
        public bool autoUpdate = true;
        [Tooltip("下载失败时的最大重试次数")]
        public int downloadRetryCount = 3;

        // ── 运行时构建完整的 CDN URL ──────────────────────────
        public string GetHostServerURL()
        {
            var platform = GetPlatformPath();
            var baseUrl = $"{hostServerURL}/CDN/{platform}";
            
            if (appendVersionToURL)
            {
                baseUrl = $"{baseUrl}/{Application.version}";
            }
            
            return baseUrl;
        }

        public string GetFallbackServerURL()
        {
            var platform = GetPlatformPath();
            var baseUrl = $"{fallbackServerURL}/CDN/{platform}";

            if (appendVersionToURL)
            {
                baseUrl = $"{baseUrl}/{Application.version}";
            }

            return baseUrl;
        }

        private string GetPlatformPath()
        {
#if UNITY_EDITOR
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            if (target == UnityEditor.BuildTarget.Android) return "Android";
            if (target == UnityEditor.BuildTarget.iOS)     return "IPhone";
            if (target == UnityEditor.BuildTarget.WebGL)   return "WebGL";
            return "PC";
#else
            if (Application.platform == RuntimePlatform.Android)    return "Android";
            if (Application.platform == RuntimePlatform.IPhonePlayer) return "IPhone";
            if (Application.platform == RuntimePlatform.WebGLPlayer) return "WebGL";
            return "PC";
#endif
        }
    }
}
