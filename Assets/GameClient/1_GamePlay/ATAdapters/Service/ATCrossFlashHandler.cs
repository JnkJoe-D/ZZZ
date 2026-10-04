using UnityEngine;
using Game.Framework;
using ATEditor;

namespace Game.GamePlay
{
    /// <summary>
    /// ATEditor 十字闪光适配器。
    /// 贯彻 IService 契约，将时间轴的闪光指令通过领域事件分发至表现层，并携带 Attacker 实体引用以供时钟同步，严禁直接引用 View 层。
    /// </summary>
    public class ATCrossFlashHandler : ICrossFlashHandler
    {
        private readonly CharacterEntity _entity;

        public ATCrossFlashHandler(CharacterEntity entity)
        {
            _entity = entity;
        }

        public void TriggerCrossFlash(Vector3 worldPosition, Transform targetTransform, Vector3 positionOffset, in CrossFlashParameters parameters)
        {
            EventCenter.Publish(new CrossFlashTriggeredEvent
            {
                Attacker = _entity,
                WorldPosition = worldPosition,
                TargetTransform = targetTransform,
                PositionOffset = positionOffset,
                Parameters = parameters
            });
        }
    }
}
