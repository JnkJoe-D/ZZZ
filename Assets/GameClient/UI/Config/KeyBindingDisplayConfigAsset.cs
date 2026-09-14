using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// 单个按键的展示配置项 (支持文本与原版图标)
    /// </summary>
    [Serializable]
    public class KeyDisplayItem
    {
        [Tooltip("Input System 控件路径，例如 <Keyboard>/e, <Mouse>/leftButton, <Gamepad>/buttonSouth 等")]
        public string Path;

        [Tooltip("友好中文显示文本 (用于文本提示或无图标时的兜底)")]
        public string DisplayText;

        [Tooltip("按键图标 Sprite (原版按键底框/按键图标，可在 Inspector 直观拖拽)")]
        public Sprite KeyIcon;

        [Tooltip("按键按下态图标 Sprite (可选，用于按键交互动效)")]
        public Sprite KeyPressedIcon;
    }

    /// <summary>
    /// 全局按键与图标映射配置资产 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "KeyBindingDisplayConfig", menuName = "Config/UI/Key Binding Display Config")]
    public class KeyBindingDisplayConfigAsset : ScriptableObject
    {
        [Header("按键映射列表 (覆盖 26 字母、鼠标、主/小键盘、控制键与手柄)")]
        [SerializeField]
        private List<KeyDisplayItem> _items = new();

        // 运行时快速检索字典 (忽略大小写)
        [NonSerialized]
        private Dictionary<string, KeyDisplayItem> _lookupCache;

        /// <summary>
        /// 获取所有配置项列表
        /// </summary>
        public IReadOnlyList<KeyDisplayItem> Items => _items;

        private void OnEnable()
        {
            _lookupCache = null;
        }

        /// <summary>
        /// 确保运行时字典缓存已构建
        /// </summary>
        public void EnsureLookupInitialized()
        {
            if (_lookupCache != null) return;

            _lookupCache = new Dictionary<string, KeyDisplayItem>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || string.IsNullOrEmpty(item.Path)) continue;

                // 注册完整路径 (如 "<Keyboard>/e")
                _lookupCache[item.Path] = item;

                // 注册去掉设备前缀后的控件名 (如 "e", "leftButton", "space")，增强容错
                int lastSlash = item.Path.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < item.Path.Length - 1)
                {
                    string controlName = item.Path.Substring(lastSlash + 1);
                    if (!_lookupCache.ContainsKey(controlName))
                    {
                        _lookupCache[controlName] = item;
                    }
                }
            }
        }

        /// <summary>
        /// 根据 InputControl 路径查询按键配置项 (O(1) 检索)
        /// </summary>
        public bool TryGetDisplayItem(string controlPath, out KeyDisplayItem item)
        {
            EnsureLookupInitialized();
            if (string.IsNullOrEmpty(controlPath))
            {
                item = null;
                return false;
            }

            return _lookupCache.TryGetValue(controlPath, out item);
        }

        /// <summary>
        /// 根据 InputAction 提取对应的展示配置项 (带多层兜底)
        /// </summary>
        public KeyDisplayItem GetDisplayItem(InputAction action)
        {
            if (action == null || action.bindings.Count == 0)
            {
                return new KeyDisplayItem { DisplayText = "—" };
            }

            EnsureLookupInitialized();

            // 1. 遍历有效绑定项
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite) continue;

                string path = binding.effectivePath;
                if (!string.IsNullOrEmpty(path) && TryGetDisplayItem(path, out var matchedItem))
                {
                    return matchedItem;
                }
            }

            // 2. 兜底回退：通过 Input System 原生 API 获取可读字符串
            string nativeStr = action.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions |
                InputBinding.DisplayStringOptions.IgnoreBindingOverrides);

            string friendly = ConvertNativeDisplayString(nativeStr);
            return new KeyDisplayItem
            {
                DisplayText = string.IsNullOrEmpty(friendly) ? "—" : friendly
            };
        }

        /// <summary>
        /// 快捷获取 InputAction 对应的友好显示文本
        /// </summary>
        public string GetFriendlyName(InputAction action)
        {
            var item = GetDisplayItem(action);
            return string.IsNullOrEmpty(item?.DisplayText) ? "—" : item.DisplayText;
        }

        private static string ConvertNativeDisplayString(string nativeStr)
        {
            if (string.IsNullOrWhiteSpace(nativeStr)) return "—";

            if (nativeStr.Equals("Left Button", StringComparison.OrdinalIgnoreCase)) return "鼠标左键";
            if (nativeStr.Equals("Right Button", StringComparison.OrdinalIgnoreCase)) return "鼠标右键";
            if (nativeStr.Equals("Middle Button", StringComparison.OrdinalIgnoreCase)) return "鼠标中键";
            if (nativeStr.Equals("Space", StringComparison.OrdinalIgnoreCase)) return "空格";
            if (nativeStr.Equals("Delta", StringComparison.OrdinalIgnoreCase)) return "鼠标移动";

            return nativeStr;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器实用功能：一键预填充 26 个字母与全部常用键鼠预置模板
        /// </summary>
        [ContextMenu("Populate Standard Keys (填充标准键鼠映射模板)")]
        public void PopulateStandardKeys()
        {
            var map = new Dictionary<string, string>
            {
                // 鼠标按键
                { "<Mouse>/leftButton", "鼠标左键" },
                { "<Mouse>/rightButton", "鼠标右键" },
                { "<Mouse>/middleButton", "鼠标中键" },
                { "<Mouse>/forwardButton", "鼠标侧键向前" },
                { "<Mouse>/backButton", "鼠标侧键向后" },
                { "<Mouse>/press", "鼠标点击" },
                { "<Mouse>/scroll/up", "滚轮向上" },
                { "<Mouse>/scroll/down", "滚轮向下" },

                // 26 个字母
                { "<Keyboard>/a", "A" }, { "<Keyboard>/b", "B" }, { "<Keyboard>/c", "C" },
                { "<Keyboard>/d", "D" }, { "<Keyboard>/e", "E" }, { "<Keyboard>/f", "F" },
                { "<Keyboard>/g", "G" }, { "<Keyboard>/h", "H" }, { "<Keyboard>/i", "I" },
                { "<Keyboard>/j", "J" }, { "<Keyboard>/k", "K" }, { "<Keyboard>/l", "L" },
                { "<Keyboard>/m", "M" }, { "<Keyboard>/n", "N" }, { "<Keyboard>/o", "O" },
                { "<Keyboard>/p", "P" }, { "<Keyboard>/q", "Q" }, { "<Keyboard>/r", "R" },
                { "<Keyboard>/s", "S" }, { "<Keyboard>/t", "T" }, { "<Keyboard>/u", "U" },
                { "<Keyboard>/v", "V" }, { "<Keyboard>/w", "W" }, { "<Keyboard>/x", "X" },
                { "<Keyboard>/y", "Y" }, { "<Keyboard>/z", "Z" },

                // 主键盘数字
                { "<Keyboard>/0", "0" }, { "<Keyboard>/1", "1" }, { "<Keyboard>/2", "2" },
                { "<Keyboard>/3", "3" }, { "<Keyboard>/4", "4" }, { "<Keyboard>/5", "5" },
                { "<Keyboard>/6", "6" }, { "<Keyboard>/7", "7" }, { "<Keyboard>/8", "8" },
                { "<Keyboard>/9", "9" },
                { "<Keyboard>/digit0", "0" }, { "<Keyboard>/digit1", "1" }, { "<Keyboard>/digit2", "2" },
                { "<Keyboard>/digit3", "3" }, { "<Keyboard>/digit4", "4" }, { "<Keyboard>/digit5", "5" },
                { "<Keyboard>/digit6", "6" }, { "<Keyboard>/digit7", "7" }, { "<Keyboard>/digit8", "8" },
                { "<Keyboard>/digit9", "9" },

                // 小键盘
                { "<Keyboard>/numpad0", "小键盘0" }, { "<Keyboard>/numpad1", "小键盘1" },
                { "<Keyboard>/numpad2", "小键盘2" }, { "<Keyboard>/numpad3", "小键盘3" },
                { "<Keyboard>/numpad4", "小键盘4" }, { "<Keyboard>/numpad5", "小键盘5" },
                { "<Keyboard>/numpad6", "小键盘6" }, { "<Keyboard>/numpad7", "小键盘7" },
                { "<Keyboard>/numpad8", "小键盘8" }, { "<Keyboard>/numpad9", "小键盘9" },
                { "<Keyboard>/numpadPeriod", "小键盘." },
                { "<Keyboard>/numpadDivide", "小键盘/" },
                { "<Keyboard>/numpadMultiply", "小键盘*" },
                { "<Keyboard>/numpadMinus", "小键盘-" },
                { "<Keyboard>/numpadPlus", "小键盘+" },
                { "<Keyboard>/numpadEnter", "小键盘回车" },

                // 控制键
                { "<Keyboard>/space", "空格" },
                { "<Keyboard>/leftShift", "Shift" },
                { "<Keyboard>/rightShift", "右Shift" },
                { "<Keyboard>/shift", "Shift" },
                { "<Keyboard>/leftCtrl", "Ctrl" },
                { "<Keyboard>/rightCtrl", "右Ctrl" },
                { "<Keyboard>/ctrl", "Ctrl" },
                { "<Keyboard>/leftAlt", "Alt" },
                { "<Keyboard>/rightAlt", "右Alt" },
                { "<Keyboard>/alt", "Alt" },
                { "<Keyboard>/tab", "Tab" },
                { "<Keyboard>/capsLock", "大写锁定" },
                { "<Keyboard>/enter", "回车" },
                { "<Keyboard>/escape", "Esc" },
                { "<Keyboard>/backspace", "退格" },

                // 编辑与方向键
                { "<Keyboard>/delete", "Delete" },
                { "<Keyboard>/insert", "Insert" },
                { "<Keyboard>/home", "Home" },
                { "<Keyboard>/end", "End" },
                { "<Keyboard>/pageUp", "PageUp" },
                { "<Keyboard>/pageDown", "PageDown" },
                { "<Keyboard>/upArrow", "↑" },
                { "<Keyboard>/downArrow", "↓" },
                { "<Keyboard>/leftArrow", "←" },
                { "<Keyboard>/rightArrow", "→" },

                // 功能键
                { "<Keyboard>/f1", "F1" }, { "<Keyboard>/f2", "F2" }, { "<Keyboard>/f3", "F3" },
                { "<Keyboard>/f4", "F4" }, { "<Keyboard>/f5", "F5" }, { "<Keyboard>/f6", "F6" },
                { "<Keyboard>/f7", "F7" }, { "<Keyboard>/f8", "F8" }, { "<Keyboard>/f9", "F9" },
                { "<Keyboard>/f10", "F10" }, { "<Keyboard>/f11", "F11" }, { "<Keyboard>/f12", "F12" },

                // 手柄常用键
                { "<Gamepad>/buttonSouth", "A" },
                { "<Gamepad>/buttonEast", "B" },
                { "<Gamepad>/buttonWest", "X" },
                { "<Gamepad>/buttonNorth", "Y" },
                { "<Gamepad>/leftShoulder", "LB" },
                { "<Gamepad>/rightShoulder", "RB" },
                { "<Gamepad>/leftTrigger", "LT" },
                { "<Gamepad>/rightTrigger", "RT" },
                { "<Gamepad>/dpad/up", "十字键↑" },
                { "<Gamepad>/dpad/down", "十字键↓" },
                { "<Gamepad>/dpad/left", "十字键←" },
                { "<Gamepad>/dpad/right", "十字键→" }
            };

            var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _items.Count; i++)
            {
                if (!string.IsNullOrEmpty(_items[i]?.Path))
                    existingPaths.Add(_items[i].Path);
            }

            foreach (var kvp in map)
            {
                if (!existingPaths.Contains(kvp.Key))
                {
                    _items.Add(new KeyDisplayItem
                    {
                        Path = kvp.Key,
                        DisplayText = kvp.Value
                    });
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[KeyBindingDisplayConfigAsset] 标准键位模板填充完成，当前共有 {_items.Count} 项。");
        }
        #endif
    }
}
