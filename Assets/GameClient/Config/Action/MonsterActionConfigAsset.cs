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
        // 怪物特定动作参数未来若有可在此扩展，打断力与抗打断韧性统一由 Luban 技能表驱动

    }
}
