using UnityEngine;
using Game.Logic;
using System.Collections.Generic;

namespace Game.Logic
{
    /// <summary>
    /// 受击反应配置。定义角色级别的受击参数。
    /// 当前阶段为接口占位，具体逻辑后续实现。
    /// </summary>
    [CreateAssetMenu(fileName = "HitReactionConfig", menuName = "Config/Role/Hit Reaction Config")]
    public class HitReactionConfig : GameConfigAsset
    {
        [Header("受击动画")]
        [Tooltip("击退轻")]
        public ActionConfigAsset hitAnimLight;
        [Tooltip("击退重")]
        public ActionConfigAsset hitAnimHeavy;
        [Tooltip("击飞")]
        public ActionConfigAsset hitAnimKnowAway;

        [Tooltip("原地抖动")]
        public ActionConfigAsset hitAnimShake;
        [Tooltip("原地打断")]
        public ActionConfigAsset hitAnimStay;
        [Tooltip("击倒")]
        public ActionConfigAsset hitAnimKnockDown;

        [Header("击退（接口占位）")]
        public float knockbackForce = 0f;

        [Header("击飞（接口占位）")]
        public float launchForce = 0f;

        [Header("霸体阈值（接口占位）")]
        public float superArmorThreshold = 0f;
        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (hitAnimLight != null) yield return hitAnimLight;
            if (hitAnimHeavy != null) yield return hitAnimHeavy;
            if (hitAnimKnowAway != null) yield return hitAnimKnowAway;
            if (hitAnimShake != null) yield return hitAnimShake;
            if (hitAnimStay != null) yield return hitAnimStay;
            if (hitAnimKnockDown != null) yield return hitAnimKnockDown;
        }

        public ActionConfigAsset GetHitAction(cfg.ZZZ.HitReactionType type)
        {
            return type switch
            {
                cfg.ZZZ.HitReactionType.Light => hitAnimLight,
                cfg.ZZZ.HitReactionType.Heavy => hitAnimHeavy,
                cfg.ZZZ.HitReactionType.Launch => hitAnimKnowAway, // Note: Assuming KnowAway maps to Knockback/Launch
                cfg.ZZZ.HitReactionType.Shake => hitAnimShake,
                cfg.ZZZ.HitReactionType.KnockDown => hitAnimKnockDown,
                // TODO: add other types if necessary
                _ => hitAnimLight // Fallback
            };
        }
    }
}
