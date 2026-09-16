using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Framework
{
    /// <summary>
    /// 一键迁移并修复动作配置文件（.asset）中 [SerializeReference] 路由触发器及条件的旧命名空间。
    /// 将历史遗留的 Game.Logic 命名空间批量精准对齐为 Game.GamePlay。
    /// </summary>
    public static class ActionRouteMigrationTool
    {
        private static readonly string[] MigratedClasses = new[]
        {
            "IntentCommandTrigger",
            "DirectAssetTrigger",
            "SystemEventTrigger",
            "AutoTransitionTrigger",
            "ConditionOnlyTrigger",
            "MoveInputCondition",
            "ShortMoveInputCondition",
            "LostMoveInputCondition",
            "HasMovementInputCondition",
            "TimeSinceActionStartCondition",
            "HasTargetCondition",
            "TargetDistanceCondition",
            "MovementDirectionCondition",
            "PreviousActionCondition",
            "SwitchOutPendingCondition",
            "CombatWarningCondition",
            "ParrySucceededCondition",
            "IsInBulletTimeCondition",
            "ParryWeightCondition"
        };

        [MenuItem("Tools/Action/Migrate Route Namespaces (Game.Logic -> Game.GamePlay)", false, 200)]
        public static void MigrateRouteNamespaces()
        {
            string assetDir = Application.dataPath;
            string patternStr = @"(type:\s*\{class:\s*(" + string.Join("|", MigratedClasses) + @"),\s*ns:\s*)Game\.Logic(,\s*asm:\s*Assembly-CSharp\})";
            Regex regex = new Regex(patternStr);

            string[] assetFiles = Directory.GetFiles(assetDir, "*.asset", SearchOption.AllDirectories);
            int modifiedFiles = 0;
            int totalReplacements = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < assetFiles.Length; i++)
                {
                    string filePath = assetFiles[i];
                    EditorUtility.DisplayProgressBar("Migrating Route Namespaces", Path.GetFileName(filePath), (float)i / assetFiles.Length);

                    string content = File.ReadAllText(filePath);
                    int count = 0;
                    string newContent = regex.Replace(content, match =>
                    {
                        count++;
                        return $"{match.Groups[1].Value}Game.GamePlay{match.Groups[3].Value}";
                    });

                    if (count > 0)
                    {
                        File.WriteAllText(filePath, newContent);
                        modifiedFiles++;
                        totalReplacements += count;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log($"[ActionRouteMigration] 完成路由命名空间迁移！共检查 {assetFiles.Length} 个资产文件，修复 {modifiedFiles} 个文件，更新 {totalReplacements} 处多态引用。");
            EditorUtility.DisplayDialog("路由命名空间迁移完成", $"共修复 {modifiedFiles} 个文件，恢复 {totalReplacements} 处路由策略引用！", "确定");
        }
    }
}
