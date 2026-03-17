using UnityEngine;
using Game.Logic.Action.Config;

namespace Game.Logic.Character.Config
{
    /// <summary>
    /// 受击反应配置。定义角色级别的受击参数。
    /// 当前阶段为接口占位，具体逻辑后续实现。
    /// </summary>
    [CreateAssetMenu(fileName = "HitReactionConfig", menuName = "Config/Role/Hit Reaction Config")]
    public class HitReactionConfig : ScriptableObject
    {
        [Header("受击动画")]
        [Tooltip("前方受击")]
        public ActionConfigAsset hitAnimFront;
        [Tooltip("后方受击")]
        public ActionConfigAsset hitAnimBack;
        [Tooltip("左侧受击")]
        public ActionConfigAsset hitAnimLeft;
        [Tooltip("右侧受击")]
        public ActionConfigAsset hitAnimRight;

        [Header("击退（接口占位）")]
        public float knockbackForce = 0f;

        [Header("击飞（接口占位）")]
        public float launchForce = 0f;

        [Header("霸体阈值（接口占位）")]
        public float superArmorThreshold = 0f;
    }
}
