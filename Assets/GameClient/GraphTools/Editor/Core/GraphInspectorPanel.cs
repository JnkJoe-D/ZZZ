using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Collections;
using Game.AI;

namespace Game.GraphTools.Editor
{
    public class GraphInspectorPanel : VisualElement
    {
        private readonly Label headerLabel;
        private readonly VisualElement bodyContainer;
        private GraphAssetBase currentGraph;
        private object currentSelection;
        private Action<string, Action> applyChange;
        private Action<object> requestSelection;

        public GraphInspectorPanel()
        {
            style.marginBottom = 8f;
            style.paddingLeft = 6f;
            style.paddingRight = 6f;

            headerLabel = new Label("Inspector");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(headerLabel);

            bodyContainer = new VisualElement();
            bodyContainer.style.marginTop = 4f;
            Add(bodyContainer);
        }

        public void BindGraph(GraphAssetBase graphAsset)
        {
            BindSelection(graphAsset, graphAsset, null, null);
        }

        public void BindSelection(
            GraphAssetBase graphAsset,
            object selectionModel,
            Action<string, Action> onApplyChange,
            Action<object> onRequestSelection)
        {
            currentGraph = graphAsset;
            currentSelection = selectionModel ?? graphAsset;
            applyChange = onApplyChange;
            requestSelection = onRequestSelection;
            Rebuild();
        }

        private void Rebuild()
        {
            bodyContainer.Clear();

            if (currentGraph == null)
            {
                AddMessage("No graph selected.");
                return;
            }

            switch (currentSelection)
            {
                case BehaviorTreeCompositeNodeModel compositeNode:
                    BuildGraphNodeInspector(compositeNode);
                    BuildCompositeInspector(compositeNode);
                    BuildBehaviorTreeChildrenInspector(compositeNode);
                    break;
                case BehaviorTreeConditionNodeModel conditionNode:
                    BuildGraphNodeInspector(conditionNode);
                    BuildConditionInspector(conditionNode);
                    break;
                case BehaviorTreeServiceNodeModel serviceNode:
                    BuildGraphNodeInspector(serviceNode);
                    BuildServiceInspector(serviceNode);
                    break;
                case BehaviorTreeActionNodeModel actionNode:
                    BuildGraphNodeInspector(actionNode);
                    BuildActionInspector(actionNode);
                    break;
                case BehaviorTreePlayActionNodeModel playActionNode:
                    BuildGraphNodeInspector(playActionNode);
                    BuildPlayActionInspector(playActionNode);
                    break;
                case BehaviorTreeBlackboardEntry blackboardEntry:
                    BuildBehaviorTreeBlackboardInspector(blackboardEntry);
                    break;
                case BehaviorTreeRootNodeModel rootNode:
                    BuildGraphNodeInspector(rootNode);
                    BuildBehaviorTreeChildrenInspector(rootNode);
                    break;
                case GraphNodeModelBase nodeModel:
                    BuildGraphNodeInspector(nodeModel);
                    break;
                case GraphEdgeModelBase edgeModel:
                    BuildGraphEdgeInspector(edgeModel);
                    break;
                default:
                    BuildGraphSummary();
                    break;
            }
        }

        private void BuildGraphSummary()
        {
            headerLabel.text = "Inspector";
            AddReadOnlyField("Name", currentGraph.name);
            AddReadOnlyField("Graph Id", currentGraph.GraphId);
            AddReadOnlyField("Nodes", currentGraph.Nodes.Count.ToString());
            AddReadOnlyField("Edges", currentGraph.Edges.Count.ToString());
            AddReadOnlyField("Blackboard", currentGraph.Blackboard.Count.ToString());
        }

