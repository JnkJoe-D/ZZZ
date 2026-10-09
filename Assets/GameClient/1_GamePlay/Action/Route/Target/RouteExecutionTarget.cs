using System;
using Game.Framework;
using UnityEngine;

namespace Game.GamePlay
{
    /// <summary>
    /// 路由执行目标抽象基类
    /// 封装路由在评估命中并仲裁胜出后需要执行的具体目标 如执行动作或触发系统事件等
    /// 支持在同一条路由内组合多个执行目标 并按列表索引顺序执行
    /// </summary>
    [Serializable]
    public abstract class RouteExecutionTarget
    {
        /// <summary>
        /// 目标在编辑器或调试时的显示名称
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 当前动作周期内最大执行次数配额
        /// 大于零表示严格限制最大执行次数 例如一表示仅执行一次
        /// 小于等于零表示无限制 每帧满足条件均可重复执行 保留周期性广播能力
        /// </summary>
        public abstract int MaxExecuteCount { get; set; }

        /// <summary>
        /// 校验配置是否合法有效
        /// </summary>
        public abstract bool IsValid();
    }

    /// <summary>
    /// 动作执行目标
    /// 切换并播放指定的目标动作配置资产
    /// </summary>
    [Serializable]
    [SubclassDisplayName("执行动作")]
    public sealed class ActionRouteTarget : RouteExecutionTarget
    {
        public override string DisplayName => Action != null ? $"动作: {Action.name}" : "动作: 未配置";

        [Tooltip("目标动作配置资产")]
        public ActionConfigAsset Action;

        [Tooltip("是否需要校验下一个动作在配表中配置的释放条件如能量或耐力要求以及执行消耗 默认为真 若为假则无需校验条件且不扣除配表消耗 可直接释放")]
        public bool ValidateSkillRequirement = true;

        /// <summary>
        /// 动作切换天然具有排他性与状态迁移属性 在当前动作生命周期内固定且必须只能执行一次
        /// </summary>
        public override int MaxExecuteCount
        {
            get => 1;
            set { } // 忽略外部设置 强制锁定为一
        }

        public override bool IsValid() => Action != null;
    }

    /// <summary>
    /// 事件执行目标
    /// 发出指定的系统路由事件 如时间轴跳跃或招架支援或连携技触发等
    /// </summary>
    [Serializable]
    [SubclassDisplayName("执行事件")]
    public sealed class EventRouteTarget : RouteExecutionTarget
    {
        public override string DisplayName => $"事件: {RouteExecuteEvent}";

        [Tooltip("触发的系统路由事件")]
        public ExecuteEvent RouteExecuteEvent = ExecuteEvent.None;

        [Tooltip("当前动作周期内最大执行次数 默认为一 彻底解决每帧重复触发问题 小于等于零表示无限制允许重复执行")]
        [SerializeField]
        private int _executeCountLimit = 1;

        public override int MaxExecuteCount
        {
            get => _executeCountLimit;
            set => _executeCountLimit = value;
        }

        public override bool IsValid() => RouteExecuteEvent != ExecuteEvent.None;
    }
}
