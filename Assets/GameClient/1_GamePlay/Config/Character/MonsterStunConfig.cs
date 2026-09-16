using System.Collections.Generic;
using UnityEngine;
using Game.Framework;
namespace Game.GamePlay
{
    /// <summary>
    /// 怪物失衡瘫痪表现配置资产。
    /// 集中管理失衡硬直、瘫痪循环与起身恢复的三段式动作资产。
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterStunConfig", menuName = "Config/Role/Monster Stun Config")]
    public class MonsterStunConfig : GameConfigAsset
    {
        [Header("失衡动作表现 (三段式)")]
        [Tooltip("失衡开始/跪地动作")]
        public ActionConfigAsset StunStart;

        [Tooltip("失衡虚弱循环动作")]
        public ActionConfigAsset StunLoop;

        [Tooltip("失衡结束/起身动作")]
        public ActionConfigAsset StunEnd;

        [Header("基础参数")]
        [Tooltip("默认失衡虚弱持续时长 (秒)")]
        public float DefaultStunDuration = 5.0f;

        /// <summary>
        /// 收集所有配置的动作资产，用于统一预热与资源依赖解析。
        /// </summary>
        public IEnumerable<ActionConfigAsset> GetAllActionConfigs()
        {
            if (StunStart != null) yield return StunStart;
            if (StunLoop != null) yield return StunLoop;
            if (StunEnd != null) yield return StunEnd;
        }
    }
}