        private void BuildGraphNodeInspector(GraphNodeModelBase model)
        {
            headerLabel.text = "Inspector - Node";
            AddReadOnlyField("Node Type", model.GetType().Name);
            AddReadOnlyField("Node Id", model.NodeId);

            bodyContainer.Add(CreateDelayedTextField(
                "Title",
                model.Title,
                value => ApplyChange("Edit Node Title", () => model.Title = value)));

            Toggle enabledToggle = new Toggle("Enabled") { value = model.IsEnabled };
            enabledToggle.RegisterValueChangedCallback(evt =>
                ApplyChange("Toggle Node", () => model.IsEnabled = evt.newValue));
            bodyContainer.Add(enabledToggle);

            bodyContainer.Add(CreateDelayedTextField(
                "Note",
                model.Note,
                value => ApplyChange("Edit Node Note", () => model.Note = value),
                true));

            if (model is BehaviorTreeNodeModelBase behaviorTreeNode)
            {
                bodyContainer.Add(CreateDelayedTextField(
                    "Description",
                    behaviorTreeNode.Description,
                    value => ApplyChange("Edit Node Description", () => behaviorTreeNode.Description = value),
                    true));
            }
        }

        private void BuildGraphEdgeInspector(GraphEdgeModelBase model)
        {
            headerLabel.text = "Inspector - Edge";
            AddReadOnlyField("Edge Type", model.GetType().Name);
            AddReadOnlyField("Edge Id", model.EdgeId);
            AddReadOnlyField("From", ResolveNodeLabel(model.OutputNodeId));
            AddReadOnlyField("To", ResolveNodeLabel(model.InputNodeId));

            Toggle enabledToggle = new Toggle("Enabled") { value = model.IsEnabled };
            enabledToggle.RegisterValueChangedCallback(evt =>
                ApplyChange("Toggle Edge", () => model.IsEnabled = evt.newValue));
            bodyContainer.Add(enabledToggle);

            bodyContainer.Add(CreateDelayedIntegerField(
                "Sort Order",
                model.SortOrder,
                value => ApplyChange("Edit Edge Sort Order", () => model.SortOrder = value)));

            if (model is BehaviorTreeChildEdgeModel childEdge &&
                currentGraph is BehaviorTreeGraphAsset behaviorTreeGraph)
            {
                AddReadOnlyField("Child Order", (childEdge.ChildIndex + 1).ToString());

                VisualElement actionRow = new VisualElement();
                actionRow.style.flexDirection = FlexDirection.Row;
                actionRow.style.marginTop = 6f;

                Button moveUpButton = new Button(() =>
                    ApplyChange("Move Child Edge Up", () => behaviorTreeGraph.MoveChildEdge(childEdge.OutputNodeId, childEdge.EdgeId, -1)))
                {
                    text = "Move Up"
                };
                moveUpButton.style.marginRight = 4f;
                moveUpButton.SetEnabled(behaviorTreeGraph.CanMoveChildEdge(childEdge.OutputNodeId, childEdge.EdgeId, -1));
                actionRow.Add(moveUpButton);

                Button moveDownButton = new Button(() =>
                    ApplyChange("Move Child Edge Down", () => behaviorTreeGraph.MoveChildEdge(childEdge.OutputNodeId, childEdge.EdgeId, 1)))
                {
                    text = "Move Down"
                };
                moveDownButton.SetEnabled(behaviorTreeGraph.CanMoveChildEdge(childEdge.OutputNodeId, childEdge.EdgeId, 1));
                actionRow.Add(moveDownButton);

                bodyContainer.Add(actionRow);
            }
        }

        private void BuildCompositeInspector(BehaviorTreeCompositeNodeModel model)
        {
            EnumField modeField = new EnumField("Mode", model.CompositeMode);
            modeField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Composite Mode", () => model.CompositeMode = (BehaviorTreeCompositeMode)evt.newValue));
            bodyContainer.Add(modeField);
        }

