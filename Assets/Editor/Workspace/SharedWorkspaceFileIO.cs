using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workspace
{
    /// <summary>
    /// 公共角色工作区 JSON 序列化持久化服务 (Shared Workspace File I/O)
    /// 负责读写公共工作区配置文件（默认：Assets/Editor/Settings/Workspace/SharedWorkspaces.json，可自定义配置路径）
    /// </summary>
    public static class SharedWorkspaceFileIO
    {
        public const string DefaultRelativePath = "Assets/Editor/Settings/Workspace/SharedWorkspaces.json";

        /// <summary>
        /// 自定义相对路径（若设置则优先于默认路径）
        /// </summary>
        public static string CustomRelativePath { get; set; } = null;

        public static string EffectiveRelativePath => !string.IsNullOrEmpty(CustomRelativePath) ? CustomRelativePath : DefaultRelativePath;

        public static string GetFullPath(string relativeOrFullPath = null)
        {
            string path = !string.IsNullOrEmpty(relativeOrFullPath) ? relativeOrFullPath : EffectiveRelativePath;
            if (Path.IsPathRooted(path)) return path.Replace('\\', '/');
            string projectPath = Directory.GetCurrentDirectory();
            return Path.Combine(projectPath, path).Replace('\\', '/');
        }

        public static string FullFilePath => GetFullPath();

        /// <summary>
        /// 从磁盘加载 SharedWorkspaces.json
        /// 若文件不存在，则创建并落盘初始化的标准工作区配置
        /// </summary>
        public static SharedWorkspaceData Load(string customPath = null)
        {
            string fullPath = GetFullPath(customPath);
            string displayPath = !string.IsNullOrEmpty(customPath) ? customPath : EffectiveRelativePath;

            if (!File.Exists(fullPath))
            {
                var initialData = CreateDefaultData();
                Save(initialData, customPath);
                return initialData;
            }

            try
            {
                string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning($"[SharedWorkspace] {displayPath} 为空，自动恢复默认数据。");
                    var fallback = CreateDefaultData();
                    Save(fallback, customPath);
                    return fallback;
                }

                var data = JsonUtility.FromJson<SharedWorkspaceData>(json);
                if (data == null)
                {
                    Debug.LogWarning($"[SharedWorkspace] 解析 {displayPath} 失败，自动恢复默认数据。");
                    data = CreateDefaultData();
                    Save(data, customPath);
                }
                else
                {
                    data.EnsureDefaults();
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SharedWorkspace] 读取 {displayPath} 异常: {ex.Message}");
                var fallback = CreateDefaultData();
                return fallback;
            }
        }

        /// <summary>
        /// 将工作区数据安全保存回 SharedWorkspaces.json
        /// </summary>
        public static bool Save(SharedWorkspaceData data, string customPath = null)
        {
            if (data == null) return false;

            try
            {
                data.EnsureDefaults();
                string fullPath = GetFullPath(customPath);
                string targetRelPath = !string.IsNullOrEmpty(customPath) ? customPath : EffectiveRelativePath;
                string dir = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);

                // 通知 Unity 资产数据库更新
                if (!Path.IsPathRooted(targetRelPath))
                {
                    AssetDatabase.ImportAsset(targetRelPath, ImportAssetOptions.ForceUpdate);
                }
                return true;
            }
            catch (Exception ex)
            {
                string displayPath = !string.IsNullOrEmpty(customPath) ? customPath : EffectiveRelativePath;
                Debug.LogError($"[SharedWorkspace] 保存 {displayPath} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建统一规范命名的默认工作区数据
        /// 分类固定为：Role, Monster, Common
        /// 命名示例：Role_Ellen, Monster_TyrfingInfested
        /// </summary>
        public static SharedWorkspaceData CreateDefaultData()
        {
            var data = new SharedWorkspaceData();

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Role_Ellen",
                category: SharedWorkspaceDefinition.CategoryRole,
                displayName: "艾莲 (Ellen)",
                folderName: "Role/Ellen",
                order: 10,
                description: "初始角色 - 艾莲"
            ));

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Role_Anby",
                category: SharedWorkspaceDefinition.CategoryRole,
                displayName: "安比 (Anby)",
                folderName: "Role/Anby",
                order: 20,
                description: "初始角色 - 安比"
            ));

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Role_Unagi",
                category: SharedWorkspaceDefinition.CategoryRole,
                displayName: "星见雅 (Unagi)",
                folderName: "Role/Unagi",
                order: 30,
                description: "初始角色 - 星见雅"
            ));

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Role_QingYi",
                category: SharedWorkspaceDefinition.CategoryRole,
                displayName: "青衣 (QingYi)",
                folderName: "Role/QingYi",
                order: 40,
                description: "初始角色 - 青衣"
            ));

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Monster_TyrfingInfested",
                category: SharedWorkspaceDefinition.CategoryMonster,
                displayName: "侵蚀提尔锋 (TyrfingInfested)",
                folderName: "Monster/TyrfingInfested",
                order: 100,
                description: "精英怪 - 侵蚀提尔锋"
            ));

            data.Workspaces.Add(new SharedWorkspaceDefinition(
                id: "Common_Shared",
                category: SharedWorkspaceDefinition.CategoryCommon,
                displayName: "通用模板 (Common)",
                folderName: "Common",
                order: 999,
                description: "通用基础与共享模板"
            ));

            return data;
        }
    }
}
