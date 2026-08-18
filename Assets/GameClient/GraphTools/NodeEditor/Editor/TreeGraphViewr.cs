using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using Game.Logic.AI.BehaviorTree;
using System.Collections.Generic;
using System.Linq;
public class TreeGraphViewr : GraphView
{
    public new class UxmlFactory : UxmlFactory<TreeGraphViewr, UxmlTraits> { }
    public BehaviorTreeAsset tree;
    public System.Action<TreeNodeView> onSelectNodeView;
    public TreeGraphViewr()
    {
        style.flexGrow = 1;
        style.flexShrink = 1;
        style.flexDirection = FlexDirection.Row;
        style.overflow = Overflow.Hidden;

        Insert(0, new GridBackground());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/GameClient/GraphTools/NodeEditor/Editor/TreeGraphViewr.uss");
        this.styleSheets.Add(styleSheet);
    }

    public void CreateNode(System.Type type, Vector2 position)
    {
        var node = tree.CreateNode(type);
        node.position = position; // 设置为鼠标点击的位置
        EditorUtility.SetDirty(node); // 标记需要保存
        CreateNodeView(node);
    }

    private void CreateNodeView(Game.Logic.AI.BehaviorTree.NodeData node)
    {
        var nodeView = new TreeNodeView(node);
        nodeView.onNodeSelected = onSelectNodeView;
        AddElement(nodeView);
    }
    internal void PopulateView(BehaviorTreeAsset tree)
    {
        this.tree = tree;
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements.ToList());
        graphViewChanged += OnGraphViewChanged;
        
        if (tree == null) return;
        
        tree.nodes.ForEach(n => CreateNodeView(n));
        tree.nodes.ForEach(n =>
        {
            var children = n.GetChildren();
            children.ForEach(c =>
            {
                var parentView = FindNodeViewByGuid(n);
                var childView = FindNodeViewByGuid(c);
                if (parentView.output != null && childView.input != null)
                {
                    var edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge);
                }
            });
        });
    }
    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (graphViewChange.elementsToRemove != null)
        {
            // 第一步：先处理所有的连线删除。如果不先删连线，底层节点被销毁后再碰连线就会报空引用异常！
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    var outputNode = edge.output.node as TreeNodeView;
                    var inputNode = edge.input.node as TreeNodeView;
                    outputNode.node.RemoveChild(inputNode.node);
                }
            }

            // 第二步：再处理所有的节点销毁。
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is TreeNodeView nodeView)
                {
                    tree.DeleteNode(nodeView.node);
                }
            }
        }
        if(graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var outputNode = edge.output.node as TreeNodeView;
                var inputNode = edge.input.node as TreeNodeView;
                outputNode.node.AddChild(inputNode.node);
            }
        }
        return graphViewChange;
    }
    private TreeNodeView FindNodeViewByGuid(Game.Logic.AI.BehaviorTree.NodeData node)
    {
        return GetNodeByGuid(node.guid) as TreeNodeView;
    }
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach((endport) =>
        {
            if (startPort != endport && startPort.direction != endport.direction
            &&startPort.node != endport.node)
            {
                compatiblePorts.Add(endport);
            }
        });
        return compatiblePorts;
    }

    public void UpdateNodeStates(Game.Logic.AI.BehaviorTree.BTRunner runner)
    {
        nodes.ForEach(n => {
            if (n is TreeNodeView view)
            {
                view.UpdateState(runner);
            }
        });

        edges.ForEach(e => {
            bool isEdgeActive = false;
            if (UnityEngine.Application.isPlaying && runner != null && runner.RuntimeTranslationResult != null && 
                e.input != null && e.input.node is TreeNodeView inputView && inputView.node != null)
            {
                if (runner.RuntimeTranslationResult.GuidToNodeMap.TryGetValue(inputView.node.guid, out var npNode))
                {
                    isEdgeActive = npNode.CurrentState == NPBehave.Node.State.ACTIVE;
                }
            }

            if (isEdgeActive)
            {
                e.AddToClassList("running-edge");
            }
            else
            {
                e.RemoveFromClassList("running-edge");
            }
        });
}
}
