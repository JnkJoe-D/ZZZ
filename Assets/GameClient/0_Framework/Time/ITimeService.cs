using System;

namespace Game.Framework
{
    /// <summary>
    /// 核心时钟服务接口（抽象契约）。
    /// 提供多层级时间缩放、统一流速时间读取以及时钟生命周期事件。
    /// </summary>
    public interface ITimeService
    {
        float GlobalTimeScale { get; set; }
        float GameplayTimeScale { get; set; }
        float UITimeScale { get; set; }

        float FinalGameplayScale { get; }
        float FinalUIScale { get; }

        float GameplayTime { get; }
        float UITime { get; }

        int TargetLogicFrameRate { get; set; }
        float LogicDeltaTime { get; }

        event Action<float> OnGameplayLogicTick;
        event Action<float> OnUIRenderTick;

        void PauseGameplay();
        void ResumeGameplay();
        void ResetToNormal();
    }
}
