namespace Game.Framework
{
    /// <summary>
    /// 客户端硬件渲染帧驱动契约。
    /// 仅适用于：高频按键轮询、相机平滑追踪、视觉插值回正、UI动效。
    /// 随客户端渲染帧率变动（30~240Hz），不受玩法逻辑时钟缩放影响。
    /// </summary>
    public interface IRenderTickable
    {
        void OnRenderTick(float unscaledDeltaTime);
    }
}
