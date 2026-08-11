using System;
using Game.GraphTools;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 行为树节点的大类。
    /// </summary>
    public enum BehaviorTreeNodeKind
    {
        Root,
        Composite,
        Condition,
        Service,
        Action
    }

    /// <summary>
    /// 组合节点的执行模式。
    /// </summary>
    public enum BehaviorTreeCompositeMode
    {
        Sequence,
        Selector,
        Parallel
    }

    /// <summary>
    /// 条件节点的中断模式。
    /// </summary>
    public enum BehaviorTreeAbortMode
    {
        None,
        Self,
        LowerPriority,
        Both
    }

    /// <summary>
    /// 条件节点支持的比较操作。
    /// </summary>
    public enum BehaviorTreeComparisonOperator
    {
        IsSet,
        Equals,
        NotEquals,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// 条件节点右值来源。
    /// </summary>
    public enum BehaviorTreeConditionValueSource
    {
        Constant,
        BlackboardKey
    }

    [Serializable]
    /// <summary>
    /// 所有行为树作者态节点的公共基类。
    /// </summary>
    public abstract class BehaviorTreeNodeModelBase : GraphNodeModelBase
    {
        public string Description = string.Empty;
        public abstract BehaviorTreeNodeKind NodeKind { get; }
    }

    [GraphNodeDefinition("BehaviorTree/Root", typeof(BehaviorTreeGraphAsset), 0)]
    /// <summary>
    /// 作者态根节点。
    /// </summary>
    public sealed class BehaviorTreeRootNodeModel : BehaviorTreeNodeModelBase
    {
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Root;

        /// <summary>
        /// 创建根节点并设置默认标题。
        /// </summary>
        public BehaviorTreeRootNodeModel()
        {
            Title = "Root";
        }
    }

    [GraphNodeDefinition("BehaviorTree/Composite", typeof(BehaviorTreeGraphAsset), 10)]
    /// <summary>
    /// 作者态组合节点。
    /// </summary>
    public sealed class BehaviorTreeCompositeNodeModel : BehaviorTreeNodeModelBase
    {
        public BehaviorTreeCompositeMode CompositeMode = BehaviorTreeCompositeMode.Sequence;
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Composite;

        /// <summary>
        /// 创建组合节点并设置默认标题。
        /// </summary>
        public BehaviorTreeCompositeNodeModel()
        {
            Title = "Composite";
        }
    }

    [GraphNodeDefinition("BehaviorTree/Condition", typeof(BehaviorTreeGraphAsset), 20)]
    /// <summary>
    /// 作者态条件节点。
    /// </summary>
    public sealed class BehaviorTreeConditionNodeModel : BehaviorTreeNodeModelBase, ISerializationCallbackReceiver
    {
        public string BlackboardKey = string.Empty;
        public BehaviorTreeComparisonOperator Comparison = BehaviorTreeComparisonOperator.IsSet;
        public BehaviorTreeConditionValueSource ExpectedValueSource = BehaviorTreeConditionValueSource.Constant;
        public string ExpectedBlackboardKey = string.Empty;

        [HideInInspector]
        public string ExpectedValue = string.Empty;

        public BehaviorTreeValueData ExpectedValueData = BehaviorTreeValueData.CreateDefault(BehaviorTreeBlackboardValueType.String);
        public BehaviorTreeAbortMode AbortMode = BehaviorTreeAbortMode.Self;
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Condition;

        /// <summary>
        /// 创建条件节点并设置默认标题。
        /// </summary>
        public BehaviorTreeConditionNodeModel()
        {
            Title = "Condition";
        }

        /// <summary>
        /// 序列化前同步旧格式常量字符串。
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (ExpectedValueData == null)
            {
                ExpectedValueData = BehaviorTreeValueData.CreateDefault(BehaviorTreeBlackboardValueType.String);
            }

            ExpectedValue = ExpectedValueData.ToLegacyString();
        }

        /// <summary>
        /// 反序列化后恢复 typed value。
        /// </summary>
        public void OnAfterDeserialize()
        {
            ExpectedValueData ??= BehaviorTreeValueData.FromLegacyString(BehaviorTreeBlackboardValueType.String, ExpectedValue);
        }
    }

    [GraphNodeDefinition("BehaviorTree/Service", typeof(BehaviorTreeGraphAsset), 30)]
    /// <summary>
    /// 作者态服务节点。
    /// </summary>
    public sealed class BehaviorTreeServiceNodeModel : BehaviorTreeNodeModelBase
    {
        public string ServiceKey = string.Empty;
        public float IntervalSeconds = 0.25f;
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Service;

        /// <summary>
        /// 创建服务节点并设置默认标题。
        /// </summary>
        public BehaviorTreeServiceNodeModel()
        {
            Title = "Service";
        }
    }

    [GraphNodeDefinition("BehaviorTree/Action", typeof(BehaviorTreeGraphAsset), 40)]
    /// <summary>
    /// 作者态动作节点。
    /// </summary>
    public sealed class BehaviorTreeActionNodeModel : BehaviorTreeNodeModelBase
    {
        public string TaskKey = string.Empty;
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Action;

        /// <summary>
        /// 创建动作节点并设置默认标题。
        /// </summary>
        public BehaviorTreeActionNodeModel()
        {
            Title = "Action";
        }
    }
    [Serializable]
    [GraphNodeDefinition("BehaviorTree/Play Action", typeof(BehaviorTreeGraphAsset), 45)]
    /// <summary>
    /// 作者态直接播放动作节点。
    /// </summary>
    public sealed class BehaviorTreePlayActionNodeModel : BehaviorTreeNodeModelBase
    {
        public Game.Logic.ActionConfigAsset TargetAction;
        public override BehaviorTreeNodeKind NodeKind => BehaviorTreeNodeKind.Action;

        public BehaviorTreePlayActionNodeModel()
        {
            Title = "Play Action";
        }
    }
}
