using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace GameClient.Editor.Utils
{
    public class FBXPrefixRenamer : EditorWindow
    {
        private string _prefix = "Mod_";

        [MenuItem("Assets/Batch Rename/Add Prefix to FBX Files", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<FBXPrefixRenamer>("FBX Renamer");
            window.minSize = new Vector2(300, 100);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("为选中的 FBX 文件批量添加前缀", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _prefix = EditorGUILayout.TextField("前缀 (Prefix)", _prefix);

            EditorGUILayout.Space();

            if (GUILayout.Button("批量重命名 (Rename)", GUILayout.Height(30)))
            {
                RenameSelectedFBX();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("提示: 仅会处理扩展名为 .fbx 的文件。", MessageType.Info);
        }

        private void RenameSelectedFBX()
        {
            Object[] selectedObjects = Selection.objects;
            List<string> fbxPaths = new List<string>();

            foreach (Object obj in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.ToLower().EndsWith(".fbx"))
                {
                    fbxPaths.Add(path);
                }
            }

            if (fbxPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选中至少一个 FBX 资源。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(_prefix))
            {
                EditorUtility.DisplayDialog("警告", "前缀不能为空。", "确定");
                return;
            }

            int renamedCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in fbxPaths)
                {
                    string oldName = Path.GetFileNameWithoutExtension(path);
                    string newName = _prefix + oldName;

                    string result = AssetDatabase.RenameAsset(path, newName);
                    if (string.IsNullOrEmpty(result))
                    {
                        renamedCount++;
                    }
                    else
                    {
                        Debug.LogError($"重命名失败: {oldName} -> {newName}. 错误: {result}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成", $"重命名操作已完成！\n成功: {renamedCount}\n总计: {fbxPaths.Count}", "确定");
            
            if (renamedCount > 0)
            {
                Close();
            }
        }
    }
}
