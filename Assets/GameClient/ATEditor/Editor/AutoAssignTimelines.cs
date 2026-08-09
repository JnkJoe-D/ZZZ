using UnityEditor;
using UnityEngine;
using Game.Logic;
using ATEditor;

namespace ATEditor.Editor
{
    public class AutoAssignTimelines
    {
        [MenuItem("ATEditor/实用工具/自动分配角色动作 SO")]
        public static void Execute()
        {
            string folderPath = "Assets/Resources/Serializations/ScriptableObjects/Action";
            string[] guids = AssetDatabase.FindAssets("t:ActionConfigAsset", new[] { folderPath });
            
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionConfigAsset config = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(path);
                
                if (config != null && config.TimelineAsset != null)
                {
                    string soName = config.TimelineAsset.name;
                    string[] soGuids = AssetDatabase.FindAssets(soName + " t:ActionTimeline");
                    if (soGuids.Length > 0)
                    {
                        string soPath = AssetDatabase.GUIDToAssetPath(soGuids[0]);
                        ActionTimeline so = AssetDatabase.LoadAssetAtPath<ActionTimeline>(soPath);
                        if (so != null && config.actionTimelineSO != so)
                        {
                            config.actionTimelineSO = so;
                            EditorUtility.SetDirty(config);
                            count++;
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("自动分配完成", $"成功为 {count} 个 ActionConfigAsset 匹配并分配了对应的 ActionTimeline SO！", "好的");
        }
    }
}
