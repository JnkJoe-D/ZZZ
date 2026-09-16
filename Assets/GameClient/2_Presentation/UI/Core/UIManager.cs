using System;
using System.Collections;
using System.Collections.Generic;
using Game.Framework;

using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 管理器
    /// 
    /// 职责：
    ///   1. 管理所有 UIModule 的创建、显示、隐藏、销毁
    ///   2. 异步加载面板 Prefab（通过 ResourceManager）
    ///   3. 驱动层级管理器和导航栈
    ///   4. 对外提供泛型和非泛型双重 API（兼容未来 XLua）
    /// </summary>
    public class UIManager : Game.Framework.Singleton<UIManager>
    {
        // ── 子模块 ────────────────────────────────
        private readonly UILayerManager _layerManager = new();
        private readonly UIStack        _stack        = new();

        // ── 已打开的模块缓存 ─────────────────────
        private readonly Dictionary<Type, UIModuleBase> _openModules = new();
        private readonly HashSet<Type> _loadingModules = new();
        private readonly HashSet<Type> _canceledOpenModules = new();

        // ── Attribute 缓存 ────────────────────────
        private readonly Dictionary<Type, UIPanelAttribute> _attrCache = new();

        // ── 协程宿主 ─────────────────────────────
        private MonoBehaviour _coroutineHost;

        // ── UI 根节点 ────────────────────────────
        private Transform _uiRoot;

        // ── 全局 UI 总配置 ───────────────────────
        private UIGlobalSettingAsset _settings;
        public UIGlobalSettingAsset Settings
        {
            get
            {
                if (_settings == null && Game.GamePlay.ConfigManager.Instance != null)
                {
                    _settings = Game.GamePlay.ConfigManager.Instance.GetConfigSO<UIGlobalSettingAsset>();
                }
                return _settings;
            }
            set => _settings = value;
        }

        // ────────────────────────────────────────
        // 初始化
        // ────────────────────────────────────────

        public void Initialize(MonoBehaviour host)
        {
            _coroutineHost = host;

            // 创建 UI 根节点（DontDestroyOnLoad）
            var rootGo = new GameObject("[UIRoot]");
            UnityEngine.Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;

            GLog.Info(LogTags.UI, "初始化完成");
        }

        // ────────────────────────────────────────
        // 泛型 API
        // ────────────────────────────────────────

        /// <summary>
        /// 打开面板（泛型，类型安全）
        /// </summary>
        public void Open<T>(object data = null) where T : UIModuleBase, new()
        {
            Open(typeof(T), data);
        }

        /// <summary>
        /// 关闭面板（泛型）
        /// </summary>
        public void Close<T>() where T : UIModuleBase
        {
            Close(typeof(T));
        }

        /// <summary>
        /// 获取已打开的模块（泛型）
        /// </summary>
        public T Get<T>() where T : UIModuleBase
        {
            if (_openModules.TryGetValue(typeof(T), out var module))
                return module as T;
            return null;
        }

        /// <summary>
        /// 检查面板是否已打开
        /// </summary>
        public bool IsOpen<T>() where T : UIModuleBase
        {
            return _openModules.ContainsKey(typeof(T));
        }

        // ────────────────────────────────────────
        // 非泛型 API（供 XLua 或反射使用）
        // ────────────────────────────────────────

        /// <summary>
        /// 打开面板（非泛型）
        /// </summary>
        public void Open(Type moduleType, object data = null)
        {
            // 如果模块已存在，直接显示
            if (_openModules.TryGetValue(moduleType, out var existing))
            {
                if (!existing.IsVisible)
                {
                    existing.Internal_Show(data);

                    var attr = GetAttribute(moduleType);
                    if (attr != null && attr.IsFullScreen)
                        _layerManager.OptimizeFullScreen();
                }
                return;
            }

            // 如果该模块正处于异步加载中，避免重复开启协程，若曾标记取消则恢复
            if (_loadingModules.Contains(moduleType))
            {
                _canceledOpenModules.Remove(moduleType);
                return;
            }

            // 首次打开 → 异步加载
            _loadingModules.Add(moduleType);
            _canceledOpenModules.Remove(moduleType);
            _coroutineHost.StartCoroutine(OpenRoutine(moduleType, data));
        }

        /// <summary>
        /// 关闭面板（非泛型）
        /// </summary>
        public void Close(Type moduleType)
        {
            // 若该面板正在加载中，标记取消加载，当异步完成后放弃实例化
            if (_loadingModules.Contains(moduleType))
            {
                _canceledOpenModules.Add(moduleType);
            }

            if (!_openModules.TryGetValue(moduleType, out var module)) return;

            var attr = GetAttribute(moduleType);
            if (attr != null)
            {
                _layerManager.RemoveFromLayer(attr.Layer, module);
            }

            module.Internal_Destroy();
            _openModules.Remove(moduleType);

            // 关闭全屏面板后，恢复下层
            if (attr != null && attr.IsFullScreen)
            {
                _layerManager.RestoreHiddenPanels();
                _layerManager.OptimizeFullScreen();
            }

            EventCenter.Publish(new UIPanelClosedEvent
            {
                ModuleName = moduleType.Name,
                Layer      = attr?.Layer ?? UILayer.Window
            });

            GLog.Info(LogTags.UI, $"关闭面板: {moduleType.Name}");
        }

        /// <summary>
        /// 关闭一个已打开的模块实例
        /// </summary>
        public void Close(UIModuleBase module)
        {
            if (module == null) return;
            Close(module.GetType());
        }

        // ────────────────────────────────────────
        // 导航
        // ────────────────────────────────────────

        /// <summary>
        /// 返回上一个面板（从导航栈弹出）
        /// </summary>
        public void Back()
        {
            var record = _stack.Pop();
            if (record.HasValue)
            {
                Open(record.Value.ModuleType, record.Value.Data);
            }
        }

        /// <summary>
        /// 清空导航栈
        /// </summary>
        public void ClearStack()
        {
            _stack.Clear();
        }

        // ────────────────────────────────────────
        // 层管理
        // ────────────────────────────────────────

        /// <summary>设置整层可见性</summary>
        public void SetLayerVisible(UILayer layer, bool visible)
        {
            _layerManager.SetLayerVisible(layer, visible);
        }

        /// <summary>
        /// 关闭指定层的所有面板
        /// </summary>
        public void CloseAllInLayer(UILayer layer)
        {
            var modules = _layerManager.GetModulesInLayer(layer);
            foreach (var module in modules)
            {
                Close(module);
            }
        }

        /// <summary>关闭所有面板</summary>
        public void CloseAll()
        {
            // 将所有 key 复制一份，避免遍历时修改集合
            var types = new List<Type>(_openModules.Keys);
            foreach (var type in types)
            {
                Close(type);
            }
        }

        // ────────────────────────────────────────
        // 关闭
        // ────────────────────────────────────────

        public void Shutdown()
        {
            _loadingModules.Clear();
            _canceledOpenModules.Clear();
            CloseAll();
            _stack.Clear();
            _layerManager.Clear();
            _attrCache.Clear();

            if (_uiRoot != null)
            {
                UnityEngine.Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }

            GLog.Info(LogTags.UI, "已关闭");
        }

        // ────────────────────────────────────────
        // 内部方法
        // ────────────────────────────────────────

        private IEnumerator OpenRoutine(Type moduleType, object data)
        {
            var attr = GetAttribute(moduleType);
            if (attr == null)
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Error(LogTags.UI, $"模块 {moduleType.Name} 缺少 [UIPanel] Attribute！");
                yield break;
            }

            if (string.IsNullOrEmpty(attr.ViewPrefab))
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Error(LogTags.UI, $"模块 {moduleType.Name} 的 ViewPrefab 路径为空！");
                yield break;
            }

            GLog.Info(LogTags.UI, $"加载面板: {moduleType.Name} ({attr.ViewPrefab})");

            // 1. 异步加载 Prefab
            GameObject prefab = null;

            // 【架构容错与启动级 UI 保护】
            // 如果 ResourceManager 尚未启动，或者明确路径位于 Resources/ 目录中，直接使用原生加载（脱离 YooAsset 流程）
            if (ResourceManager.Instance == null || attr.ViewPrefab.Contains("Resources/"))
            {
                // 提取 Resources 相对路径（去掉扩展名）
                int resIndex = attr.ViewPrefab.IndexOf("Resources/") + 10;
                string resPath = attr.ViewPrefab.Substring(resIndex);
                if (resPath.LastIndexOf('.') != -1)
                {
                    resPath = resPath.Substring(0, resPath.LastIndexOf('.'));
                }
                
                var req = Resources.LoadAsync<GameObject>(resPath);
                yield return req;
                prefab = req.asset as GameObject;
                
                if (prefab != null)
                {
                    GLog.Info(LogTags.UI, $"<color=orange>原生/降级加载面板: {attr.ViewPrefab}</color>");
                }
            }
            else
            {
                // 正常的 YooAsset 加载流程
                yield return ResourceManager.Instance.LoadAssetAsync<GameObject>(
                    attr.ViewPrefab,
                    result => prefab = result
                );
            }

            // 检查加载期间是否已被外部取消关闭
            if (_canceledOpenModules.Contains(moduleType))
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Info(LogTags.UI, $"面板 {moduleType.Name} 在加载期间被取消，放弃实例化。");
                yield break;
            }

            if (prefab == null)
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Error(LogTags.UI, $"面板 Prefab 加载失败: {attr.ViewPrefab}");
                yield break;
            }

            // 2. 实例化到 UIRoot 下
            var go = UnityEngine.Object.Instantiate(prefab, _uiRoot);
            go.name = moduleType.Name;

            // 3. 获取 UIView 组件
            var view = go.GetComponent<UIView>();
            if (view == null)
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Error(LogTags.UI, $"Prefab 上未找到 UIView 组件: {attr.ViewPrefab}");
                UnityEngine.Object.Destroy(go);
                yield break;
            }

            // 4. 创建 Module 实例（纯 C# 对象）
            var module = Activator.CreateInstance(moduleType) as UIModuleBase;
            if (module == null)
            {
                _loadingModules.Remove(moduleType);
                _canceledOpenModules.Remove(moduleType);
                GLog.Error(LogTags.UI, $"无法创建 Module 实例: {moduleType.Name}");
                UnityEngine.Object.Destroy(go);
                yield break;
            }

            // 5. 注入 View 并触发生命周期
            module.Internal_Create(view);
            _openModules[moduleType] = module;
            _loadingModules.Remove(moduleType);
            _canceledOpenModules.Remove(moduleType);

            // 6. 层级管理 (只有在 View 赋值后才能算出 SortingOrder)
            _layerManager.AddToLayer(attr.Layer, module);

            module.Internal_Show(data);

            // 7. 全屏优化
            if (attr.IsFullScreen)
            {
                _layerManager.OptimizeFullScreen();
            }

            // 8. 广播事件
            EventCenter.Publish(new UIPanelOpenedEvent
            {
                ModuleName = moduleType.Name,
                Layer      = attr.Layer
            });

            GLog.Info(LogTags.UI, $"面板已打开: {moduleType.Name}");
        }

        /// <summary>
        /// 获取并缓存模块的 UIPanelAttribute
        /// </summary>
        private UIPanelAttribute GetAttribute(Type moduleType)
        {
            if (_attrCache.TryGetValue(moduleType, out var attr))
                return attr;

            var attrs = moduleType.GetCustomAttributes(typeof(UIPanelAttribute), false);
            if (attrs.Length > 0)
            {
                attr = attrs[0] as UIPanelAttribute;
                _attrCache[moduleType] = attr;
                return attr;
            }

            return null;
        }
    }
}
