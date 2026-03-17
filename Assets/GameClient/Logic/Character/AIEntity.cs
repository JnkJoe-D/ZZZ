using Game.AI;
using UnityEngine;

namespace Game.Logic.Character
{
    /// <summary>
    /// AI 控制的实体。
    /// 在初始化时自动挂载 AI 输入组件。
    /// </summary>
    public class AIEntity : CharacterEntity
    {
        protected override void InitRequiredComponents()
        {
            // AI 特有的组件：AI 输入代理、基础运动控制
            InputProvider = gameObject.AddComponent<AIInputProvider>();

            MovementController = GetComponent<MovementController>();
            if (MovementController == null) MovementController = gameObject.AddComponent<MovementController>();

            // AI 通常不需要 CharacterCameraController，保持基类属性为 null 即可
            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();
        }
    }
}
