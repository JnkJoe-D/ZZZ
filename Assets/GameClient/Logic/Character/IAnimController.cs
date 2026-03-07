using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// 标准化动画控制解耦接口
    /// 将业务与当前项目现有的 MAnimSystem（或任何未来可能的动画系统）彻底隔绝。
    /// </summary>
    public interface IAnimController
    {
        /// <summary>
        /// 播放指定动画片段
        /// 直接基于运行时硬引用，避免由 String 哈希带来的性能损耗与低级打字错误
        /// </summary>
        /// <param name="clip">动画剪辑的引用</param>
        /// <param name="fadeDuration">融合渐变时间（秒）</param>
        /// <param name="onFadeComplete">当底层真正的底层动画系统（如 MAnimSystem）汇报此次混越完成时触发</param>
        /// <param name="onAnimEnd">当底层真正的动画系统汇报动作播完了触发</param>
        void PlayAnim(AnimationClip clip, float fadeDuration = 0.2f, System.Action onFadeComplete = null, System.Action onAnimEnd = null, bool forceResetTime = false);
        
        /// <summary>
        /// 为刚刚下达播放指令的当前动画，基于其时间轴加挂自定义事件（完全隔离底层 AnimState 对象）
        /// </summary>
        /// <param name="time">距离动画开头的绝对时间点（秒）</param>
        /// <param name="callback">满足时间点时的回调逻辑</param>
        void AddEventToCurrentAnim(float time, System.Action callback);
        void SetSpeed(int layerIndex,float speed);
    }
}
