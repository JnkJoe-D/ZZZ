using System;
using System.Collections.Generic;
using Game.Framework;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI Sprite 资源配置条目
    /// </summary>
    [Serializable]
    public struct UISpriteItem
    {
        [Tooltip("归属模块名称 (如 SkillBtnModule)。留空或设为 Global 表示全局通用")]
        public string ModuleName;

        [Tooltip("资源唯一检索 Key (例如 BranchEx, UltReady)")]
        public string Key;

        [Tooltip("Sprite 资产引用")]
        public Sprite Sprite;
    }

    /// <summary>
    /// UI Prefab 资源配置条目
    /// </summary>
    [Serializable]
    public struct UIPrefabItem
    {
        [Tooltip("归属模块名称 (如 StatusPanelModule)。留空或设为 Global 表示全局通用")]
        public string ModuleName;

        [Tooltip("资源唯一检索 Key")]
        public string Key;

        [Tooltip("Prefab 预制体引用")]
        public GameObject Prefab;
    }

    /// <summary>
    /// 通用 UI 静态资源与资产配置表 (ScriptableObject)
    /// 继承自 GameConfigAsset，作为所有 UI 模块保底图标、微件预制体等静态资产的集中注册表。
    /// 
    /// 核心特性：
    ///   1. 序列化采用原生 List&lt;T&gt;，支持 Inspector 可视化配置与编辑
    ///   2. 运行时反序列化自动烘焙为只读 Dictionary，检索复杂度 O(1) 且零 GC
    ///   3. 支持模块作用域隔离 (Module Scope)，提供带泛型约束的安全查询接口，防止跨模块资源越界
    /// </summary>
    [CreateAssetMenu(fileName = "UIAssetConfigSO", menuName = "Config/UI/UI Asset Config")]
    public class UIAssetConfigAsset : GameConfigAsset, ISerializationCallbackReceiver
    {
        public const string GlobalModuleName = "Global";

        [Header("Sprite 图标资源列表")]
        [SerializeField] private List<UISpriteItem> _spriteList = new();

        [Header("Prefab 预制体资源列表")]
        [SerializeField] private List<UIPrefabItem> _prefabList = new();

        // ── 运行时 O(1) 高速检索表 ───────────────────
        private readonly Dictionary<string, Sprite> _spriteLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _prefabLookup = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<UISpriteItem> SpriteList => _spriteList;
        public IReadOnlyList<UIPrefabItem> PrefabList => _prefabList;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            RebuildLookup();
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// 烘焙运行时高速字典
        /// </summary>
        public void RebuildLookup()
        {
            _spriteLookup.Clear();
            if (_spriteList != null)
            {
                foreach (var item in _spriteList)
                {
                    if (string.IsNullOrEmpty(item.Key)) continue;
                    string lookupKey = BuildLookupKey(item.ModuleName, item.Key);
                    _spriteLookup[lookupKey] = item.Sprite;
                }
            }

            _prefabLookup.Clear();
            if (_prefabList != null)
            {
                foreach (var item in _prefabList)
                {
                    if (string.IsNullOrEmpty(item.Key)) continue;
                    string lookupKey = BuildLookupKey(item.ModuleName, item.Key);
                    _prefabLookup[lookupKey] = item.Prefab;
                }
            }
        }

        private static string BuildLookupKey(string moduleName, string key)
        {
            if (string.IsNullOrEmpty(moduleName) || string.Equals(moduleName, GlobalModuleName, StringComparison.OrdinalIgnoreCase))
            {
                return $"global::{key.Trim()}";
            }
            return $"{moduleName.Trim()}::{key.Trim()}";
        }

        // ─────────────────────────────────────────────
        // Sprite 检索 API
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根据模块名称和 Key 获取 Sprite。优先查找模块私有资源，未找到时回退到全局通用资源。
        /// </summary>
        public Sprite GetSprite(string moduleName, string key)
        {
            if (TryGetSprite(moduleName, key, out var sprite))
            {
                return sprite;
            }

            GLog.Warning(LogTags.UI, $"[UIAssetConfig] 未找到对应 Sprite 资源: Module='{moduleName}', Key='{key}'");
            return null;
        }

        /// <summary>
        /// 强类型泛型安全接口：仅允许读取属于当前模块或全局的 Sprite
        /// </summary>
        public Sprite GetSprite<TModule>(string key) where TModule : UIModuleBase
        {
            return GetSprite(typeof(TModule).Name, key);
        }

        /// <summary>
        /// 尝试获取 Sprite，不抛出异常
        /// </summary>
        public bool TryGetSprite(string moduleName, string key, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(key))
            {
                sprite = null;
                return false;
            }

            if (_spriteLookup.Count == 0 && _spriteList.Count > 0)
            {
                RebuildLookup();
            }

            // 1. 优先查找当前模块私有
            if (!string.IsNullOrEmpty(moduleName))
            {
                string privateKey = BuildLookupKey(moduleName, key);
                if (_spriteLookup.TryGetValue(privateKey, out sprite) && sprite != null)
                {
                    return true;
                }
            }

            // 2. 回退到全局通用 (Global)
            string globalKey = BuildLookupKey(null, key);
            if (_spriteLookup.TryGetValue(globalKey, out sprite) && sprite != null)
            {
                return true;
            }

            sprite = null;
            return false;
        }

        public bool TryGetSprite<TModule>(string key, out Sprite sprite) where TModule : UIModuleBase
        {
            return TryGetSprite(typeof(TModule).Name, key, out sprite);
        }

        // ─────────────────────────────────────────────
        // Prefab 检索 API
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根据模块名称和 Key 获取 Prefab。优先查找模块私有资源，未找到时回退到全局通用资源。
        /// </summary>
        public GameObject GetPrefab(string moduleName, string key)
        {
            if (TryGetPrefab(moduleName, key, out var prefab))
            {
                return prefab;
            }

            GLog.Warning(LogTags.UI, $"[UIAssetConfig] 未找到对应 Prefab 资源: Module='{moduleName}', Key='{key}'");
            return null;
        }

        /// <summary>
        /// 强类型泛型安全接口：仅允许读取属于当前模块或全局的 Prefab
        /// </summary>
        public GameObject GetPrefab<TModule>(string key) where TModule : UIModuleBase
        {
            return GetPrefab(typeof(TModule).Name, key);
        }

        /// <summary>
        /// 尝试获取 Prefab，不抛出异常
        /// </summary>
        public bool TryGetPrefab(string moduleName, string key, out GameObject prefab)
        {
            if (string.IsNullOrEmpty(key))
            {
                prefab = null;
                return false;
            }

            if (_prefabLookup.Count == 0 && _prefabList.Count > 0)
            {
                RebuildLookup();
            }

            // 1. 优先查找当前模块私有
            if (!string.IsNullOrEmpty(moduleName))
            {
                string privateKey = BuildLookupKey(moduleName, key);
                if (_prefabLookup.TryGetValue(privateKey, out prefab) && prefab != null)
                {
                    return true;
                }
            }

            // 2. 回退到全局通用 (Global)
            string globalKey = BuildLookupKey(null, key);
            if (_prefabLookup.TryGetValue(globalKey, out prefab) && prefab != null)
            {
                return true;
            }

            prefab = null;
            return false;
        }

        public bool TryGetPrefab<TModule>(string key, out GameObject prefab) where TModule : UIModuleBase
        {
            return TryGetPrefab(typeof(TModule).Name, key, out prefab);
        }
    }
}
