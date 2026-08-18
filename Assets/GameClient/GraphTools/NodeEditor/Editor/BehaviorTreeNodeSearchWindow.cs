using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Logic.AI.BehaviorTree;

public class BehaviorTreeNodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private TreeGraphViewr _graphView;
    private EditorWindow _window;
    private Texture2D _indentationIcon;

    public void Init(TreeGraphViewr graphView, EditorWindow window)
    {
        _graphView = graphView;
        _window = window;

        // Create a transparent icon to align items without icons properly
        _indentationIcon = new Texture2D(1, 1);
        _indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        _indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
        };

        var types = TypeCache.GetTypesDerivedFrom<NodeData>();
        
        var rootGroup = new List<Type>();
        var composites = new List<Type>();
        var decorators = new List<Type>();
        var conditions = new List<Type>();
        var tasks = new List<Type>();
        var others = new List<Type>();

        foreach (var type in types)
        {
            if (type.IsAbstract) continue;
            
            if (type == typeof(RootData))
            {
                if (_graphView.tree != null && _graphView.tree.rootNode != null)
                    continue; // 已经有根节点则不显示
                else
                    rootGroup.Add(type);
                continue; // RootData 专门处理，直接进入下一个循环
            }

            // 分流：通过名字或特征判断是否属于 Condition
            if (type.Name.Contains("Condition") || type.Name.StartsWith("BBCheck"))
            {
                conditions.Add(type);
            }
            else if (typeof(CompositeData).IsAssignableFrom(type))
            {
                composites.Add(type);
            }
            else if (typeof(DecoratorData).IsAssignableFrom(type))
            {
                decorators.Add(type);
            }
            else if (typeof(TaskData).IsAssignableFrom(type))
            {
                tasks.Add(type);
            }
            else
            {
                others.Add(type);
            }
        }

        // Helper to add a group and its items
        void AddGroup(string title, List<Type> groupTypes)
        {
            if (groupTypes.Count == 0) return;
            tree.Add(new SearchTreeGroupEntry(new GUIContent(title), 1));
            
            // 组内按名称排序，让列表更整洁
            groupTypes.Sort((a, b) => a.Name.CompareTo(b.Name));
            
            foreach (var type in groupTypes)
            {
                tree.Add(new SearchTreeEntry(new GUIContent(type.Name, _indentationIcon))
                {
                    userData = type,
                    level = 2
                });
            }
        }

        AddGroup("Root", rootGroup);
        AddGroup("Composites", composites);
        AddGroup("Conditions", conditions);
        AddGroup("Decorators", decorators);
        AddGroup("Tasks", tasks);
        AddGroup("Others", others);

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        var type = SearchTreeEntry.userData as Type;
        
        // Calculate the local mouse position within the GraphView
        var windowMousePosition = _graphView.ChangeCoordinatesTo(_graphView, context.screenMousePosition - _window.position.position);
        var localMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);

        _graphView.CreateNode(type, localMousePosition);
        return true;
    }
}