using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ATEditor.Editor;
using Game.GamePlay;

namespace Game.Editor.ActionTransition
{
    /// <summary>
    /// 动作过渡工作台角色工作区服务。
    /// 深度联动 ATEditor 角色工作区中央数据库 (ATEditorWorkspaceDatabase)，
    /// 实现跨系统共享角色定义、自动切换模型预制体、自动扫描并索引归属动作列表。
    /// </summary>
    public static class ActionTransitionWorkspaceService
    {
        /// <summary>
        /// 获取所有已配置的角色工作区
        /// </summary>
        public static IReadOnlyList<ATWorkspaceDefinition> GetAllWorkspaces()
        {
            var db = ATEditorWorkspaceDatabase.Instance;
            return db != null ? db.Workspaces : Array.Empty<ATWorkspaceDefinition>();
        }

        /// <summary>
        /// 根据 Id 获取指定角色工作区
        /// </summary>
        public static ATWorkspaceDefinition GetWorkspaceById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var db = ATEditorWorkspaceDatabase.Instance;
            return db?.GetWorkspaceById(id);
        }

        /// <summary>
        /// 根据动作资产推导所属的角色工作区
        /// </summary>
        public static ATWorkspaceDefinition InferWorkspace(ActionConfigAsset action)
        {
            if (action == null) return null;
            string path = AssetDatabase.GetAssetPath(action);
            if (string.IsNullOrEmpty(path)) return null;

            string normalized = path.Replace('\\', '/');
            var workspaces = GetAllWorkspaces();

            // 1. 优先按 FolderName 匹配（如 "Player/Ellen" 匹配包含 "/Ellen/" 或 "/Player/Ellen/"）
            foreach (var ws in workspaces)
            {
                if (string.IsNullOrEmpty(ws.FolderName)) continue;
                string folderToken = "/" + ws.FolderName.Trim('/') + "/";
                if (normalized.IndexOf(folderToken, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ws;
                }
            }

            // 2. 尝试按末尾角色名匹配（如 "Ellen" 匹配 "/Ellen/"）
            foreach (var ws in workspaces)
            {
                if (string.IsNullOrEmpty(ws.FolderName)) continue;
                string roleName = Path.GetFileName(ws.FolderName.Trim('/'));
                if (!string.IsNullOrEmpty(roleName) && normalized.IndexOf("/" + roleName + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ws;
                }
            }

            // 3. 尝试按 DisplayName 或 Id 包含匹配
            foreach (var ws in workspaces)
            {
                if (!string.IsNullOrEmpty(ws.Id) && normalized.IndexOf(ws.Id, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ws;
                }
            }

            return null;
        }

        /// <summary>
        /// 扫描并获取属于指定工作区的所有 ActionConfigAsset 列表（按字母名称升序排序）
        /// </summary>
        public static List<ActionConfigAsset> GetActionsForWorkspace(ATWorkspaceDefinition ws)
        {
            var results = new List<ActionConfigAsset>();
            if (ws == null || string.IsNullOrEmpty(ws.FolderName)) return results;

            string roleToken = Path.GetFileName(ws.FolderName.Trim('/'));
            if (string.IsNullOrEmpty(roleToken)) return results;

            // 1. 优先在标准 CharacterConfig 目录下精确定位
            string[] candidateDirs = new string[]
            {
                $"Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/{roleToken}/Action",
                $"Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Role/{roleToken}",
                $"Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Monster/{roleToken}/Action",
                $"Assets/Resources/Serializations/ScriptableObjects/CharacterConfig/Monster/{roleToken}"
            };

            bool foundInExplicitDir = false;
            foreach (var dir in candidateDirs)
            {
                if (Directory.Exists(dir))
                {
                    string[] guids = AssetDatabase.FindAssets("t:ActionConfigAsset", new string[] { dir });
                    foreach (var guid in guids)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guid);
                        var asset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(p);
                        if (asset != null && !results.Contains(asset))
                        {
                            results.Add(asset);
                            foundInExplicitDir = true;
                        }
                    }
                }
            }

            // 2. 若显式路径未找到或目录结构不一致，全工程按角色名路径过滤兜底
            if (!foundInExplicitDir)
            {
                string[] allGuids = AssetDatabase.FindAssets("t:ActionConfigAsset");
                string matchToken = "/" + roleToken + "/";
                foreach (var guid in allGuids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                    if (p.IndexOf(matchToken, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<ActionConfigAsset>(p);
                        if (asset != null && !results.Contains(asset))
                        {
                            results.Add(asset);
                        }
                    }
                }
            }

            // 按名称自然排序
            results.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return results;
        }

        /// <summary>
        /// 打开 ATEditor 工作区管理器窗口
        /// </summary>
        public static void OpenWorkspaceManager()
        {
            ATWorkspaceManagerWindow.OpenWindow();
        }
    }
}
