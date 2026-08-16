using Game.Logic.AI.BehaviorTree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TreeNodeEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    public TreeGraphViewr graphView;
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
    }

    public void LoadTree(BehaviorTreeAsset tree)
    {
        if (graphView == null) return;
        graphView.PopulateView(tree);
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
                var runner = Selection.activeGameObject.GetComponent<BTRunner>();
                if (runner != null && runner.RuntimeTree != null)
                {
                    LoadTree(runner.RuntimeTree);
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
        }
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying && graphView != null)
        {
            graphView.UpdateNodeStates();
        }
    }
}
