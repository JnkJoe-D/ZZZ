namespace SkillEditor.Runtime
{
    /// <summary>
    /// 技能系统全局上下文 / 启动点
    /// 作为服务定位模式 (Service Locator) 的入口，存放各项跨域解耦依赖
    /// </summary>
    public static class SkillSystemContext
    {
        public static ISkillAssetLoader AssetLoader { get; private set; }

        /// <summary>
        /// 主工程启动时调用此方法，注入符合业务（如 YooAsset/Addressables）的资源加载器实现
        /// </summary>
        public static void InjectAssetLoader(ISkillAssetLoader loader)
        {
            AssetLoader = loader;
        }
    }
}
