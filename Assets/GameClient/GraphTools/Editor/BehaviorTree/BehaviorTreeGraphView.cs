using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Game.AI;

namespace Game.GraphTools.Editor
{
    public sealed class BehaviorTreeGraphView : BaseGraphView, IEdgeConnectorListener
    {
        private readonly Dictionary<string, BehaviorTreeNodeView> nodeViewsById = new Dictionary<string, BehaviorTreeNodeView>();
        private BehaviorTreeNodeSearchProvider nodeSearchProvider;
        private Label edgeInfoOverlay;

        public Action<BehaviorTreeBlackboardValueType> BlackboardEntryCreateRequested { get; set; }
        public bool CanCreateRootFromSearch => graphAsset is BehaviorTreeGraphAsset behaviorTreeGraph && behaviorTreeGraph.RootNode == null;
        public bool CanCreateChildNodeFromSearch => graphAsset is BehaviorTreeGraphAsset behaviorTreeGraph && behaviorTreeGraph.RootNode != null;

        public BehaviorTreeGraphView()
        {
            CreateEdgeInfoOverlay();
        }

        private void CreateEdgeInfoOverlay()
        {
            edgeInfoOverlay = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 10,
                    top = 10,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f),
                    color = new Color(0.9f, 0.9f, 0.9f),
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = new Color(0.3f, 0.3f, 0.3f),
                    borderRightColor = new Color(0.3f, 0.3f, 0.3f),
                    borderTopColor = new Color(0.3f, 0.3f, 0.3f),
                    borderBottomColor = new Color(0.3f, 0.3f, 0.3f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    display = DisplayStyle.None
                }
            };
            Add(edgeInfoOverlay);
        }

        public void Initialize(EditorWindow ownerWindow)
        {
            nodeSearchProvider ??= ScriptableObject.CreateInstance<BehaviorTreeNodeSearchProvider>();
            nodeSearchProvider.hideFlags = HideFlags.HideAndDontSave;
            nodeSearchProvider.Initialize(this, ownerWindow);
        }

        public override void BindGraph(GraphAssetBase asset)
        {
            base.BindGraph(asset);
            nodeViewsById.Clear();
            nodeCreationRequest = null;

            if (asset is not BehaviorTreeGraphAsset graphAsset)
            {
                return;
            }

            nodeCreationRequest = HandleNodeCreationRequest;

            foreach (BehaviorTreeNodeModelBase node in graphAsset.BehaviorNodes.Where(node => node != null))
            {
                BehaviorTreeNodeView nodeView = new BehaviorTreeNodeView(graphAsset, node, ApplyNodeChange, this);
                nodeViewsById[node.NodeId] = nodeView;
                AddElement(nodeView);
            }

            foreach (BehaviorTreeChildEdgeModel edgeModel in graphAsset.ChildEdges.Where(edge => edge != null))
            {
                Edge edgeView = CreateEdgeView(edgeModel);
                if (edgeView != null)
                {
                    AddElement(edgeView);
                }
            }

            RefreshPresentation();
        }

        public override void RefreshPresentation()
        {
            foreach (BehaviorTreeNodeView nodeView in nodeViewsById.Values)
            {
                nodeView.RefreshFromModel();
            }

            foreach (Edge edgeView in edges.ToList())
            {
                if (edgeView.userData is BehaviorTreeChildEdgeModel edgeModel)
                {
                    // Update any cached data here if needed, tooltips are removed
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return base.GetCompatiblePorts(startPort, nodeAdapter)
                .Where(port => startPort.node is BehaviorTreeNodeView && port.node is BehaviorTreeNodeView)
                .ToList();
        }


        protected override bool TryCreateEdgeModel(Edge edgeView, out GraphEdgeModelBase edgeModel)
        {
            edgeModel = null;

            if (graphAsset is not BehaviorTreeGraphAsset behaviorTreeGraph)
            {
                return false;
            }

            if (edgeView.output?.node is not BehaviorTreeNodeView parentNodeView ||
                edgeView.input?.node is not BehaviorTreeNodeView childNodeView ||
                parentNodeView.Model is not BehaviorTreeNodeModelBase parentNode ||
                childNodeView.Model is not BehaviorTreeNodeModelBase childNode)
            {
                return false;
            }

            string outputPortId = GetPortId(edgeView.output);
            string inputPortId = GetPortId(edgeView.input);

            // Deduplication check: Check if an edge already exists in the asset
            BehaviorTreeChildEdgeModel existingEdge = behaviorTreeGraph.ChildEdges.FirstOrDefault(e =>
                e.OutputNodeId == parentNode.NodeId &&
                e.OutputPortId == outputPortId &&
                e.InputNodeId == childNode.NodeId &&
                e.InputPortId == inputPortId);

            if (existingEdge != null)
            {
                edgeView.userData = existingEdge;
                RegisterEdgeHoverEvents(edgeView);
                edgeModel = existingEdge;
                return false; // Return false because it already exists in the model
            }

            BehaviorTreeChildEdgeModel childEdge = new BehaviorTreeChildEdgeModel
            {
                OutputNodeId = parentNode.NodeId,
                OutputPortId = outputPortId,
                InputNodeId = childNode.NodeId,
                InputPortId = inputPortId,
                ChildIndex = behaviorTreeGraph.GetOrderedChildEdges(parentNode.NodeId)
                    .Select(e => e.ChildIndex)
                    .DefaultIfEmpty(-1)
                    .Max() + 1
            };
            childEdge.SortOrder = childEdge.ChildIndex;

            edgeView.userData = childEdge;
            RegisterEdgeHoverEvents(edgeView);
            edgeModel = childEdge;
            return true;
        }

        private void RegisterEdgeHoverEvents(Edge edgeView)
        {
            edgeView.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (edgeView.userData is BehaviorTreeChildEdgeModel model)
                {
                    edgeInfoOverlay.text = $"Child Order: {model.ChildIndex + 1}";
                    edgeInfoOverlay.style.display = DisplayStyle.Flex;
                }
            });

            edgeView.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                edgeInfoOverlay.style.display = DisplayStyle.None;
            });
        }

        private void ApplyNodeChange(string actionName, Action applyAction)
        {
            ApplyGraphChange(actionName, applyAction);
        }

        public Vector2 GetGraphPositionFromScreen(Vector2 screenPosition, EditorWindow ownerWindow)
        {
            Vector2 windowMousePosition = ownerWindow.rootVisualElement.ChangeCoordinatesTo(
                ownerWindow.rootVisualElement.parent,
                screenPosition - ownerWindow.position.position);
            return contentViewContainer.WorldToLocal(windowMousePosition);
        }

        public bool CreateNodeFromSearch(
            BehaviorTreeNodeSearchItem item,
            Vector2 position,
            NodeCreationContext creationContext)
        {
            BehaviorTreeNodeModelBase node = CreateNodeFromSearchItem(item);
            if (node == null)
            {
                return false;
            }

            Port sourcePort = ResolveCreationSourcePort(creationContext);
            CreateNodeAtPosition(node, position, sourcePort);
            return true;
        }

        public bool CreateNodeFromSearch(
            BehaviorTreeNodeSearchItem item,
            Vector2 position,
            Port sourcePort)
        {
            BehaviorTreeNodeModelBase node = CreateNodeFromSearchItem(item);
            if (node == null)
            {
                return false;
            }

            CreateNodeAtPosition(node, position, sourcePort);
            return true;
        }

        private void CreateNodeAtPosition(BehaviorTreeNodeModelBase node, Vector2 position)
        {
            CreateNodeAtPosition(node, position, null);
        }

        private void CreateNodeAtPosition(BehaviorTreeNodeModelBase node, Vector2 position, Port sourcePort)
        {
            if (graphAsset is not BehaviorTreeGraphAsset behaviorTreeGraph || node == null)
            {
                return;
            }

            node.Position = position;
            ApplyGraphChange($"Create {node.GetType().Name}", () =>
            {
                behaviorTreeGraph.Nodes.Add(node);

                if (sourcePort?.direction == Direction.Output &&
                    sourcePort.node is BehaviorTreeNodeView parentNodeView &&
                    parentNodeView.Model is BehaviorTreeNodeModelBase parentNode)
                {
                    int childOrder = behaviorTreeGraph.GetOrderedChildEdges(parentNode.NodeId).Count();
                    behaviorTreeGraph.Edges.Add(new BehaviorTreeChildEdgeModel
                    {
                        OutputNodeId = parentNode.NodeId,
                        OutputPortId = GetPortId(sourcePort),
                        InputNodeId = node.NodeId,
                        InputPortId = BaseNodeView.DefaultInputPortId,
                        ChildIndex = childOrder,
                        SortOrder = childOrder
                    });
                }
            });
            BindGraph(behaviorTreeGraph);

            if (nodeViewsById.TryGetValue(node.NodeId, out BehaviorTreeNodeView nodeView))
            {
                ClearSelection();
                AddToSelection(nodeView);
                NotifySelectionChanged();
            }
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Port sourcePort = edge.output ?? edge.input;
            if (sourcePort == null)
            {
                return;
            }

            nodeSearchProvider.SetPendingPort(sourcePort);
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(position)), nodeSearchProvider);
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            if (edge.input == null || edge.output == null)
            {
                return;
            }

            // Trigger the internal creation logic for the model
            List<Edge> edgesToCreate = new List<Edge> { edge };
            GraphViewChange change = new GraphViewChange { edgesToCreate = edgesToCreate };
            
            // Re-capture the change result to see if any edges were filtered out (e.g., duplicates)
            GraphViewChange result = graphViewChanged?.Invoke(change) ?? change;

            // Ensure the edge is added to the UI if it was a manual drop and WASN'T filtered out
            if (result.edgesToCreate != null && result.edgesToCreate.Contains(edge))
            {
                if (edge.parent == null)
                {
                    AddElement(edge);
                }
            }
        }

        private Edge CreateEdgeView(BehaviorTreeChildEdgeModel edgeModel)
        {
            if (!nodeViewsById.TryGetValue(edgeModel.OutputNodeId, out BehaviorTreeNodeView outputNodeView) ||
                !nodeViewsById.TryGetValue(edgeModel.InputNodeId, out BehaviorTreeNodeView inputNodeView))
            {
                return null;
            }

            if (outputNodeView.OutputPort == null || inputNodeView.InputPort == null)
            {
                return null;
            }

            Edge edgeView = new Edge
            {
                output = outputNodeView.OutputPort,
                input = inputNodeView.InputPort,
                userData = edgeModel
            };

            RegisterEdgeHoverEvents(edgeView);

            edgeView.output.Connect(edgeView);
            edgeView.input.Connect(edgeView);
            return edgeView;
        }

        private static string GetPortId(Port port)
        {
            return port?.userData as string ?? string.Empty;
        }

        private void HandleNodeCreationRequest(NodeCreationContext context)
        {
            if (nodeSearchProvider == null)
            {
                return;
            }

            nodeSearchProvider.SetPendingContext(context);
            nodeSearchProvider.SetPendingPort(ResolveCreationSourcePort(context));
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), nodeSearchProvider);
        }

        private static Port ResolveCreationSourcePort(NodeCreationContext context)
        {
            if (context.target is Port port)
            {
                return port;
            }

            if (context.target is Edge edge)
            {
                return edge.output ?? edge.input;
            }

            return null;
        }

        private static BehaviorTreeNodeModelBase CreateNodeFromSearchItem(BehaviorTreeNodeSearchItem item)
        {
            return item switch
            {
                BehaviorTreeNodeSearchItem.Root => new BehaviorTreeRootNodeModel(),
                BehaviorTreeNodeSearchItem.Sequence => new BehaviorTreeCompositeNodeModel
                {
                    Title = "Sequence",
                    CompositeMode = BehaviorTreeCompositeMode.Sequence
                },
                BehaviorTreeNodeSearchItem.Selector => new BehaviorTreeCompositeNodeModel
                {
                    Title = "Selector",
                    CompositeMode = BehaviorTreeCompositeMode.Selector
                },
                BehaviorTreeNodeSearchItem.Parallel => new BehaviorTreeCompositeNodeModel
                {
                    Title = "Parallel",
                    CompositeMode = BehaviorTreeCompositeMode.Parallel
                },
                BehaviorTreeNodeSearchItem.Condition => new BehaviorTreeConditionNodeModel(),
                BehaviorTreeNodeSearchItem.Service => new BehaviorTreeServiceNodeModel(),
                BehaviorTreeNodeSearchItem.Action => new BehaviorTreeActionNodeModel(),
                BehaviorTreeNodeSearchItem.PlayAction => new BehaviorTreePlayActionNodeModel(),
                _ => null
            };
        }
    }
}
