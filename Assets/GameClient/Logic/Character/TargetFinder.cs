using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.Character
{
    [Serializable]
    public class TargetSearchConfig
    {
        [Tooltip("搜索半径")]
        public float SearchRadius = 15f;
        [Tooltip("搜索的层级过滤")]
        public LayerMask SearchLayerMask = -1; // 默认 All
        [Tooltip("优先级标签，越靠前获取时优先级越高")]
        public List<string> PriorityTags = new List<string> { "Enemy" };
    }

    /// <summary>
    /// 索敌类，用于获取范围内最新的优先目标
    /// </summary>
    public class TargetFinder : MonoBehaviour
    {
        public TargetSearchConfig config = new TargetSearchConfig();

        /// <summary>
        /// 暂定获取 Enemy 标签或优先配置标签里分值最高的最近目标
        /// </summary>
        public Transform GetEnemy()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, config.SearchRadius, config.SearchLayerMask);
            Transform bestTarget = null;
            int bestPriority = int.MaxValue;
            float closestSqrDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.gameObject == this.gameObject) continue;

                int priority = config.PriorityTags.IndexOf(col.tag);
                
                // 如果对象的 Tag 不在优先级配置列表中，退回兼容用户的暂定要求 (Enemy 标签)
                if (priority == -1)
                {
                    if (col.CompareTag("Enemy"))
                        priority = 999; // 给定一个较低的默认优先级
                    else
                        continue;
                }

                float sqrDist = (col.transform.position - transform.position).sqrMagnitude;

                // 优先级数值越小越优先（索引靠前）
                if (priority < bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
                else if (priority == bestPriority && sqrDist < closestSqrDist) // 同等优先级取距离最近
                {
                    bestTarget = col.transform;
                    closestSqrDist = sqrDist;
                }
            }

            return bestTarget;
        }
    }
}
