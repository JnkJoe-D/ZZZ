using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Editor.Workspace;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditor 工作区中央数据库 (ScriptableObject)
    /// 承载 ATEditor 专属扩展数据（PreviewPrefab、场景站位），并与公共 SharedWorkspaces.json 双向联动。
    /// 分类固定为：Role, Monster, Common
    /// 命名示例：Role_Ellen, Monster_TyrfingInfested, Common_Shared
    /// </summary>
    public class ATEditorWorkspaceDatabase : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GameClient/Dependencies/ATEditor/Settings/WorkspaceDatabase.asset";
        public const string DefaultCategory = SharedWorkspaceDefinition.CategoryRole;

        public static readonly string[] FixedCategories = new[]
        {
            SharedWorkspaceDefinition.CategoryRole,
            SharedWorkspaceDefinition.CategoryMonster,
            SharedWorkspaceDefinition.CategoryCommon
        };

        [SerializeField]
        private List<string> categories = new List<string>(FixedCategories);

        [SerializeField]
        private List<ATWorkspaceDefinition> workspaces = new List<ATWorkspaceDefinition>();

        [Header("工作区全局路径配置")]
        [SerializeField]
        private string jsonRootDirectory = "Assets/Resources/Serializations/JSON/ActionTimelines";

        [SerializeField]
        private string soRootDirectory = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";

        [SerializeField]
        private string sharedWorkspaceJsonPath = "Assets/Editor/Settings/Workspace/SharedWorkspaces.json";

        public string JsonRootDirectory
        {
            get => string.IsNullOrEmpty(jsonRootDirectory) ? "Assets/Resources/Serializations/JSON/ActionTimelines" : jsonRootDirectory;
            set
            {
                string normalized = value?.Trim().Replace('\\', '/').TrimEnd('/');
                if (string.IsNullOrEmpty(normalized)) normalized = "Assets/Resources/Serializations/JSON/ActionTimelines";
                if (jsonRootDirectory != normalized)
                {
                    jsonRootDirectory = normalized;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public string SoRootDirectory
        {
            get => string.IsNullOrEmpty(soRootDirectory) ? "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines" : soRootDirectory;
            set
            {
                string normalized = value?.Trim().Replace('\\', '/').TrimEnd('/');
                if (string.IsNullOrEmpty(normalized)) normalized = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";
                if (soRootDirectory != normalized)
                {
                    soRootDirectory = normalized;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public string SharedWorkspaceJsonPath
        {
            get => string.IsNullOrEmpty(sharedWorkspaceJsonPath) ? "Assets/Editor/Settings/Workspace/SharedWorkspaces.json" : sharedWorkspaceJsonPath;
            set
            {
                string normalized = value?.Trim().Replace('\\', '/');
                if (string.IsNullOrEmpty(normalized)) normalized = "Assets/Editor/Settings/Workspace/SharedWorkspaces.json";
                if (sharedWorkspaceJsonPath != normalized)
                {
                    sharedWorkspaceJsonPath = normalized;
                    SharedWorkspaceFileIO.CustomRelativePath = normalized;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>
        /// 根据角色物理子目录推导实际 JSON 完整目录：前缀 + FolderName
        /// </summary>
        public string GetWorkspaceJsonDirectory(ATWorkspaceDefinition ws)
        {
            if (ws == null || string.IsNullOrEmpty(ws.FolderName)) return JsonRootDirectory;
            string subFolder = ws.FolderName.Trim().Replace('\\', '/').Trim('/');
            return $"{JsonRootDirectory}/{subFolder}";
        }

        /// <summary>
        /// 根据角色物理子目录推导实际 SO 完整目录：前缀 + FolderName
        /// </summary>
        public string GetWorkspaceAssetDirectory(ATWorkspaceDefinition ws)
        {
            if (ws == null || string.IsNullOrEmpty(ws.FolderName)) return SoRootDirectory;
            string subFolder = ws.FolderName.Trim().Replace('\\', '/').Trim('/');
            return $"{SoRootDirectory}/{subFolder}";
        }

        private static ATEditorWorkspaceDatabase _instance;
        private static bool _isSyncing = false;

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

        [InitializeOnLoadMethod]
        private static void InitWatcher()
        {
            SharedWorkspaceWatcher.OnWorkspacesFileChanged -= OnSharedFileChanged;
            SharedWorkspaceWatcher.OnWorkspacesFileChanged += OnSharedFileChanged;
        }

        private static void OnSharedFileChanged()
        {
            if (_isSyncing) return;
            if (_instance != null)
            {
                _instance.SyncFromSharedJson(true);
            }
        }

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

            // 同步公共 JSON 数据
            db.EnsureDefaultCategories();
            db.SyncFromSharedJson(false);
            return db;
        }

        /// <summary>
        /// 从公共 SharedWorkspaces.json 同步角色定义，并保留本地已配置的 PreviewPrefab 等扩展参数
        /// </summary>
        public void SyncFromSharedJson(bool saveAsset = true)
        {
            if (_isSyncing) return;
            _isSyncing = true;

            try
            {
                SharedWorkspaceFileIO.CustomRelativePath = SharedWorkspaceJsonPath;
                var sharedData = SharedWorkspaceFileIO.Load(SharedWorkspaceJsonPath);
                if (sharedData == null || sharedData.Workspaces == null)
                {
                    return;
                }

                if (workspaces == null) workspaces = new List<ATWorkspaceDefinition>();

                // 暂存现有扩展数据（以 Id 索引）
                var existingMap = new Dictionary<string, ATWorkspaceDefinition>(StringComparer.OrdinalIgnoreCase);
                foreach (var ws in workspaces)
                {
                    if (!string.IsNullOrEmpty(ws.Id))
                    {
                        existingMap[ws.Id] = ws;
                    }
                }

                var updatedList = new List<ATWorkspaceDefinition>();
                bool isDirty = false;

                foreach (var sharedDef in sharedData.Workspaces)
                {
                    // 查找是否有匹配的现有定义
                    ATWorkspaceDefinition matched = null;
                    if (existingMap.TryGetValue(sharedDef.Id, out var direct))
                    {
                        matched = direct;
                    }

                    if (matched != null)
                    {
                        // 保留已配置的 Prefab 与 Transform，更新公共元数据
                        matched.Id = sharedDef.Id;
                        matched.Category = sharedDef.Category;
                        matched.DisplayName = sharedDef.DisplayName;
                        matched.FolderName = sharedDef.FolderName;
                        updatedList.Add(matched);
                    }
                    else
                    {
                        // 尝试自动查找匹配的 Prefab
                        GameObject defaultPrefab = TryFindDefaultPrefab(sharedDef.Id, sharedDef.FolderName);
                        var newWs = new ATWorkspaceDefinition(sharedDef, defaultPrefab);
                        updatedList.Add(newWs);
                        isDirty = true;
                    }
                }

                workspaces = updatedList;
                EnsureDefaultCategories();

                if (isDirty && saveAsset)
                {
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private GameObject TryFindDefaultPrefab(string id, string folderName)
        {
            string charName = Path.GetFileName(folderName?.Trim('/'));
            if (string.IsNullOrEmpty(charName)) return null;

            string[] candidatePaths = new[]
            {
                $"Assets/Resources/Prefab/Role/{charName}/Avatar_Female_Size02_{charName}.prefab",
                $"Assets/Resources/Prefab/Role/{charName}/{charName}.prefab",
                $"Assets/Resources/Prefab/Role/{charName}/Unagi.prefab",
                $"Assets/Resources/Prefab/Role/Anbi/Anbi.prefab",
                $"Assets/Resources/Prefab/Monster/Monster_{charName}.prefab",
                $"Assets/Resources/Prefab/Monster/{charName}.prefab"
            };

            foreach (var p in candidatePaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) return go;
            }
            return null;
        }

        /// <summary>
        /// 将当前工作区基础数据写回公共 SharedWorkspaces.json
        /// </summary>
        public bool SaveToSharedJson()
        {
            if (_isSyncing) return false;
            _isSyncing = true;

            try
            {
                var sharedData = new SharedWorkspaceData();
                sharedData.Categories = new List<string>(FixedCategories);

                int order = 10;
                foreach (var ws in workspaces)
                {
                    var sharedDef = ws.ToSharedDefinition();
                    sharedDef.Order = order;
                    sharedData.Workspaces.Add(sharedDef);
                    order += 10;
                }

                SharedWorkspaceFileIO.CustomRelativePath = SharedWorkspaceJsonPath;
                bool success = SharedWorkspaceFileIO.Save(sharedData, SharedWorkspaceJsonPath);
                if (success)
                {
                    SharedWorkspaceWatcher.NotifyChanged();
                }
                return success;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// 根据 Id 获取角色工作区
        /// </summary>
        public ATWorkspaceDefinition GetWorkspaceById(string id)
        {
            if (string.IsNullOrEmpty(id) || workspaces == null) return null;
            return workspaces.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 根据动作资产路径自动推导所属的角色工作区
        /// 兼容 Role/{Char} 与历史 Player/{Char} 物理路径
        /// </summary>
        public ATWorkspaceDefinition GetWorkspaceByAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || workspaces == null) return null;
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
                    // 1. 优先完全一致匹配 FolderName (如 "Role/Ellen")
                    var matched = workspaces.Find(w => string.Equals(w.FolderName?.Trim('/'), dir.Trim('/'), StringComparison.OrdinalIgnoreCase));
                    if (matched != null) return matched;

                    // 兼容历史 Player/ 路径匹配
                    if (dir.StartsWith("Player/", StringComparison.OrdinalIgnoreCase))
                    {
                        string modernDir = "Role/" + dir.Substring(7);
                        matched = workspaces.Find(w => string.Equals(w.FolderName?.Trim('/'), modernDir, StringComparison.OrdinalIgnoreCase));
                        if (matched != null) return matched;
                    }
                    else if (dir.StartsWith("Role/", StringComparison.OrdinalIgnoreCase))
                    {
                        string legacyDir = "Player/" + dir.Substring(5);
                        matched = workspaces.Find(w => string.Equals(w.FolderName?.Trim('/'), legacyDir, StringComparison.OrdinalIgnoreCase));
                        if (matched != null) return matched;
                    }

                    // 2. 尝试末尾角色名匹配（如 "Ellen"）
                    string charToken = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(charToken))
                    {
                        matched = workspaces.Find(w =>
                        {
                            string wsChar = Path.GetFileName(w.FolderName?.Trim('/'));
                            return string.Equals(wsChar, charToken, StringComparison.OrdinalIgnoreCase);
                        });
                        if (matched != null) return matched;
                    }
                }
            }

            // 3. 尝试直接在路径中包含 FolderName 或角色名
            foreach (var ws in workspaces)
            {
                if (!string.IsNullOrEmpty(ws.FolderName))
                {
                    string folder = ws.FolderName.Trim('/');
                    if (normalized.IndexOf("/" + folder + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return ws;
                    }

                    // 兼容历史 Player/ 包含
                    if (folder.StartsWith("Role/", StringComparison.OrdinalIgnoreCase))
                    {
                        string legacyFolder = "Player/" + folder.Substring(5);
                        if (normalized.IndexOf("/" + legacyFolder + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return ws;
                        }
                    }
                }
            }

            return null;
        }

        public void EnsureDefaultCategories()
        {
            if (categories == null) categories = new List<string>();
            categories.Clear();
            categories.AddRange(FixedCategories);
        }

        public List<string> GetCategories()
        {
            EnsureDefaultCategories();
            return new List<string>(categories);
        }

        public bool AddCategory(string name, out string errorMessage)
        {
            errorMessage = "工作区分类名称已固定为 Role, Monster, Common，不支持新增自定义分类。";
            return false;
        }

        public bool DeleteCategory(string name, out string errorMessage)
        {
            errorMessage = "预设分类为系统固定分类，不可删除！";
            return false;
        }

        public bool RenameCategory(string oldName, string newName, out string errorMessage)
        {
            errorMessage = "预设分类为系统固定分类，不可重命名！";
            return false;
        }

        public List<ATWorkspaceDefinition> GetWorkspacesByCategory(string category)
        {
            var list = new List<ATWorkspaceDefinition>();
            if (workspaces == null) return list;

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
        /// 添加新工作区，并同步写回公共 JSON
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

            SaveToSharedJson();
            return true;
        }

        /// <summary>
        /// 更新已有工作区，并同步写回公共 JSON
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

            SaveToSharedJson();
            return true;
        }

        /// <summary>
        /// 删除工作区，并同步写回公共 JSON
        /// </summary>
        public bool RemoveWorkspace(string id)
        {
            int count = workspaces.RemoveAll(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            if (count > 0)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();

                SaveToSharedJson();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 初始化默认工作区（包含项目现有的艾莲、安比、提尔锋、通用）
        /// 分类统一为 Role, Monster, Common
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

            TryAddDefault("Role_Ellen", "Role", "艾莲 (Ellen)", "Role/Ellen", "Assets/Resources/Prefab/Role/Ellen/Avatar_Female_Size02_Ellen.prefab");
            TryAddDefault("Role_Anby", "Role", "安比 (Anby)", "Role/Anby", "Assets/Resources/Prefab/Role/Anbi/Anbi.prefab");
            TryAddDefault("Role_Unagi", "Role", "星见雅 (Unagi)", "Role/Unagi", "Assets/Resources/Prefab/Role/Unagi/Unagi.prefab");
            TryAddDefault("Role_QingYi", "Role", "青衣 (QingYi)", "Role/QingYi", "Assets/Resources/Prefab/Role/QingYi/QingYi.prefab");
            TryAddDefault("Monster_TyrfingInfested", "Monster", "侵蚀提尔锋 (TyrfingInfested)", "Monster/TyrfingInfested", "Assets/Resources/Prefab/Monster/Monster_TyrfingInfested.prefab");
            TryAddDefault("Common_Shared", "Common", "通用模板 (Common)", "Common", null);
        }
    }
}