        private void BuildBehaviorTreeChildrenInspector(BehaviorTreeNodeModelBase model)
        {
            if (currentGraph is not BehaviorTreeGraphAsset behaviorTreeGraph || model == null)
            {
                return;
            }

            List<BehaviorTreeChildEdgeModel> childEdges = behaviorTreeGraph.GetOrderedChildEdges(model.NodeId).ToList();
            bodyContainer.Add(CreateReorderableListSection(
                "Children",
                childEdges,
                edge => FormatBehaviorTreeChild(edge),
                onSelected: null,
                onReordered: (oldIndex, newIndex) =>
                {
                    if (newIndex < 0 || newIndex >= childEdges.Count)
                    {
                        return;
                    }

                    BehaviorTreeChildEdgeModel movedEdge = childEdges[newIndex];
                    ApplyChange("Reorder Children", () => behaviorTreeGraph.MoveChildEdgeToIndex(model.NodeId, movedEdge.EdgeId, newIndex));
                },
                onItemChosen: edge =>
                {
                    GraphNodeModelBase childNode = currentGraph.FindNode(edge.InputNodeId);
                    requestSelection?.Invoke(childNode != null ? (object)childNode : edge);
                },
                emptyMessage: "No child connections."));
        }

        private void BuildConditionInspector(BehaviorTreeConditionNodeModel model)
        {
            bodyContainer.Add(CreateBlackboardKeyField(
                "Blackboard Key",
                model.BlackboardKey,
                value =>
                {
                    model.BlackboardKey = value;
                    SyncConditionExpectedValueType(model);
                }));

            EnumField comparisonField = new EnumField("Comparison", model.Comparison);
            comparisonField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Comparison", () => model.Comparison = (BehaviorTreeComparisonOperator)evt.newValue));
            bodyContainer.Add(comparisonField);

            SyncConditionExpectedValueType(model);
            if (model.Comparison != BehaviorTreeComparisonOperator.IsSet)
            {
                bodyContainer.Add(CreateConditionExpectedValueField(model));
            }

            EnumField abortField = new EnumField("Abort Mode", model.AbortMode);
            abortField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Abort Mode", () => model.AbortMode = (BehaviorTreeAbortMode)evt.newValue));
            bodyContainer.Add(abortField);
        }

        private void BuildServiceInspector(BehaviorTreeServiceNodeModel model)
        {
            bodyContainer.Add(CreateDelayedTextField(
                "Service Key",
                model.ServiceKey,
                value => ApplyChange("Edit Service Key", () => model.ServiceKey = value)));

            bodyContainer.Add(CreateDelayedFloatField(
                "Interval Seconds",
                model.IntervalSeconds,
                value => ApplyChange("Edit Interval Seconds", () => model.IntervalSeconds = value)));
        }

        private void BuildActionInspector(BehaviorTreeActionNodeModel model)
        {
            bodyContainer.Add(CreateDelayedTextField(
                "Task Key",
                model.TaskKey,
                value => ApplyChange("Edit Task Key", () => model.TaskKey = value)));
        }

        private void BuildPlayActionInspector(BehaviorTreePlayActionNodeModel model)
        {
            UnityEditor.UIElements.ObjectField actionField = new UnityEditor.UIElements.ObjectField("Target Action")
            {
                objectType = typeof(Game.Logic.ActionConfigAsset),
                allowSceneObjects = false,
                value = model.TargetAction
            };
            actionField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Target Action", () => model.TargetAction = (Game.Logic.ActionConfigAsset)evt.newValue));
            bodyContainer.Add(actionField);
        }

