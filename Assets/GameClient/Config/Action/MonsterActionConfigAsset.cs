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

    }
}
