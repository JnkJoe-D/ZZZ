using UnityEditor.Experimental.GraphView;
using Game.Logic.AI.BehaviorTree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TreeNodeEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    public TreeGraphViewr graphView;
    public Game.Logic.AI.BehaviorTree.BTRunner currentRunner;
    public InspectorViewr inspectorView;

    [MenuItem("BehaviourTree/TreeNodeEditor")]
    public static void ShowExample()
    {
        TreeNodeEditor wnd = GetWindow<TreeNodeEditor>(typeof(UnityEditor.SceneView));
        wnd.titleContent = new GUIContent("TreeNodeEditor");

        // 如果通过菜单打开时，当前正好选中了一棵树，则顺便加载它
        if (Selection.activeObject is BehaviorTreeAsset tree)
        {
            wnd.LoadTree(tree);
        }
    }

    [UnityEditor.Callbacks.OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceId);
        if (obj is BehaviorTreeAsset tree)
        {
            // 获取或打开窗口
            TreeNodeEditor wnd = GetWindow<TreeNodeEditor>(typeof(UnityEditor.SceneView));
            wnd.titleContent = new GUIContent("TreeNodeEditor");
            wnd.Focus(); // 确保窗口获得焦点并置前
            wnd.LoadTree(tree);
            return true; // 告诉 Unity 我们已经处理了这个资源的双击事件
        }
        return false;
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        m_VisualTreeAsset.CloneTree(root);
        graphView = root.Q<TreeGraphViewr>();
        inspectorView = root.Q<InspectorViewr>();
        graphView.onSelectNodeView = OnSelectNodeView;

        // 动态添加一个右侧靠上的 IMGUI 容器用于展示黑板
        var blackboardContainer = new IMGUIContainer(DrawBlackboardPanel);
        blackboardContainer.style.position = Position.Absolute;
        blackboardContainer.style.top = 5;
        blackboardContainer.style.right = 5;
        blackboardContainer.style.width = 250;
        // 半透明背景，让底层网格仍然可见
        blackboardContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 0.8f));
        blackboardContainer.style.borderTopLeftRadius = 5;
        blackboardContainer.style.borderTopRightRadius = 5;
        blackboardContainer.style.borderBottomLeftRadius = 5;
        blackboardContainer.style.borderBottomRightRadius = 5;
        blackboardContainer.style.paddingTop = 5;
        blackboardContainer.style.paddingBottom = 5;
        blackboardContainer.style.paddingLeft = 5;
        blackboardContainer.style.paddingRight = 5;
        
        root.Add(blackboardContainer);

        var validateBtn = root.Q<Button>("Validate");
        if (validateBtn != null)
        {
            validateBtn.clicked += ValidateTreeStructure;
        }

        var saveAsBtn = root.Q<Button>("SaveAs");
        if (saveAsBtn != null)
        {
            saveAsBtn.clicked += SaveAsTree;
        }

        // Setup Search Window Provider for node creation
        var searchWindow = ScriptableObject.CreateInstance<BehaviorTreeNodeSearchWindow>();
        searchWindow.Init(graphView, this);
        graphView.nodeCreationRequest = context => 
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        };

        // Initialize default state on open
        OnSelectionChange();
        if (graphView.tree == null)
        {
            LoadTree(null);
        }
    }

    private void ValidateTreeStructure()
    {
        if (graphView == null || graphView.tree == null)
        {
            EditorUtility.DisplayDialog("预转译校验", "当前没有加载任何行为树资产。", "确定");
            return;
        }

        var tree = graphView.tree;
        if (tree.rootNode == null)
        {
            EditorUtility.DisplayDialog("校验失败", "缺少 RootNode 根节点！NPBehave 强制要求必须有唯一一个 Root 节点作为树的入口。", "确定");
            return;
        }

        
        System.Collections.Generic.HashSet<NodeData> reachableNodes = new System.Collections.Generic.HashSet<NodeData>();
        System.Action<NodeData> traverse = null;
        traverse = (n) => {
            if (n == null || reachableNodes.Contains(n)) return;
            reachableNodes.Add(n);
            foreach (var child in n.GetChildren()) traverse(child);
        };
        traverse(tree.rootNode);

        bool hasError = false;
        bool hasWarning = false;
        System.Text.StringBuilder errors = new System.Text.StringBuilder();

        foreach (var node in tree.nodes)
        {
            if (!reachableNodes.Contains(node))
            {
                errors.AppendLine($"- [警告] 节点 '{node.name}' 无法从 Root 节点到达，转译时将被忽略。");
                hasWarning = true;
            }

            var children = node.GetChildren();
            
            if (node is RootData)
            {
                if (children.Count != 1)
                {
                    errors.AppendLine($"- RootNode ({node.name}) 必须有且仅有 1 个子节点（当前有 {children.Count} 个）。");
                    hasError = true;
                }
            }
            else if (node is DecoratorData)
            {
                if (children.Count != 1)
                {
                    errors.AppendLine($"- Decorator ({node.name}) 作为装饰器节点，必须有且仅有 1 个子节点（当前有 {children.Count} 个）。");
                    hasError = true;
                }
            }
            else if (node is CompositeData)
            {
                if (children.Count == 0)
                {
                    errors.AppendLine($"- Composite ({node.name}) 作为复合节点，至少需要 1 个子节点。");
                    hasError = true;
                }
            }
            else if (node is TaskData)
            {
                if (children.Count > 0)
                {
                    errors.AppendLine($"- Task/Action ({node.name}) 作为叶子节点，不允许有任何子节点（当前有 {children.Count} 个）。");
                    hasError = true;
                }
            }
        }

        if (hasError)
        {
            EditorUtility.DisplayDialog("校验失败", "当前的树结构不符合 NPBehave 规范，发现了以下问题：\n\n" + errors.ToString(), "好的");
        }
        else if (hasWarning)
        {
            EditorUtility.DisplayDialog("校验通过 (有警告)", "树结构符合规范，但存在孤立节点：\n\n" + errors.ToString(), "好的");
        }
        else
        {
            EditorUtility.DisplayDialog("校验通过", "树结构完美符合 NPBehave 规范，可以安全转译！", "好的");
        }
    }

    
    private BTRunner GetSelectedRunner()
    {
        if (Selection.activeGameObject != null)
        {
            return Selection.activeGameObject.GetComponent<BTRunner>();
        }
        return null;
    }

    private void DrawBlackboardPanel()
    {
        if (!Application.isPlaying || graphView == null || graphView.tree == null)
            return;

        var runner = GetSelectedRunner();
        if (runner == null || runner.RuntimeBlackboard == null)
            return;

        GUILayout.Label("Runtime Blackboard", EditorStyles.boldLabel);
        
        // Use reflection or standard dictionary to display blackboard
        // NPBehave Blackboard data is internal dictionary, so we might need reflection if not exposed
        var bbField = typeof(NPBehave.Blackboard).GetField("keys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bbField != null)
        {
            var keys = bbField.GetValue(runner.RuntimeBlackboard) as System.Collections.Generic.Dictionary<string, int>;
            if (keys != null)
            {
                foreach (var k in keys)
                {
                    var val = runner.RuntimeBlackboard.Get(k.Key);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(k.Key, GUILayout.Width(120));
                    GUILayout.Label(val != null ? val.ToString() : "null");
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    public void LoadTree(BehaviorTreeAsset tree)
    {
        if (graphView == null) return;
        
        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            tree.name = "New Behavior Tree (Temp)";
        }

        graphView.PopulateView(tree);

        // 监听图的变化以设置脏标记 (仅对内存临时资产)
        graphView.graphViewChanged -= OnGraphViewChanged;
        graphView.graphViewChanged += OnGraphViewChanged;
        UpdateUnsavedChangesState();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        UpdateUnsavedChangesState();
        return graphViewChange;
    }

    private void UpdateUnsavedChangesState()
    {
        if (graphView != null && graphView.tree != null)
        {
            bool isSavedOnDisk = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graphView.tree));
            // 如果是临时资产，一旦有改动或者刚刚创建，就标记为需要保存
            hasUnsavedChanges = !isSavedOnDisk;
            saveChangesMessage = "当前行为树尚未保存，关闭将丢失所有节点数据。是否保存到磁盘？";
        }
    }

    public override void SaveChanges()
    {
        if (hasUnsavedChanges)
        {
            SaveAsTree();
        }
        base.SaveChanges();
    }

    private void SaveAsTree()
    {
        if (graphView == null || graphView.tree == null) return;

        var tree = graphView.tree;
        bool isSavedOnDisk = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tree));

        string defaultName = isSavedOnDisk ? tree.name : "NewBehaviorTree";
        string path = EditorUtility.SaveFilePanelInProject("Save Behavior Tree", defaultName, "asset", "Please enter a file name to save the behavior tree to");

        if (string.IsNullOrEmpty(path))
            return;

        if (isSavedOnDisk)
        {
            // 已存在资产的另存为逻辑
            if (AssetDatabase.GetAssetPath(tree) == path) return; // 同名不处理
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(tree), path);
            AssetDatabase.SaveAssets();
            var newTree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
            LoadTree(newTree); // 焦点切到新树
            Selection.activeObject = newTree;
        }
        else
        {
            // 临时资产落盘逻辑
            AssetDatabase.CreateAsset(tree, path);
            foreach (var node in tree.nodes)
            {
                AssetDatabase.AddObjectToAsset(node, tree);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 刷新脏标记
            hasUnsavedChanges = false;
        }
    }

    private void OnSelectNodeView(TreeNodeView nodeView)
    {
        inspectorView.UpdateSelection(nodeView);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        OnSelectionChange();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.EnteredPlayMode)
        {
            OnSelectionChange();
        }
        else if (obj == PlayModeStateChange.ExitingPlayMode)
        {
            // 清理运行时的残留树引用，防止报错
            if (graphView != null) graphView.PopulateView(null);
            if (inspectorView != null) inspectorView.UpdateSelection(null);
        }
        else if (obj == PlayModeStateChange.EnteredEditMode)
        {
            OnSelectionChange();
        }
    }

    private void OnSelectionChange()
    {
        if (Application.isPlaying)
        {
            if (Selection.activeGameObject != null)
            {
                var runner = GetSelectedRunner();
                currentRunner = runner;
                if (runner != null && runner.treeAsset != null)
                {
                    LoadTree(runner.treeAsset);
                    return;
                }
            }
        }
        else
        {
            if (Selection.activeObject is BehaviorTreeAsset tree)
            {
                LoadTree(tree);
            }
            // 不要在这里加 else LoadTree(null)！
            // 因为当你在编辑器里点击空白处，或者选中别的普通物体（如文件夹）时，
            // activeObject 就会变成其他类型，如果加了 else 就会强行把你当前的画布顶掉重置为新画布！
        }
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying && graphView != null)
        {
            graphView.UpdateNodeStates(currentRunner);
            // 确保面板每一帧重绘，以便实时看到值的变化
            rootVisualElement.Q<IMGUIContainer>()?.MarkDirtyRepaint();
        }
    }
}
