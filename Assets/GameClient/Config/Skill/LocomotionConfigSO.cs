using UnityEngine;

namespace Game.Logic.Action.Config
{
    /// <summary>
    /// 移动配置，继承自通用全局行为配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewLocomotionConfig", menuName = "Config/Action/Locomotion Config")]
    public class LocomotionConfigSO : ActionConfigSO
    {
        [Header("Locomotion Params")]
        public float MoveSpeedMultiplier = 1.0f;
        public string BlendParamRule;
    }
}
