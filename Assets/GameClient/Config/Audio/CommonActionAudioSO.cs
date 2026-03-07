using UnityEngine;

namespace Game.Audio.Config
{
    /// <summary>
    /// 通用动作/战斗音效库 (CommonActionAudioSO)
    /// 采用 AudioBank 模式，收集游戏内高频复用的战斗环境或动作声音。
    /// 生命周期：可以随关卡/场景加载，也可以常驻，由游戏体量决定。
    /// </summary>
    [CreateAssetMenu(fileName = "CommonActionAudioSO", menuName = "Config/Audio/Common Action Audio Box")]
    public class CommonActionAudioSO : ScriptableObject
    {
        [Header("材质脚步声 (Footsteps)")]
        [Tooltip("默认脚步声组合 (可随机播放)")]
        public AudioClip[] Footstep;
        [Tooltip("草地脚步声组合")]
        public AudioClip[] FootstepGrass;
        
        [Tooltip("石板/混凝土脚步声组合")]
        public AudioClip[] FootstepStone;
        
        [Tooltip("泥地/水坑脚步声组合")]
        public AudioClip[] FootstepWater;

        [Tooltip("停下时脚步音效")]
        public AudioClip[] FootstepEnd;

        [Header("通用受击与打击声 (Impacts)")]
        [Tooltip("利刃切割肉体音效")]
        public AudioClip[] HitFleshSlash;
        
        [Tooltip("钝器重击肉体音效")]
        public AudioClip[] HitFleshBlunt;
        
        [Tooltip("金属格挡/冷兵器碰撞声")]
        public AudioClip[] HitMetalClang;

        [Header("基础环境与动作 (Movement & Foley)")]
        [Tooltip("默认前闪避风声")]
        public AudioClip[] DodgeFront;
        [Tooltip("默认后闪避风声")]
        public AudioClip[] DodgeBack;
        [Tooltip("基础翻滚/闪避摩擦声 (布料/皮甲)")]
        public AudioClip[] DodgeSwoosh;
        
        [Tooltip("高空落地/受身声")]
        public AudioClip[] LandingHeavy;
        
        [Tooltip("轻质跳跃起跳声")]
        public AudioClip[] JumpStart;

        /// <summary>
        /// 提供一个获取随机 Clip 的辅助方法，方便外部直接调用
        /// </summary>
        public AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
