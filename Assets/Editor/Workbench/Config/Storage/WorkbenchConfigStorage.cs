using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 配置持久化存储管理服务
    /// 负责读写 WorkbenchConfig.json 与 CharacterOverrides/{WorkspaceId}.json，
    /// 提供安全原子写入、自动目录创建与损坏备份机制。
    /// </summary>
    public static class WorkbenchConfigStorage
    {
        public const string DefaultSettingsRoot = "Assets/Editor/Settings/Workbench";
        public const string ConfigFileName = "WorkbenchConfig.json";
        public const string OverridesFolderName = "CharacterOverrides";

        public static string ConfigFilePath => Path.Combine(DefaultSettingsRoot, ConfigFileName).Replace('\\', '/');
        public static string OverridesDirectoryPath => Path.Combine(DefaultSettingsRoot, OverridesFolderName).Replace('\\', '/');

        public static event Action OnWorkspaceConfigChanged;
        public static event Action<string> OnCharacterOverrideChanged;

        /// <summary>
        /// 加载工作台全局工作区配置。若文件不存在则返回默认配置。
        /// </summary>
        public static WorkbenchWorkspaceConfig LoadWorkspaceConfig()
        {
            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                return WorkbenchWorkspaceConfig.CreateDefault();
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var config = JsonUtility.FromJson<WorkbenchWorkspaceConfig>(json);
                if (config == null)
                {
                    Debug.LogWarning($"[WorkbenchConfigStorage] 解析工作区配置为空，回退默认配置: {path}");
                    return WorkbenchWorkspaceConfig.CreateDefault();
                }
                config.Normalize();
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorkbenchConfigStorage] 读取工作区配置异常: {ex.Message}，保留损坏文件，使用默认配置。");
                return WorkbenchWorkspaceConfig.CreateDefault();
            }
        }

        /// <summary>
        /// 保存工作台全局工作区配置（采用安全原子写入）。
        /// </summary>
        public static bool SaveWorkspaceConfig(WorkbenchWorkspaceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Normalize();

            EnsureDirectoryExists(DefaultSettingsRoot);
            string json = JsonUtility.ToJson(config, true);
            bool success = SafeWriteFile(ConfigFilePath, json);
            if (success)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                OnWorkspaceConfigChanged?.Invoke();
            }
            return success;
        }

        /// <summary>
        /// 获取指定角色的覆盖配置文件路径
        /// </summary>
        public static string GetCharacterOverrideFilePath(string workspaceId)
        {
            if (string.IsNullOrEmpty(workspaceId)) return string.Empty;
            string safeId = workspaceId.Replace('/', '_').Replace('\\', '_');
            return Path.Combine(OverridesDirectoryPath, $"{safeId}.json").Replace('\\', '/');
        }

        /// <summary>
        /// 加载指定角色的覆盖配置。若不存在则返回未覆盖的空白配置。
        /// </summary>
        public static CharacterOverrideConfig LoadCharacterOverride(string workspaceId)
        {
            if (string.IsNullOrEmpty(workspaceId))
            {
                return new CharacterOverrideConfig();
            }

            string path = GetCharacterOverrideFilePath(workspaceId);
            if (!File.Exists(path))
            {
                return new CharacterOverrideConfig { WorkspaceId = workspaceId };
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var config = JsonUtility.FromJson<CharacterOverrideConfig>(json);
                if (config == null)
                {
                    return new CharacterOverrideConfig { WorkspaceId = workspaceId };
                }
                config.Normalize();
                config.WorkspaceId = workspaceId;
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorkbenchConfigStorage] 读取角色覆盖配置异常 ({workspaceId}): {ex.Message}");
                return new CharacterOverrideConfig { WorkspaceId = workspaceId };
            }
        }

        /// <summary>
        /// 保存指定角色的覆盖配置。
        /// </summary>
        public static bool SaveCharacterOverride(CharacterOverrideConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.WorkspaceId))
            {
                Debug.LogError("[WorkbenchConfigStorage] 无法保存未指定 WorkspaceId 的角色覆盖配置！");
                return false;
            }

            config.Normalize();
            EnsureDirectoryExists(OverridesDirectoryPath);

            string path = GetCharacterOverrideFilePath(config.WorkspaceId);
            string json = JsonUtility.ToJson(config, true);
            bool success = SafeWriteFile(path, json);
            if (success)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                OnCharacterOverrideChanged?.Invoke(config.WorkspaceId);
            }
            return success;
        }

        /// <summary>
        /// 删除指定角色的覆盖配置文件（恢复全量继承）
        /// </summary>
        public static bool DeleteCharacterOverride(string workspaceId)
        {
            if (string.IsNullOrEmpty(workspaceId)) return false;
            string path = GetCharacterOverrideFilePath(workspaceId);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    string meta = path + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                    AssetDatabase.Refresh();
                    OnCharacterOverrideChanged?.Invoke(workspaceId);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorkbenchConfigStorage] 删除角色覆盖配置失败: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 加载所有已配置的角色覆盖映射字典
        /// </summary>
        public static IReadOnlyDictionary<string, CharacterOverrideConfig> LoadAllCharacterOverrides()
        {
            var dict = new Dictionary<string, CharacterOverrideConfig>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(OverridesDirectoryPath))
            {
                return dict;
            }

            string[] files = Directory.GetFiles(OverridesDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    var cfg = JsonUtility.FromJson<CharacterOverrideConfig>(json);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.WorkspaceId))
                    {
                        cfg.Normalize();
                        dict[cfg.WorkspaceId] = cfg;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorkbenchConfigStorage] 解析覆盖文件失败 ({file}): {ex.Message}");
                }
            }

            return dict;
        }

        private static void EnsureDirectoryExists(string unityPath)
        {
            if (string.IsNullOrEmpty(unityPath)) return;
            if (!Directory.Exists(unityPath))
            {
                Directory.CreateDirectory(unityPath);
            }
        }

        private static bool SafeWriteFile(string filePath, string content)
        {
            string tempPath = filePath + ".tmp";
            try
            {
                File.WriteAllText(tempPath, content, Encoding.UTF8);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempPath, filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorkbenchConfigStorage] 写入文件失败 ({filePath}): {ex.Message}");
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                return false;
            }
        }
    }
}
