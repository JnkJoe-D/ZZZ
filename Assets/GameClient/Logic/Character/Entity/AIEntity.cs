using Game.AI;
using UnityEngine;

namespace Game.Logic
{
    public class AIEntity : CharacterEntity
    {
        protected override bool AutoAssignCameraOnStart => false;

        protected override void InitRequiredComponents()
        {
            SetInputProvider(gameObject.AddComponent<AIInputProvider>());

            MovementController = GetComponent<MovementController>();
            if (MovementController == null) MovementController = gameObject.AddComponent<MovementController>();

            HitReactionModule = GetComponent<HitReactionModule>();
            if (HitReactionModule == null) HitReactionModule = gameObject.AddComponent<HitReactionModule>();

            // 暂时禁用 FootIK 模块
            // FootIKModule = GetComponent<FootIKModule>();
            // if (FootIKModule == null) FootIKModule = GetComponentInChildren<FootIKModule>();
            // if (FootIKModule == null) FootIKModule = gameObject.AddComponent<FootIKModule>();
        }
    }
}