        private void BuildBehaviorTreeBlackboardInspector(BehaviorTreeBlackboardEntry model)
        {
            headerLabel.text = "Inspector - Blackboard";
            AddReadOnlyField("Entry Type", nameof(BehaviorTreeBlackboardEntry));

            bodyContainer.Add(CreateDelayedTextField(
                "Key",
                model.Key,
                value => ApplyChange("Edit Blackboard Key", () =>
                {
                    model.Key = value;
                    if (string.IsNullOrEmpty(model.DisplayName))
                    {
                        model.DisplayName = model.Key;
                    }
                })));

            bodyContainer.Add(CreateDelayedTextField(
                "Display Name",
                model.DisplayName,
                value => ApplyChange("Edit Blackboard Display Name", () => model.DisplayName = value)));

            EnumField valueTypeField = new EnumField("Value Type", model.ValueType);
            valueTypeField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Blackboard Value Type", () =>
                {
                    model.ValueType = (BehaviorTreeBlackboardValueType)evt.newValue;
                    model.DefaultValueData ??= BehaviorTreeValueData.CreateDefault(model.ValueType);
                    model.DefaultValueData.SetValueType(model.ValueType, true);
                    model.SerializedTypeName = model.ValueType.ToString();
                    SyncConditionNodesForBlackboardKey(model.Key, model.ValueType);
                }));
            bodyContainer.Add(valueTypeField);

            bodyContainer.Add(CreateBlackboardDefaultValueField(model));
            AddReadOnlyField("Serialized Type", model.SerializedTypeName);

