using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditor 工作区中央数据库 (ScriptableObject)
    /// 管理所有角色工作区配置，负责增删改查、唯一性校验与持久化
    /// </summary>
    public class ATEditorWorkspaceDatabase : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GameClient/Dependencies/ATEditor/Settings/WorkspaceDatabase.asset";
        public const string DefaultCategory = "None";

        [SerializeField]
        private List<string> categories = new List<string>();

        [SerializeField]
        private List<ATWorkspaceDefinition> workspaces = new List<ATWorkspaceDefinition>();

        private static ATEditorWorkspaceDatabase _instance;

        public static ATEditorWorkspaceDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetOrCreateDatabase();
                }
                return _instance;
            }
        }

        public IReadOnlyList<string> Categories
        {
            get
            {
                EnsureDefaultCategories();
                return categories;
            }
        }

        public IReadOnlyList<ATWorkspaceDefinition> Workspaces => workspaces;

        public static ATEditorWorkspaceDatabase GetOrCreateDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<ATEditorWorkspaceDatabase>(DefaultAssetPath);
            if (db == null)
            {
                string directory = Path.GetDirectoryName(DefaultAssetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                db = CreateInstance<ATEditorWorkspaceDatabase>();
                db.EnsureDefaultCategories();
                db.EnsureDefaultWorkspaces();
                AssetDatabase.CreateAsset(db, DefaultAssetPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                bool dirty = false;
                if (db.categories == null || db.categories.Count == 0)
                {
                    db.EnsureDefaultCategories();
                    dirty = true;
                }
                if (db.workspaces == null || db.workspaces.Count == 0)
                {
                    db.EnsureDefaultWorkspaces();
                    dirty = true;
                }
                if (dirty)
                {
                    EditorUtility.SetDirty(db);
                    AssetDatabase.SaveAssets();
                }
            }
            return db;
        }

        public ATWorkspaceDefinition GetWorkspaceById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return workspaces.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 根据动作资产路径自动推导所属的角色工作区
        /// 例如 Assets/.../ActionTimelines/Player/Ellen/Atk01.asset -> 匹配 FolderName="Player/Ellen"
        /// </summary>
        public ATWorkspaceDefinition GetWorkspaceByAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            string normalized = assetPath.Replace('\\', '/');

            // 提取相对于 ActionTimelines 根目录的相对路径
            string marker = "/ActionTimelines/";
            int markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                string relative = normalized.Substring(markerIndex + marker.Length);
                string dir = Path.GetDirectoryName(relative)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir))
                {
                    // 1. 优先完全一致匹配 FolderName (如 "Player/Ellen")
                    var matched = workspaces.Find(w => string.Equals(w.FolderName?.Trim('/'), dir.Trim('/'), StringComparison.OrdinalIgnoreCase));
                    if (matched != null) return matched;

                    // 2. 尝试前缀或层级包含匹配
                    matched = workspaces.Find(w => dir.StartsWith(w.FolderName?.Trim('/'), StringComparison.OrdinalIgnoreCase));
                    if (matched != null) return matched;
                }
            }

            // 3. 尝试直接在路径中包含 FolderName
            foreach (var ws in workspaces)
            {
                if (!string.IsNullOrEmpty(ws.FolderName) && normalized.IndexOf("/" + ws.FolderName.Trim('/') + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ws;
                }
            }

            return null;
        }

        public void EnsureDefaultCategories()
        {
            if (categories == null) categories = new List<string>();
            if (!categories.Contains(DefaultCategory))
            {
                categories.Insert(0, DefaultCategory);
            }
            else if (categories.IndexOf(DefaultCategory) != 0)
            {
                categories.Remove(DefaultCategory);
                categories.Insert(0, DefaultCategory);
            }

            // 初次如果只有 None，注入初始分类
            if (categories.Count == 1)
            {
                if (!categories.Contains("Player")) categories.Add("Player");
                if (!categories.Contains("Monster")) categories.Add("Monster");
                if (!categories.Contains("NPC")) categories.Add("NPC");
                if (!categories.Contains("Common")) categories.Add("Common");
            }
        }

        public List<string> GetCategories()
        {
            EnsureDefaultCategories();
            return new List<string>(categories);
        }

        public bool AddCategory(string name, out string errorMessage)
        {
            errorMessage = string.Empty;
            EnsureDefaultCategories();

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "分类名称不能为空。";
                return false;
            }

            string trimmed = name.Trim();
            if (categories.Exists(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"已存在分类 '{trimmed}'。";
                return false;
            }

            categories.Add(trimmed);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        public bool DeleteCategory(string name, out string errorMessage)
        {
            errorMessage = string.Empty;
            EnsureDefaultCategories();

            if (string.Equals(name, DefaultCategory, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "预设分类 'None' 不可删除！";
                return false;
            }

            int index = categories.FindIndex(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                errorMessage = $"未找到分类 '{name}'。";
                return false;
            }

            // 级联处理：将其下的所有工作区分类重置为 None
            foreach (var ws in workspaces)
            {
                if (string.Equals(ws.Category, name, StringComparison.OrdinalIgnoreCase))
                {
                    ws.Category = DefaultCategory;
                }
            }

            categories.RemoveAt(index);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        public bool RenameCategory(string oldName, string newName, out string errorMessage)
        {
            errorMessage = string.Empty;
            EnsureDefaultCategories();

            if (string.Equals(oldName, DefaultCategory, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "预设分类 'None' 不可重命名！";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                errorMessage = "新分类名称不能为空。";
                return false;
            }

            string trimmedNew = newName.Trim();
            if (string.Equals(oldName, trimmedNew, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (categories.Exists(c => string.Equals(c, trimmedNew, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"已存在分类 '{trimmedNew}'。";
                return false;
            }

            int index = categories.FindIndex(c => string.Equals(c, oldName, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                errorMessage = $"未找到分类 '{oldName}'。";
                return false;
            }

            categories[index] = trimmedNew;

            // 级联更新工作区分类
            foreach (var ws in workspaces)
            {
                if (string.Equals(ws.Category, oldName, StringComparison.OrdinalIgnoreCase))
                {
                    ws.Category = trimmedNew;
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        public List<ATWorkspaceDefinition> GetWorkspacesByCategory(string category)
        {
            var list = new List<ATWorkspaceDefinition>();
            foreach (var ws in workspaces)
            {
                if (string.Equals(ws.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(ws);
                }
            }
            return list;
        }

        /// <summary>
        /// 校验工作区 ID 与 FolderName 的唯一性与合法性
        /// </summary>
        public bool ValidateWorkspace(string id, string folderName, string currentId, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                errorMessage = "工作区 ID 不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                errorMessage = "工作区目录 (FolderName) 不能为空。";
                return false;
            }

            // 检查非法路径字符
            char[] invalidChars = Path.GetInvalidPathChars();
            if (folderName.IndexOfAny(invalidChars) >= 0 || folderName.Contains(":") || folderName.Contains("*") || folderName.Contains("?"))
            {
                errorMessage = "工作区目录包含非法字符。";
                return false;
            }

            foreach (var ws in workspaces)
            {
                if (!string.IsNullOrEmpty(currentId) && string.Equals(ws.Id, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 忽略自身
                }

                if (string.Equals(ws.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"已存在 ID 为 '{id}' 的工作区，请使用唯一 ID。";
                    return false;
                }

                if (string.Equals(ws.FolderName?.Trim('/'), folderName.Trim('/'), StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"已存在目录为 '{folderName}' 的工作区，避免目录冲突。";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 添加新工作区
        /// </summary>
        public bool AddWorkspace(ATWorkspaceDefinition def, out string errorMessage)
        {
            if (!ValidateWorkspace(def.Id, def.FolderName, null, out errorMessage))
            {
                return false;
            }

            workspaces.Add(def);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// 更新已有工作区
        /// </summary>
        public bool UpdateWorkspace(ATWorkspaceDefinition def, out string errorMessage)
        {
            int index = workspaces.FindIndex(w => string.Equals(w.Id, def.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                errorMessage = $"未找到 ID 为 '{def.Id}' 的工作区。";
                return false;
            }

            if (!ValidateWorkspace(def.Id, def.FolderName, def.Id, out errorMessage))
            {
                return false;
            }

            workspaces[index] = def;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// 删除工作区（仅从配置注销，不删物理资产）
        /// </summary>
        public bool RemoveWorkspace(string id)
        {
            int count = workspaces.RemoveAll(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            if (count > 0)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 初始化默认工作区（包含项目现有的艾莲、安比、提尔锋、通用）
        /// </summary>
        public void EnsureDefaultWorkspaces()
        {
            if (workspaces == null) workspaces = new List<ATWorkspaceDefinition>();

            void TryAddDefault(string id, string category, string displayName, string folderName, string prefabPath)
            {
                if (workspaces.Exists(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase))) return;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                workspaces.Add(new ATWorkspaceDefinition(id, category, displayName, folderName, prefab));
            }

            TryAddDefault("Player_Ellen", "Player", "艾莲 (Ellen)", "Player/Ellen", "Assets/Resources/Prefab/Role/Ellen/Avatar_Female_Size02_Ellen.prefab");
            TryAddDefault("Player_Anby", "Player", "安比 (Anby)", "Player/Anby", "Assets/Resources/Prefab/Role/Anbi/Anbi.prefab");
            TryAddDefault("Player_HoshimiMiyabi", "Player", "星见雅 (Miyabi)", "Player/HoshimiMiyabi", "Assets/Resources/Prefab/Role/Unagi/Unagi.prefab");
            TryAddDefault("Monster_TyrfingInfested", "Monster", "侵蚀提尔锋 (TyrfingInfested)", "Monster/TyrfingInfested", "Assets/Resources/Prefab/Monster/Monster_TyrfingInfested.prefab");
            TryAddDefault("Common_Shared", "Common", "通用模板 (Common)", "Common", "Assets/ATEditor/Editor/Resources/DefaultPreviewCharacter.prefab");
        }
    }
}
