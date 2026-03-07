using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using cfg;
using SimpleJSON;
using Game.Framework;
using Game.Resource;
using Game.Audio.Config;

namespace Game.Config
{
    /// <summary>
    /// 数据表及全局静态配置管理器
    /// 全局SO全从这里拿，离散SO独自按需加载
    /// </summary>
    public class ConfigManager : Singleton<ConfigManager>
    {
        private Tables _tables;
        
        // 缓存全局级别 (A类) 的 ScriptableObject 配置资产
        private Dictionary<Type, ScriptableObject> _SOConfigBanks = new Dictionary<Type, ScriptableObject>();
        
        /// <summary>
        /// 配置表访问入口
        /// </summary>
        public Tables Tables => _tables;

        /// <summary>
        /// 初始化配置表
        /// </summary>
        public async Task InitializeAsync()
        {
            // 通过 Luban 生成的入口初始化，注入资源加载委托
            _tables = new Tables(LoadConfigJson);
            
            // 集中加载所有的全局级别 ScriptableObject 配置
            await LoadGlobalSOConfigsAsync();
            
            // 如果需要预加载某些表，可以在这里处理
            // await _tables.TbItem.LoadAsync(); (如果导出了异步加载逻辑)
            
            Debug.Log("[ConfigManager] 初始化完成 (Luban JSON & Global SOs)");
        }

        private async Task LoadGlobalSOConfigsAsync()
        {
            _SOConfigBanks.Clear();
            
            // 此处加载常驻的全局资产
            var globalAudioConfig = await Game.Resource.ResourceManager.Instance.LoadAssetAsync<GlobalAudioConfigSO>("Assets/Resources/Settings/GlobalAudioConfigSO.asset");
            if (globalAudioConfig != null) _SOConfigBanks[typeof(GlobalAudioConfigSO)] = globalAudioConfig;
            var actionAudioConfig = await Game.Resource.ResourceManager.Instance.LoadAssetAsync<CommonActionAudioSO>("Assets/Resources/Settings/CommonActionAudioSO.asset");
            if (actionAudioConfig != null) _SOConfigBanks[typeof(CommonActionAudioSO)] = actionAudioConfig;
            
            // 为了避免编译警告，此处添加一个空等待以确保异步签名正确
            await Task.Yield();
        }

        /// <summary>
        /// 获取全局系统级的 SO 配置表 (常驻内存的 A 类资产)
        /// 请勿将怪物或技能这种个体资产塞入此类！
        /// </summary>
        public T GetConfigSO<T>() where T : ScriptableObject
        {
            if (_SOConfigBanks.TryGetValue(typeof(T), out var config))
            {
                return config as T;
            }
            Debug.LogError($"[ConfigManager] 试图获取未注册或加载失败的全局 SO 配置表: {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// Luban 内部加载委托 (适配 SimpleJSON)
        /// 【生产环境补充】：
        /// 在 HostPlayMode 下，同步加载 (LoadAssetSync) 仅在资源已存在于本地缓存时有效。
        /// 本架构通过 GameRoot 保证了 ResourceManager 初始化（及热更新下载）先于 ConfigManager 运行，
        /// 因此此处同步加载是安全且符合生产环境规范的。
        /// </summary>
        /// <param name="file">JSON 文件名 (不带后缀)</param>
        /// <returns>解析后的 JSONNode</returns>
        private JSONNode LoadConfigJson(string file)
        {
            // 拼接寻址路径：Assets/Configs/{file}.json
            string assetPath = $"Assets/Configs/{file}.json";
            var asset = ResourceManager.Instance.LoadAsset<TextAsset>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[ConfigManager] 找不到配置文件: {file}");
                return null;
            }

            return JSONNode.Parse(asset.text);
        }
    }
}