            VisualElement actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 6f;
            bodyContainer.Add(actionRow);
        }

        private VisualElement CreateReorderableListSection<TItem>(
            string title,
            List<TItem> items,
            Func<TItem, string> getLabel,
            Action<TItem> onSelected,
            Action<int, int> onReordered,
            Action<TItem> onItemChosen,
            string emptyMessage)
        {
            VisualElement container = new VisualElement();
            container.style.marginTop = 8f;

            ReorderableList reorderableList = null;

            IMGUIContainer imguiContainer = new IMGUIContainer(() =>
            {
                if (reorderableList == null || reorderableList.list.Count != items.Count)
                {
                    reorderableList = new ReorderableList((IList)items, typeof(TItem), true, true, false, false);
                    reorderableList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, title);
                    reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        EditorGUI.LabelField(rect, getLabel(items[index]));
                    };
                    reorderableList.onReorderCallbackWithDetails = (list, oldIdx, newIdx) =>
                    {
                        onReordered?.Invoke(oldIdx, newIdx);
                    };
                    reorderableList.onSelectCallback = (list) =>
                    {
                        if (list.index >= 0 && list.index < items.Count)
                        {
                            onSelected?.Invoke(items[list.index]);
                        }
                    };
                }
                reorderableList.DoLayoutList();
            });

            container.Add(imguiContainer);

            if (items.Count == 0)
            {
                Label emptyLabel = new Label(emptyMessage);
                emptyLabel.style.marginTop = 4f;
                emptyLabel.style.whiteSpace = WhiteSpace.Normal;
                container.Add(emptyLabel);
            }
            return container;
        }

        private string FormatBehaviorTreeChild(BehaviorTreeChildEdgeModel edge)
        {
            GraphNodeModelBase childNode = currentGraph?.FindNode(edge.InputNodeId);
            if (childNode == null)
            {
                return $"Missing Node ({edge.InputNodeId})";
            }

            string typeName = childNode.GetType().Name
                .Replace("BehaviorTree", string.Empty)
                .Replace("NodeModel", string.Empty);
            return $"{childNode.Title} [{typeName}]";
        }

        private string ResolveNodeLabel(string nodeId)
        {
            GraphNodeModelBase node = currentGraph.FindNode(nodeId);
            if (node == null)
            {
                return string.IsNullOrEmpty(nodeId) ? "<None>" : nodeId;
            }

            return string.IsNullOrEmpty(node.Title) ? node.NodeId : node.Title;
        }

        private void ApplyChange(string actionName, Action change)
        {
            applyChange?.Invoke(actionName, change);
        }

        private VisualElement CreateBlackboardKeyField(string label, string currentValue, Action<string> onChanged)
        {
            List<string> entries = BuildBlackboardKeyChoices(currentValue);
            if (entries.Count <= 1)
            {
                return CreateDelayedTextField(
                    label,
                    currentValue,
                    value => ApplyChange("Edit Blackboard Key", () => onChanged(value)));
            }

            string selectionValue = string.IsNullOrEmpty(currentValue) ? "<None>" : currentValue;
            PopupField<string> popupField = new PopupField<string>(label, entries, selectionValue);
            popupField.RegisterValueChangedCallback(evt =>
                ApplyChange("Edit Blackboard Key", () => onChanged(evt.newValue == "<None>" ? string.Empty : evt.newValue)));
            return popupField;
        }

        private VisualElement CreateBlackboardDefaultValueField(BehaviorTreeBlackboardEntry model)
        {
            model.DefaultValueData ??= BehaviorTreeValueData.CreateDefault(model.ValueType);
            model.DefaultValueData.SetValueType(model.ValueType, false);

            return model.ValueType switch
            {
                BehaviorTreeBlackboardValueType.Bool => CreateToggleField(
                    "Default Value",
                    model.DefaultValueData.BoolValue,
                    value => ApplyChange("Edit Blackboard Default Value", () => model.DefaultValueData.BoolValue = value)),
                BehaviorTreeBlackboardValueType.Int => CreateDelayedIntegerField(
                    "Default Value",
                    model.DefaultValueData.IntValue,
                    value => ApplyChange("Edit Blackboard Default Value", () => model.DefaultValueData.IntValue = value)),
                BehaviorTreeBlackboardValueType.Float => CreateDelayedFloatField(
                    "Default Value",
                    model.DefaultValueData.FloatValue,
                    value => ApplyChange("Edit Blackboard Default Value", () => model.DefaultValueData.FloatValue = value)),
                _ => CreateDelayedTextField(
                    "Default Value",
                    model.DefaultValueData.StringValue,
                    value => ApplyChange("Edit Blackboard Default Value", () => model.DefaultValueData.StringValue = value))
            };
        }

        private VisualElement CreateConditionExpectedValueField(BehaviorTreeConditionNodeModel model)
        {
            model.ExpectedValueData ??= BehaviorTreeValueData.CreateDefault(ResolveConditionExpectedValueType(model));

            BehaviorTreeBlackboardValueType valueType = ResolveConditionExpectedValueType(model);
            model.ExpectedValueData.SetValueType(valueType, false);

            return valueType switch
            {
                BehaviorTreeBlackboardValueType.Bool => CreateToggleField(
                    "Expected Value",
                    model.ExpectedValueData.BoolValue,
                    value => ApplyChange("Edit Expected Value", () => model.ExpectedValueData.BoolValue = value)),
                BehaviorTreeBlackboardValueType.Int => CreateDelayedIntegerField(
                    "Expected Value",
                    model.ExpectedValueData.IntValue,
                    value => ApplyChange("Edit Expected Value", () => model.ExpectedValueData.IntValue = value)),
                BehaviorTreeBlackboardValueType.Float => CreateDelayedFloatField(
                    "Expected Value",
                    model.ExpectedValueData.FloatValue,
                    value => ApplyChange("Edit Expected Value", () => model.ExpectedValueData.FloatValue = value)),
                _ => CreateDelayedTextField(
                    "Expected Value",
                    model.ExpectedValueData.StringValue,
                    value => ApplyChange("Edit Expected Value", () => model.ExpectedValueData.StringValue = value))
            };
        }

        private List<string> BuildBlackboardKeyChoices(string currentValue)
        {
            List<string> keys = new List<string> { "<None>" };

            if (currentGraph is BehaviorTreeGraphAsset behaviorTreeGraph)
            {
                keys.AddRange(behaviorTreeGraph.BlackboardEntries
                    .Select(entry => entry?.Key)
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Distinct());
            }

            if (!string.IsNullOrWhiteSpace(currentValue) && !keys.Contains(currentValue))
            {
                keys.Add(currentValue);
            }

            return keys;
        }

        private void SyncConditionExpectedValueType(BehaviorTreeConditionNodeModel model)
        {
            if (model == null)
            {
                return;
            }

            BehaviorTreeBlackboardValueType valueType = ResolveConditionExpectedValueType(model);
            model.ExpectedValueData ??= BehaviorTreeValueData.CreateDefault(valueType);
            if (model.ExpectedValueData.ValueType != valueType)
            {
                model.ExpectedValueData.SetValueType(valueType, true);
            }
        }

        private BehaviorTreeBlackboardValueType ResolveConditionExpectedValueType(BehaviorTreeConditionNodeModel model)
        {
            if (model == null)
            {
                return BehaviorTreeBlackboardValueType.String;
            }

            if (currentGraph is BehaviorTreeGraphAsset behaviorTreeGraph)
            {
                BehaviorTreeBlackboardEntry entry = behaviorTreeGraph.BlackboardEntries
                    .FirstOrDefault(item => item != null && item.Key == model.BlackboardKey);
                if (entry != null)
                {
                    return entry.ValueType;
                }
            }

            return model.ExpectedValueData?.ValueType ?? BehaviorTreeBlackboardValueType.String;
        }

        private void SyncConditionNodesForBlackboardKey(string blackboardKey, BehaviorTreeBlackboardValueType valueType)
        {
            if (currentGraph is not BehaviorTreeGraphAsset behaviorTreeGraph || string.IsNullOrWhiteSpace(blackboardKey))
            {
                return;
            }

            foreach (BehaviorTreeConditionNodeModel node in behaviorTreeGraph.BehaviorNodes.OfType<BehaviorTreeConditionNodeModel>())
            {
                if (node.BlackboardKey != blackboardKey)
                {
                    continue;
                }

                node.ExpectedValueData ??= BehaviorTreeValueData.CreateDefault(valueType);
                node.ExpectedValueData.SetValueType(valueType, true);
            }
        }

        private TextField CreateDelayedTextField(string label, string value, Action<string> onValueChanged, bool multiline = false)
        {
            TextField field = new TextField(label)
            {
                value = value ?? string.Empty,
                multiline = multiline,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue ?? string.Empty));
            return field;
        }

        private IntegerField CreateDelayedIntegerField(string label, int value, Action<int> onValueChanged)
        {
            IntegerField field = new IntegerField(label)
            {
                value = value,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }

        private Toggle CreateToggleField(string label, bool value, Action<bool> onValueChanged)
        {
            Toggle field = new Toggle(label) { value = value };
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }

        private FloatField CreateDelayedFloatField(string label, float value, Action<float> onValueChanged)
        {
            FloatField field = new FloatField(label)
            {
                value = value,
                isDelayed = true
            };
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }

        private bool CanMoveBlackboardEntry(BehaviorTreeBlackboardEntry entry, int direction)
        {
            int index = currentGraph?.Blackboard.IndexOf(entry) ?? -1;
            if (index < 0)
            {
                return false;
            }

            int targetIndex = index + direction;
            return targetIndex >= 0 && targetIndex < currentGraph.Blackboard.Count;
        }

        private void MoveBlackboardEntry(BehaviorTreeBlackboardEntry entry, int direction)
        {
            if (currentGraph == null)
            {
                return;
            }

            int index = currentGraph.Blackboard.IndexOf(entry);
            int targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= currentGraph.Blackboard.Count)
            {
                return;
            }

            currentGraph.Blackboard.RemoveAt(index);
            currentGraph.Blackboard.Insert(targetIndex, entry);
        }

        private void AddReadOnlyField(string label, string value)
        {
            TextField field = new TextField(label) { value = value ?? string.Empty };
            field.SetEnabled(false);
            bodyContainer.Add(field);
        }

        private void AddMessage(string message)
        {
            headerLabel.text = "Inspector";
            Label label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            bodyContainer.Add(label);
        }

        private void AddMessageBlock(string title, string message)
        {
            Label header = new Label(title);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 8f;
            header.style.marginBottom = 4f;
            bodyContainer.Add(header);

            Label label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            bodyContainer.Add(label);
        }
    }
}
