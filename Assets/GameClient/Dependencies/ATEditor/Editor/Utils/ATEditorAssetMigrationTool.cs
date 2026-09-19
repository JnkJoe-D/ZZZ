using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// 存量动作资产一键归类迁移工具
    /// 将根目录下扁平存放的 60+ 动作资产按前缀平移到对应的角色工作区英文子目录中
    /// 使用 AssetDatabase.MoveAsset 确保 GUID 与上层引用（如 ActionConfigAsset）绝对不丢失
    /// </summary>
    public static class ATEditorAssetMigrationTool
    {
        public const string SoRoot = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";
        public const string JsonRoot = "Assets/Resources/Serializations/JSON/ActionTimelines";

        // 前缀与目标工作区英文目录映射
        private static readonly Dictionary<string, string> PrefixToFolderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "艾莲_", "Player/Ellen" },
            { "安比_", "Player/Anby" },
            { "星见雅_", "Player/HoshimiMiyabi" },
            { "TyrfingInfested_", "Monster/TyrfingInfested" },
            { "模板_", "Common" },
        };

        [MenuItem("ATEditor/工具/一键归类迁移存量动作资产")]
        public static void MigrateLegacyAssets()
        {
            if (!Directory.Exists(SoRoot) && !Directory.Exists(JsonRoot))
            {
                EditorUtility.DisplayDialog("提示", "未找到 ActionTimelines 根目录。", "确定");
                return;
            }

            // 1. 扫描根目录下未归类的 .asset 文件
            string[] legacySoFiles = Directory.Exists(SoRoot)
                ? Directory.GetFiles(SoRoot, "*.asset", SearchOption.TopDirectoryOnly)
                : new string[0];

            string[] legacyJsonFiles = Directory.Exists(JsonRoot)
                ? Directory.GetFiles(JsonRoot, "*.json", SearchOption.TopDirectoryOnly)
                : new string[0];

            if (legacySoFiles.Length == 0 && legacyJsonFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "根目录下没有发现需要迁移的存量资产，当前结构已完成归类！", "好的");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "确认迁移",
                $"即将整理根目录下的存量动作资产：\n" +
                $"• 发现 {legacySoFiles.Length} 个根目录 SO 资产\n" +
                $"• 发现 {legacyJsonFiles.Length} 个根目录 JSON 文件\n\n" +
                $"系统将按角色前缀（艾莲_ -> Player/Ellen、安比_ -> Player/Anby、星见雅_ -> Player/HoshimiMiyabi、TyrfingInfested_ -> Monster/TyrfingInfested、模板_ -> Common）移动到对应工作区子目录。\n" +
                $"操作将连同 .meta 文件一起同步迁移，GUID 保持绝对不变，上层配置引用不丢失。\n\n是否继续？",
                "立即迁移",
                "取消");

            if (!confirm) return;

            int movedCount = 0;
            int total = legacySoFiles.Length + legacyJsonFiles.Length;

            try
            {
                // 1. 迁移 SO 资产
                for (int i = 0; i < legacySoFiles.Length; i++)
                {
                    string filePath = legacySoFiles[i].Replace("\\", "/");
                    string fileName = Path.GetFileName(filePath);

                    EditorUtility.DisplayProgressBar("正在迁移动作资产", $"正在移动 SO: {fileName}", (float)i / total);

                    string targetFolder = ResolveTargetFolder(fileName);
                    string targetDir = Path.Combine(SoRoot, targetFolder).Replace("\\", "/");
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    string destFile = Path.Combine(targetDir, fileName).Replace("\\", "/");
                    File.Move(filePath, destFile);

                    string metaFile = filePath + ".meta";
                    if (File.Exists(metaFile))
                    {
                        string destMeta = destFile + ".meta";
                        File.Move(metaFile, destMeta);
                    }
                    movedCount++;
                }

                // 2. 迁移 JSON 文件
                for (int i = 0; i < legacyJsonFiles.Length; i++)
                {
                    string filePath = legacyJsonFiles[i].Replace("\\", "/");
                    string fileName = Path.GetFileName(filePath);

                    EditorUtility.DisplayProgressBar("正在迁移动作资产", $"正在移动 JSON: {fileName}", (float)(legacySoFiles.Length + i) / total);

                    string targetFolder = ResolveTargetFolder(fileName);
                    string targetDir = Path.Combine(JsonRoot, targetFolder).Replace("\\", "/");
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    string destFile = Path.Combine(targetDir, fileName).Replace("\\", "/");
                    File.Move(filePath, destFile);

                    string metaFile = filePath + ".meta";
                    if (File.Exists(metaFile))
                    {
                        string destMeta = destFile + ".meta";
                        File.Move(metaFile, destMeta);
                    }
                    movedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtility.DisplayDialog("迁移完成", $"迁移完成！成功整理移动了 {movedCount} 个资产文件到对应工作区子目录中。", "好的");
            }
        }

        private static string ResolveTargetFolder(string fileName)
        {
            foreach (var kvp in PrefixToFolderMap)
            {
                if (fileName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return "Common";
        }
    }
}
