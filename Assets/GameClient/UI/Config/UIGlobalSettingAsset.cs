using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 全局 UI 总配置资产 (类似 YooAsset 的 SettingSO 模式)
    /// 存放于 Assets/Resources/Settings/UIGlobalSettingSO.asset
    /// </summary>
    [CreateAssetMenu(fileName = "UIGlobalSettingSO", menuName = "Config/UI/UI Global Setting")]
    public class UIGlobalSettingAsset : ScriptableObject
    {
        [Header("按键显示配置 (Key Display Config)")]
        [Tooltip("按键与图标映射配置资产 (ScriptableObject)")]
        public KeyBindingDisplayConfigAsset KeyBindingConfig;

        [Header("通用 UI 参数与资产扩展")]
        [Tooltip("HUD 默认缩放")]
        public float HudDefaultScale = 1.0f;
    }
}
