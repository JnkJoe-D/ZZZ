using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Game.AI;

namespace Game.GraphTools.Editor
{
    public sealed class BehaviorTreeNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private BehaviorTreeGraphView graphView;
        private EditorWindow ownerWindow;
        private NodeCreationContext pendingContext;
        private Port pendingPort;

        public void Initialize(BehaviorTreeGraphView view, EditorWindow window)
        {
            graphView = view;
            ownerWindow = window;
        }

        public void SetPendingContext(NodeCreationContext context)
        {
            pendingContext = context;
        }

        public void SetPendingPort(Port port)
        {
            pendingPort = port;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Add Behavior Tree Node"), 0)
            };

            bool isDraggingFromPort = pendingPort != null;
            bool canCreateRoot = !isDraggingFromPort && graphView != null && graphView.CanCreateRootFromSearch;
            bool canCreateChildren = isDraggingFromPort || (graphView != null && graphView.CanCreateChildNodeFromSearch);

            if (canCreateRoot)
            {
                entries.Add(new SearchTreeEntry(new GUIContent("Root"))
                {
                    level = 1,
                    userData = BehaviorTreeNodeSearchItem.Root
                });
            }

            if (canCreateChildren)
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent("Composite"), 1));
                entries.Add(CreateEntry("Sequence", BehaviorTreeNodeSearchItem.Sequence, 2));
                entries.Add(CreateEntry("Selector", BehaviorTreeNodeSearchItem.Selector, 2));
                entries.Add(CreateEntry("Parallel", BehaviorTreeNodeSearchItem.Parallel, 2));

                entries.Add(new SearchTreeGroupEntry(new GUIContent("Decorator"), 1));
                entries.Add(CreateEntry("Condition", BehaviorTreeNodeSearchItem.Condition, 2));
                entries.Add(CreateEntry("Service", BehaviorTreeNodeSearchItem.Service, 2));

                entries.Add(new SearchTreeGroupEntry(new GUIContent("Leaf"), 1));
                entries.Add(CreateEntry("Action", BehaviorTreeNodeSearchItem.Action, 2));
                entries.Add(CreateEntry("Play Action", BehaviorTreeNodeSearchItem.PlayAction, 2));
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (graphView == null || ownerWindow == null || searchTreeEntry.userData is not BehaviorTreeNodeSearchItem item)
            {
                return false;
            }

            Vector2 graphPosition = graphView.GetGraphPositionFromScreen(context.screenMousePosition, ownerWindow);
            return pendingPort != null
                ? graphView.CreateNodeFromSearch(item, graphPosition, pendingPort)
                : graphView.CreateNodeFromSearch(item, graphPosition, pendingContext);
        }

        private static SearchTreeEntry CreateEntry(string label, BehaviorTreeNodeSearchItem item, int level)
        {
            return new SearchTreeEntry(new GUIContent(label))
            {
                level = level,
                userData = item
            };
        }
    }

    public enum BehaviorTreeNodeSearchItem
    {
        Root,
        Sequence,
        Selector,
        Parallel,
        Condition,
        Service,
        Action,
        PlayAction
    }
}
