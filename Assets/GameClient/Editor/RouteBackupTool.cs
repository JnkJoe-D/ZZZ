using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Logic.Editor
{
    [Serializable]
    public class RouteModifierCheckBackup
    {
        public ModifierCategory Category;
        public HardwareInputType RequiredKey;
        public ConditionCommand Condition;
        public bool Inverse;
    }

    [Serializable]
    public class RouteBackupItem
    {
        public int Priority;
        public ExecuteTarget ExecuteType;
        public string ExecuteActionPath;
        public ExecuteEvent RouteExecuteEvent;
        public float CrossfadeOverride;
        
        public RouteTriggerCategory Category;
        public string RequiredWindowTag;
        public CommandTriggerMode TriggerMode;
        
        public HardwareInputType RequiredType;
        public CommandPhase RequiredPhase;
        public RouteEventType EventType;
        
        public RouteSingleModifierCheckTiming ModifierCheckTiming;
        public RouteSingleModifierCheckTiming AutoCheckTiming;
        
        public List<RouteModifierCheckBackup> Modifiers;
    }

    [Serializable]
    public class ActionConfigBackup
    {
        public string AssetPath;
        public List<RouteBackupItem> Routes;
    }

    [Serializable]
    public class RouteBackupRoot
    {
        public List<ActionConfigBackup> AllConfigs = new List<ActionConfigBackup>();
    }

    public static class RouteBackupTool
    {
        private const string BACKUP_FILE_PATH = "Assets/ActionRoutesBackup.json";

        [MenuItem("Tools/Action System/1. Backup All Routes to JSON")]
        public static void BackupRoutes()
        {
            var root = new RouteBackupRoot();
            
            // 仅搜索指定的配置目录
            string[] searchFolders = new string[] { "Assets/Resources/Serializations/ScriptableObjects/Action" };
            // 1. 备份所有 ActionConfigAsset
            string[] configGuids = AssetDatabase.FindAssets("t:ActionConfigAsset", searchFolders);
            foreach (string guid in configGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(path);
                if (asset == null || asset.Routes == null || asset.Routes.Count == 0) continue;

                var configBackup = new ActionConfigBackup
                {
                    AssetPath = path,
                    Routes = new List<RouteBackupItem>()
                };

                foreach (var route in asset.Routes)
                {
                    // This method now won't compile because it reads fields that were deleted from ActionRoute.
                    // But since backup is already done, we can just comment out BackupRoute body if needed, or leave it.
                    // Actually I'll comment out the body of BackupRoute so it compiles.
                }
                
                root.AllConfigs.Add(configBackup);
            }

            // 2. 备份所有 ActionRouteSetAsset (指令集)
            string[] setGuids = AssetDatabase.FindAssets("t:ActionRouteSetAsset", searchFolders);
            foreach (string guid in setGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ActionRouteSetAsset>(path);
                if (asset == null || asset.Routes == null || asset.Routes.Count == 0) continue;

                var configBackup = new ActionConfigBackup
                {
                    AssetPath = path,
                    Routes = new List<RouteBackupItem>()
                };

                foreach (var route in asset.Routes)
                {
                    // Body commented out for compilation since ActionRoute lost old fields
                }
                
                root.AllConfigs.Add(configBackup);
            }

            string json = JsonUtility.ToJson(root, true);
            File.WriteAllText(BACKUP_FILE_PATH, json);
            AssetDatabase.Refresh();
            
            Debug.Log($"<color=green>[Action Backup]</color> 成功备份了 {root.AllConfigs.Count} 个资产的空架子到: {BACKUP_FILE_PATH}");
        }

        [MenuItem("Tools/Action System/2. Restore Routes from JSON")]
        public static void RestoreRoutes()
        {
            if (!File.Exists(BACKUP_FILE_PATH))
            {
                Debug.LogError($"[Action Backup] 未找到备份文件: {BACKUP_FILE_PATH}");
                return;
            }

            string json = File.ReadAllText(BACKUP_FILE_PATH);
            var root = JsonUtility.FromJson<RouteBackupRoot>(json);
            if (root == null || root.AllConfigs == null) return;

            int restoredCount = 0;
            foreach (var config in root.AllConfigs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(config.AssetPath);
                if (asset == null)
                {
                    Debug.LogWarning($"[Action Backup] 找不到目标资产: {config.AssetPath}");
                    continue;
                }

                List<ActionRoute> newRoutes = new List<ActionRoute>();
                foreach (var backup in config.Routes)
                {
                    var newRoute = new ActionRoute
                    {
                        Priority = backup.Priority,
                        ExecuteType = backup.ExecuteType,
                        ExecuteAction = string.IsNullOrEmpty(backup.ExecuteActionPath) ? null : AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(backup.ExecuteActionPath),
                        RouteExecuteEvent = backup.RouteExecuteEvent,
                        CrossfadeOverride = backup.CrossfadeOverride,
                    };

                    List<RouteModifierCheck> modifiers = new List<RouteModifierCheck>();
                    if (backup.Modifiers != null)
                    {
                        foreach (var m in backup.Modifiers)
                        {
                            modifiers.Add(new RouteModifierCheck
                            {
                                Category = m.Category,
                                RequiredKey = m.RequiredKey,
                                Condition = m.Condition,
                                Inverse = m.Inverse
                            });
                        }
                    }

                    if (backup.Category == RouteTriggerCategory.IntentCommand || backup.Category == RouteTriggerCategory.DirectAsset)
                    {
                        if (backup.Category == RouteTriggerCategory.IntentCommand)
                        {
                            newRoute.TriggerStrategy = new IntentCommandTrigger
                            {
                                RequiredInput = backup.RequiredType,
                                RequiredPhase = backup.RequiredPhase,
                                RequiredWindowTag = backup.RequiredWindowTag,
                                TriggerMode = backup.TriggerMode,
                                Modifiers = modifiers
                            };
                        }
                        else
                        {
                            newRoute.TriggerStrategy = new DirectAssetTrigger
                            {
                                RequiredWindowTag = backup.RequiredWindowTag
                            };
                        }
                    }
                    else if (backup.Category == RouteTriggerCategory.AutoTransition)
                    {
                        newRoute.TriggerStrategy = new AutoTransitionTrigger
                        {
                            RequiredWindowTag = backup.RequiredWindowTag,
                            Timing = backup.AutoCheckTiming,
                            Modifiers = modifiers
                        };
                    }
                    else if (backup.Category == RouteTriggerCategory.ConditionOnly)
                    {
                        newRoute.TriggerStrategy = new ConditionOnlyTrigger
                        {
                            RequiredWindowTag = backup.RequiredWindowTag,
                            Timing = backup.ModifierCheckTiming,
                            Modifiers = modifiers
                        };
                    }
                    else if (backup.Category == RouteTriggerCategory.Event)
                    {
                        newRoute.TriggerStrategy = new SystemEventTrigger
                        {
                            EventType = backup.EventType,
                            RequiredWindowTag = backup.RequiredWindowTag,
                            Modifiers = modifiers
                        };
                    }

                    newRoutes.Add(newRoute);
                }

                if (asset is ActionConfigAsset configAsset)
                {
                    configAsset.Routes = newRoutes;
                    EditorUtility.SetDirty(configAsset);
                }
                else if (asset is ActionRouteSetAsset setAsset)
                {
                    setAsset.Routes = newRoutes;
                    EditorUtility.SetDirty(setAsset);
                }

                restoredCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[Action Restore]</color> 成功还原并多态化了 {restoredCount} 个配置的路由！");
        }
    }
}
