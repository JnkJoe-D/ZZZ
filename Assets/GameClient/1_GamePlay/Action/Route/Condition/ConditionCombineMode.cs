using System;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 条件组合逻辑模式
    /// 用于控制多个输入条件或额外条件在路由评估时的布尔运算规则
    /// </summary>
    public enum ConditionCombineMode
    {
        [InspectorName("全部满足 (AND - 且)")]
        AllMatch_AND = 0,

        [InspectorName("任一满足 (OR - 或)")]
        AnyMatch_OR = 1,
    }

    /// <summary>
    /// 比较运算符枚举
    /// 供条件系统（如点击次数、数值比对）进行灵活的阈值判定
    /// </summary>
    public enum ConditionCompareOperator
    {
        [InspectorName("等于 (==)")]
        Equal = 0,

        [InspectorName("不等于 (!=)")]
        NotEqual = 1,

        [InspectorName("小于 (<)")]
        LessThan = 2,

        [InspectorName("小于等于 (<=)")]
        LessOrEqual = 3,

        [InspectorName("大于 (>)")]
        GreaterThan = 4,

        [InspectorName("大于等于 (>=)")]
        GreaterOrEqual = 5,
    }
}
