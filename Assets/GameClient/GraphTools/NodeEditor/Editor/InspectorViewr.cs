using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class InspectorViewr : VisualElement
{
    public new class UxmlFactory : UxmlFactory<InspectorViewr, UxmlTraits> { }
    Editor editor;
    internal void UpdateSelection(TreeNodeView nodeView)
    {
        Clear();
        if (nodeView == null) return;
        UnityEngine.Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(nodeView.node);
        IMGUIContainer container = new IMGUIContainer(() => 
        { if (editor.target) editor.OnInspectorGUI(); });
        Add(container);
    }
}
