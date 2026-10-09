using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 角色独有的动作配置 包含派生和连招路由
    /// </summary>
    [CreateAssetMenu(fileName = "RoleActionConfigAsset", menuName = "Config/Action/Role Action Config")]
    public class RoleActionConfigAsset : ActionConfigAsset
    {
        // 派生与继承
        [Header("派生与继承")]
        [Tooltip("当前动作对于前置动作派生路由的继承策略")]
        public RouteInheritMode InheritMode = RouteInheritMode.None;

        // 路由和通用路由集已在基类中声明与收集 派生路由继承已由解析器统一处理
    }
}
