using System.Collections.Generic;
using UnityEngine;

namespace Game.Logic.AI.BehaviorTree
{
    [CreateAssetMenu(menuName = "AI/BehaviorTree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        public NodeData rootNode;
        
        [HideInInspector]
        public List<NodeData> nodes = new List<NodeData>();

        public NodeData CreateNode(System.Type type)
        {
            var node = ScriptableObject.CreateInstance(type) as NodeData;
            if (node != null)
            {
                node.name = type.Name;
                node.guid = System.Guid.NewGuid().ToString();
                nodes.Add(node);

                if (node is RootData && rootNode == null)
                {
                    rootNode = node;
                }

#if UNITY_EDITOR
                bool isSavedOnDisk = !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this));
                if (isSavedOnDisk)
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(node, this);
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
#endif
            }
            return node;
        }

        public void DeleteNode(NodeData node)
        {
            nodes.Remove(node);
            if (node == rootNode)
            {
                rootNode = null;
            }

#if UNITY_EDITOR
            bool isSavedOnDisk = !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this));
            if (isSavedOnDisk)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(node);
                DestroyImmediate(node, true);
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            else
            {
                DestroyImmediate(node);
            }
#endif
        }
    }
}
