using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace Game.Editor.BehaviorTree
{
    public class InspectorViewr : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InspectorViewr, UxmlTraits> { }
        public System.Action onNodeModified;
        UnityEditor.Editor editor;
        internal void UpdateSelection(TreeNodeView nodeView)
        {
            Clear();
            if (nodeView == null) return;
            UnityEngine.Object.DestroyImmediate(editor);
            editor = UnityEditor.Editor.CreateEditor(nodeView.node);
            IMGUIContainer container = new IMGUIContainer(() => 
            { 
                if (editor.target) 
                {
                    EditorGUI.BeginChangeCheck();
                    editor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        onNodeModified?.Invoke();
                    }
                }
            });
            Add(container);
        }
    }
}
