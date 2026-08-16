using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Logic.AI.BehaviorTree
{
    [CreateAssetMenu(menuName = "Behavior Tree/Behavior Tree Asset")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        public Node rootNode;
        public NodeState treeState = NodeState.Inactive;
        public Blackboard blackboard;
        
        [HideInInspector]
        public List<Node> nodes = new List<Node>();

        public Node CreateNode(Type type)
        {
            Node node = ScriptableObject.CreateInstance(type) as Node;
            node.name = type.Name;
            node.guid = Guid.NewGuid().ToString();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AssetDatabase.AddObjectToAsset(node, this);
                Undo.RegisterCreatedObjectUndo(node, "Behavior Tree (CreateNode)");
            }
#endif
            nodes.Add(node);
            
            if (node is Root && rootNode == null)
            {
                rootNode = node;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif
            return node;
        }

        public Node DeleteNode(Node node)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Behavior Tree (DeleteNode)");
            }
#endif
            var result = node;
            nodes.Remove(node);
            
            if (node == rootNode)
            {
                rootNode = null;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                Undo.DestroyObjectImmediate(node);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif
            return result;
        }
    
        public BehaviorTreeAsset Clone()
        {
            BehaviorTreeAsset tree = Instantiate(this);
            if(tree.rootNode != null)
            {
                tree.rootNode = tree.rootNode.Clone();
            }
            tree.nodes = new List<Node>();
            Traverse(tree.rootNode, (n) => 
            {
                tree.nodes.Add(n);
            });
            return tree;
        }

        public static void Traverse(Node node, Action<Node> visiter)
        {
            if (node != null)
            {
                visiter.Invoke(node);
                var children = node.GetChildren();
                foreach (var child in children)
                {
                    Traverse(child, visiter);
                }
            }
        }

}
}
