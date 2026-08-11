using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.AI
{
    [Serializable]
    /// <summary>
    /// 编译后的整棵行为树定义，是运行时真正执行的数据。
    /// </summary>
    public sealed class BehaviorTreeDefinition
    {
        public string RootNodeId = string.Empty;
        public List<BehaviorTreeDefinitionNode> Nodes = new List<BehaviorTreeDefinitionNode>();
        public List<BehaviorTreeBlackboardEntry> Blackboard = new List<BehaviorTreeBlackboardEntry>();
    }

    [Serializable]
    /// <summary>
    /// 编译后的单个行为树节点定义。
    /// </summary>
    public sealed class BehaviorTreeDefinitionNode : ISerializationCallbackReceiver
    {
        public string NodeId = string.Empty;
        public string ParentNodeId = string.Empty;
        public int ChildIndex = -1;
        public string Title = string.Empty;
        public BehaviorTreeNodeKind NodeKind;
        public string Description = string.Empty;
        public BehaviorTreeCompositeMode CompositeMode;
        public BehaviorTreeAbortMode AbortMode;
        public BehaviorTreeComparisonOperator Comparison;
        public BehaviorTreeConditionValueSource ExpectedValueSource;
        public string BlackboardKey = string.Empty;
        public string ExpectedBlackboardKey = string.Empty;

        [HideInInspector]
        public string ExpectedValue = string.Empty;

        public BehaviorTreeValueData ExpectedValueData = BehaviorTreeValueData.CreateDefault(BehaviorTreeBlackboardValueType.String);
        public string ServiceKey = string.Empty;
        public float IntervalSeconds;
        public string TaskKey = string.Empty;
        public Game.Logic.ActionConfigAsset TargetAction;
        public List<string> Children = new List<string>();

        /// <summary>
        /// 序列化前同步旧格式期望值字符串。
        /// </summary>
        public void OnBeforeSerialize()
        {
            ExpectedValueData ??= BehaviorTreeValueData.CreateDefault(BehaviorTreeBlackboardValueType.String);
            ExpectedValue = ExpectedValueData.ToLegacyString();
        }

        /// <summary>
        /// 反序列化后恢复 typed expected value。
        /// </summary>
        public void OnAfterDeserialize()
        {
            ExpectedValueData ??= BehaviorTreeValueData.FromLegacyString(BehaviorTreeBlackboardValueType.String, ExpectedValue);
        }
    }
}
