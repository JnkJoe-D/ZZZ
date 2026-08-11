using System.Collections.Generic;
using System.Linq;
using Game.GraphTools;

namespace Game.AI
{
    /// <summary>
    /// 行为树图编译器：把作者态图数据转换为运行时定义。
    /// </summary>
    public sealed class BehaviorTreeGraphCompiler : IGraphCompiler<BehaviorTreeGraphAsset>
    {
        /// <summary>
        /// 编译一张行为树图资产。
        /// </summary>
        /// <param name="graphAsset">要编译的行为树图。</param>
        /// <returns>编译结果报告。</returns>
        public GraphCompileReport Compile(BehaviorTreeGraphAsset graphAsset)
        {
            GraphCompileReport report = new GraphCompileReport();
            GraphValidationResult validation = BehaviorTreeGraphValidator.Validate(graphAsset);
            report.Merge(validation);
            if (graphAsset == null || validation.HasErrors)
            {
                return report;
            }

            Dictionary<string, List<BehaviorTreeChildEdgeModel>> edgesByParent = graphAsset.ChildEdges
                .Where(edge => edge != null && edge.IsEnabled)
                .GroupBy(edge => edge.OutputNodeId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(edge => edge.ChildIndex).ThenBy(edge => edge.SortOrder).ToList());

            Dictionary<string, (string parentNodeId, int childIndex)> parentByNodeId =
                new Dictionary<string, (string parentNodeId, int childIndex)>();

            foreach (KeyValuePair<string, List<BehaviorTreeChildEdgeModel>> pair in edgesByParent)
            {
                string parentNodeId = pair.Key;
                List<BehaviorTreeChildEdgeModel> childEdges = pair.Value;
                for (int childIndex = 0; childIndex < childEdges.Count; childIndex++)
                {
                    BehaviorTreeChildEdgeModel childEdge = childEdges[childIndex];
                    if (childEdge == null || string.IsNullOrWhiteSpace(childEdge.InputNodeId))
                    {
                        continue;
                    }

                    parentByNodeId[childEdge.InputNodeId] = (parentNodeId, childIndex);
                }
            }

            BehaviorTreeDefinition definition = new BehaviorTreeDefinition
            {
                RootNodeId = graphAsset.RootNode?.NodeId ?? string.Empty,
                Blackboard = graphAsset.BlackboardEntries
                    .Select(entry => new BehaviorTreeBlackboardEntry
                    {
                        Key = entry.Key,
                        DisplayName = entry.DisplayName,
                        SerializedTypeName = entry.SerializedTypeName,
                        ValueType = entry.ValueType,
                        DefaultValueData = entry.DefaultValueData?.Clone() ?? BehaviorTreeValueData.CreateDefault(entry.ValueType)
                    })
                    .ToList()
            };

            foreach (BehaviorTreeNodeModelBase node in graphAsset.BehaviorNodes.Where(node => node != null))
            {
                string parentNodeId = string.Empty;
                int childIndex = -1;
                if (parentByNodeId.TryGetValue(node.NodeId, out (string parentNodeId, int childIndex) parentInfo))
                {
                    parentNodeId = parentInfo.parentNodeId;
                    childIndex = parentInfo.childIndex;
                }

                BehaviorTreeDefinitionNode definitionNode = new BehaviorTreeDefinitionNode
                {
                    NodeId = node.NodeId,
                    ParentNodeId = parentNodeId,
                    ChildIndex = childIndex,
                    Title = node.Title,
                    NodeKind = node.NodeKind,
                    Description = node.Description
                };

                if (edgesByParent.TryGetValue(node.NodeId, out List<BehaviorTreeChildEdgeModel> childEdges))
                {
                    definitionNode.Children.AddRange(childEdges.Select(edge => edge.InputNodeId));
                }

                switch (node)
                {
                    case BehaviorTreeCompositeNodeModel compositeNode:
                        definitionNode.CompositeMode = compositeNode.CompositeMode;
                        break;
                    case BehaviorTreeConditionNodeModel conditionNode:
                        definitionNode.AbortMode = conditionNode.AbortMode;
                        definitionNode.BlackboardKey = conditionNode.BlackboardKey;
                        definitionNode.Comparison = conditionNode.Comparison;
                        definitionNode.ExpectedValueSource = conditionNode.ExpectedValueSource;
                        definitionNode.ExpectedBlackboardKey = conditionNode.ExpectedBlackboardKey;
                        definitionNode.ExpectedValueData = ResolveExpectedValue(graphAsset, conditionNode);
                        break;
                    case BehaviorTreeServiceNodeModel serviceNode:
                        definitionNode.ServiceKey = serviceNode.ServiceKey;
                        definitionNode.IntervalSeconds = serviceNode.IntervalSeconds;
                        break;
                    case BehaviorTreeActionNodeModel actionNode:
                        definitionNode.TaskKey = actionNode.TaskKey;
                        break;
                    case BehaviorTreePlayActionNodeModel playActionNode:
                        definitionNode.TaskKey = "character.play_action";
                        definitionNode.TargetAction = playActionNode.TargetAction;
                        break;
                }

                definition.Nodes.Add(definitionNode);
            }

            graphAsset.CompiledDefinition = definition;
            report.AddInfo("bt.compile.success", $"Compiled {definition.Nodes.Count} behavior tree nodes.");
            return report;
        }

        /// <summary>
        /// 解析条件节点的期望值，并按黑板定义同步类型。
        /// </summary>
        /// <param name="graphAsset">当前行为树图资产。</param>
        /// <param name="conditionNode">条件节点模型。</param>
        /// <returns>编译后的期望值数据。</returns>
        private static BehaviorTreeValueData ResolveExpectedValue(
            BehaviorTreeGraphAsset graphAsset,
            BehaviorTreeConditionNodeModel conditionNode)
        {
            BehaviorTreeValueData valueData = conditionNode.ExpectedValueData?.Clone()
                ?? BehaviorTreeValueData.FromLegacyString(BehaviorTreeBlackboardValueType.String, conditionNode.ExpectedValue);

            BehaviorTreeBlackboardEntry referencedEntry = graphAsset.BlackboardEntries
                .FirstOrDefault(entry => entry != null && entry.Key == conditionNode.BlackboardKey);

            if (referencedEntry != null && valueData.ValueType != referencedEntry.ValueType)
            {
                valueData.SetValueType(referencedEntry.ValueType, true);
            }

            return valueData;
        }
    }
}
