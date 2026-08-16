using UnityEngine;
using ATEditor;
using System.Collections.Generic;

namespace Game.Logic
{
    /// <summary>
    /// 怪物独有的动作配置
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterActionConfigAsset", menuName = "Config/Action/Monster Action Config")]
    public class MonsterActionConfigAsset : ActionConfigAsset
    {
        [Header("怪物特定动作参数")]
        [Tooltip("该动作是否可以被轻受击打断")]
        public bool CanBeInterruptedByLightHit = true;
        
        [Tooltip("该动作是否可以被重受击打断")]
        public bool CanBeInterruptedByHeavyHit = true;

        public override void CollectEffectiveRoutes(List<ActionRoute> results, RoleEntity actor = null)
        {
            // 怪物动作暂时不需要类似玩家复杂的派生继承机制，目前直接留空。
            // 后续如果有需要，可在这里实现怪物的连招或状态转移路由收集。
        }
    }
}
