using UnityEngine;
using Game.Framework;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// 全屏十字闪光触发领域事件。
    /// 由 ATCrossFlashHandler 广播，表现层 CrossFlashPresenterComponent 监听并消费。
    /// 贯彻单向依赖与表现层解耦契约。
    /// </summary>
    public struct CrossFlashTriggeredEvent : IGameEvent
    {
        public CharacterEntity Attacker;
        public Vector3 WorldPosition;
        public Transform TargetTransform;
        public Vector3 PositionOffset;
        public CrossFlashParameters Parameters;
    }
}
