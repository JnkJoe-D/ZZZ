using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI 层级管理器
    /// 
    /// 职责：
    ///   1. 按 UILayer 分层管理所有已打开的面板
    ///   2. 自动为同层面板分配递增的 SortingOrder
    ///   3. 全屏面板优化：打开全屏面板时隐藏下层以减少 DrawCall
    /// </summary>
    public class UILayerManager
    {
        /// <summary>每层内的面板 SortingOrder 间隔</summary>
        private const int OrderStep = 10;

        /// <summary>分层数据：UILayer → 该层的模块列表</summary>
        private readonly Dictionary<UILayer, List<UIModuleBase>> _layers = new();

        /// <summary>
        /// 注册模块到指定层
        /// </summary>
        public void AddToLayer(UILayer layer, UIModuleBase module)
        {
            if (!_layers.TryGetValue(layer, out var list))
            {
                list = new List<UIModuleBase>();
                _layers[layer] = list;
            }

            if (!list.Contains(module))
            {
                list.Add(module);
                RearrangeSortingOrder(layer);
            }
        }

        /// <summary>
        /// 从层中移除模块
        /// </summary>
        public void RemoveFromLayer(UILayer layer, UIModuleBase module)
        {
            if (_layers.TryGetValue(layer, out var list))
            {
                list.Remove(module);
                RearrangeSortingOrder(layer);
            }
        }

        /// <summary>
        /// 全屏面板优化：隐藏被遮挡的下层面板
        /// 当最顶层的全屏面板打开时，下层可以安全地隐藏以减少 DrawCall
        /// </summary>
        public void OptimizeFullScreen()
        {
            // 找到最高的全屏面板所在层
            UILayer? highestFullScreenLayer = null;

            foreach (var kvp in _layers)
            {
                foreach (var module in kvp.Value)
                {
                    if (!module.IsVisible) continue;

                    var attr = GetPanelAttribute(module);
                    if (attr != null && attr.IsFullScreen)
                    {
                        if (highestFullScreenLayer == null || kvp.Key > highestFullScreenLayer.Value)
                        {
                            highestFullScreenLayer = kvp.Key;
                        }
                    }
                }
            }

            if (highestFullScreenLayer == null) return;

            // 隐藏低于该层的所有面板（不触发 Module 的 OnHide，仅做 GameObject 隐藏）
            foreach (var kvp in _layers)
            {
                if (kvp.Key < highestFullScreenLayer.Value)
                {
                    foreach (var module in kvp.Value)
                    {
                        if (module.IsVisible)
                        {
                            module.View?.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 恢复被全屏优化隐藏的面板
        /// </summary>
        public void RestoreHiddenPanels()
        {
            foreach (var kvp in _layers)
            {
                foreach (var module in kvp.Value)
                {
                    if (module.IsVisible && module.View != null)
                    {
                        module.View.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// 设置指定层的可见性
        /// </summary>
        public void SetLayerVisible(UILayer layer, bool visible)
        {
            if (!_layers.TryGetValue(layer, out var list)) return;

            foreach (var module in list)
            {
                module.View?.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 获取指定层的所有模块（只读副本）
        /// </summary>
        public List<UIModuleBase> GetModulesInLayer(UILayer layer)
        {
            if (_layers.TryGetValue(layer, out var list))
                return new List<UIModuleBase>(list);
            return new List<UIModuleBase>();
        }

        /// <summary>
        /// 清除所有层级数据
        /// </summary>
        public void Clear()
        {
            _layers.Clear();
        }

        // ────────────────────────────────────────
        // 内部方法
        // ────────────────────────────────────────

        /// <summary>
        /// 重新排列指定层内所有面板的 SortingOrder
        /// </summary>
        private void RearrangeSortingOrder(UILayer layer)
        {
            if (!_layers.TryGetValue(layer, out var list)) return;

            int baseOrder = (int)layer;
            for (int i = 0; i < list.Count; i++)
            {
                var view = list[i].View;
                if (view != null)
                {
                    view.SortingOrder = baseOrder + i * OrderStep;
                }
            }
        }

        private static readonly Dictionary<Type, UIPanelAttribute> _attrCache = new();

        /// <summary>
        /// 通过反射获取模块的 UIPanelAttribute（带缓存）
        /// </summary>
        internal static UIPanelAttribute GetPanelAttribute(UIModuleBase module)
        {
            if (module == null) return null;
            var type = module.GetType();
            if (_attrCache.TryGetValue(type, out var cached))
                return cached;

            var attrs = type.GetCustomAttributes(typeof(UIPanelAttribute), false);
            var attr = attrs.Length > 0 ? attrs[0] as UIPanelAttribute : null;
            _attrCache[type] = attr;
            return attr;
        }
    }
}
