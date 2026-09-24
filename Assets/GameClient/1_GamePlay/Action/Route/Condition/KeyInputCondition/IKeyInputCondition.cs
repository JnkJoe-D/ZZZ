using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由输入条件接口,仅RoleEntity可用
    /// </summary>
    [ConditionScope(ConditionScope.Role)]
    public interface IKeyInputCondition
    {
        bool Check(RoleEntity actor);
    }
}
