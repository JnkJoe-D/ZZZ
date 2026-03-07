using UnityEngine;
using System.Collections.Generic;

namespace Game.Audio.Config
{
    /// <summary>
    /// 全局音频配置 (GlobalAudioConfigSO)
    /// 定位: A类资源，生命周期与游戏等长，由 ConfigManager 常驻加载。
    /// 包含极其通用的全局声音（UI点击、系统警告、大厅/登陆界面的 BGM 等）。
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalAudioConfigSO", menuName = "Config/Audio/Global Audio Config")]
    public class GlobalAudioConfigSO : ScriptableObject
    {
        [Header("UI & System SFX")]
        [Tooltip("通用按钮点击音效")]
        public AudioClip ButtonClickSound;
        
        [Tooltip("通用弹窗/对话框弹出音效")]
        public AudioClip PopupOpenSound;

        [Tooltip("通用错误提示音效")]
        public AudioClip ErrorPromptSound;

        [Tooltip("物品掉落/拾取音效")]
        public AudioClip ItemLootSound;
        
        [Tooltip("金币/货币获得音效")]
        public AudioClip CoinAcquireSound;

        [Header("Global Ambient / BGM")]
        [Tooltip("登录界面 BGM")]
        public AudioClip LoginSceneBGM;

        [Tooltip("主大厅 BGM")]
        public AudioClip MainLobbyBGM;

        // 【扩展建议】
        // 如果你的 UI 声音非常庞大，也可以改成字典或者数组+枚举来管理
        // 但对于最核心基础的几个声音，直接声明 public AudioClip 是最快最清晰的做法
    }
}
