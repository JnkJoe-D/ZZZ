using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.Editor.Workspace
{
    /// <summary>
    /// 资产类型枚举
    /// </summary>
    public enum WorkbenchAssetKind
    {
        Unknown = 0,
        TimelineJson = 1,
        TimelineSo = 2,
        ActionConfigAsset = 3
    }

    /// <summary>
    /// 全局统一路径解析服务 (WorkbenchPathResolver)
    /// <para>统一管理 ActionConfig、Timeline JSON 与 Timeline SO 的根目录与角色子目录，</para>
    /// <para>提供防路径穿越安全校验、双轨资产对偶推导（消灭暴力字符串替换）与相对/绝对路径转换。</para>
    /// </summary>
    public static class WorkbenchPathResolver
    {
        // 跨运行时兼容斜杠字符数组（避免 .NET Standard 2.1 TrimEnd(char) 在旧 CLR 反射调用时的缺失错误）
        private static readonly char[] SlashChars = new[] { '/', '\\' };

        // 默认根目录常量
        public const string DEFAULT_CHARACTER_CONFIG_ROOT = "Assets/Resources/Serializations/ScriptableObjects/CharacterConfig";
        public const string DEFAULT_TIMELINE_JSON_ROOT = "Assets/Resources/Serializations/JSON/ActionTimelines";
        public const string DEFAULT_TIMELINE_SO_ROOT = "Assets/Resources/Serializations/ScriptableObjects/ActionTimelines";

        /// <summary>
        /// ActionConfig 默认根目录别名（等价于 DEFAULT_CHARACTER_CONFIG_ROOT）
        /// </summary>
        public const string DefaultActionConfigRoot = DEFAULT_CHARACTER_CONFIG_ROOT;

        // 外部动态重写配置提供者（如测试或工作台专属配置）
        private static Func<string> _customCharConfigRootProvider;
        private static Func<string> _customTimelineJsonRootProvider;
        private static Func<string> _customTimelineSoRootProvider;

        /// <summary>
        /// 注册自定义根目录获取委托（优先级最高，常用于单元测试或特定独立运行环境）
        /// </summary>
        public static void SetCustomRootProviders(
            Func<string> charConfigProvider = null,
            Func<string> jsonRootProvider = null,
            Func<string> soRootProvider = null)
        {
            _customCharConfigRootProvider = charConfigProvider;
            _customTimelineJsonRootProvider = jsonRootProvider;
            _customTimelineSoRootProvider = soRootProvider;
        }

        /// <summary>
        /// 重置所有自定义根目录提供者
        /// </summary>
        public static void ResetCustomRootProviders()
        {
            _customCharConfigRootProvider = null;
            _customTimelineJsonRootProvider = null;
            _customTimelineSoRootProvider = null;
        }

        #region 根目录属性 (Root Properties)

        /// <summary>
        /// ActionConfig 根目录（默认: Assets/Resources/Serializations/ScriptableObjects/CharacterConfig）
        /// </summary>
        public static string CharacterConfigRoot
        {
            get
            {
                if (_customCharConfigRootProvider != null)
                {
                    string custom = _customCharConfigRootProvider();
                    if (!string.IsNullOrEmpty(custom)) return NormalizePath(custom);
                }

                return NormalizePath(DEFAULT_CHARACTER_CONFIG_ROOT);
            }
        }

        /// <summary>
        /// ActionConfig 根目录别名（等价于 CharacterConfigRoot）
        /// </summary>
        public static string ActionConfigRoot => CharacterConfigRoot;

        /// <summary>
        /// Timeline JSON 根目录（默认: Assets/Resources/Serializations/JSON/ActionTimelines）
        /// </summary>
        public static string TimelineJsonRoot
        {
            get
            {
                if (_customTimelineJsonRootProvider != null)
                {
                    string custom = _customTimelineJsonRootProvider();
                    if (!string.IsNullOrEmpty(custom)) return NormalizePath(custom);
                }

                return NormalizePath(DEFAULT_TIMELINE_JSON_ROOT);
            }
        }

        /// <summary>
        /// Timeline SO 根目录（默认: Assets/Resources/Serializations/ScriptableObjects/ActionTimelines）
        /// </summary>
        public static string TimelineSoRoot
        {
            get
            {
                if (_customTimelineSoRootProvider != null)
                {
                    string custom = _customTimelineSoRootProvider();
                    if (!string.IsNullOrEmpty(custom)) return NormalizePath(custom);
                }

                return NormalizePath(DEFAULT_TIMELINE_SO_ROOT);
            }
        }

        #endregion

        #region 路径规范化与安全校验 (Normalization & Safety)

        /// <summary>
        /// 规范化路径：统一斜杠为 '/'，去除首尾多余空格与尾部斜杠，压缩连续双斜杠
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string normalized = path.Trim().Replace('\\', '/');

            // 替换重复的双斜杠
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.TrimEnd(SlashChars);
        }

        /// <summary>
        /// 检查路径是否存在跨越根目录的非法路径穿越字符（如 "../"）
        /// </summary>
        public static bool IsPathTraversing(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string normalized = path.Replace('\\', '/');
            return normalized.Contains("../") || normalized.Contains("/..") || normalized == "..";
        }

        /// <summary>
        /// 校验路径是否合法且安全（不能包含路径穿越，不能为空）
        /// </summary>
        public static bool ValidateSafePath(string path, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "路径不能为空或纯空格。";
                return false;
            }

            if (IsPathTraversing(path))
            {
                errorMessage = $"检测到非法路径穿越字符 (..)，拒绝访问: {path}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判定指定路径是否以 Unity 的 Assets/ 开头
        /// </summary>
        public static bool IsProjectAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string normalized = NormalizePath(path);
            return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将项目内相对路径安全转换为系统绝对物理路径
        /// </summary>
        public static string ToAbsoluteSystemPath(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) return string.Empty;

            string normalized = NormalizePath(projectRelativePath);
            if (Path.IsPathRooted(normalized))
            {
                return normalized;
            }

            string projectRoot = NormalizePath(Directory.GetCurrentDirectory());
            return NormalizePath(Path.Combine(projectRoot, normalized));
        }

        /// <summary>
        /// 将系统绝对物理路径或任意路径安全转换为 Unity 项目相对路径 (Assets/...)
        /// </summary>
        public static string ToProjectAssetPath(string fullOrRelativePath)
        {
            if (string.IsNullOrEmpty(fullOrRelativePath)) return string.Empty;

            string normalized = NormalizePath(fullOrRelativePath);
            string projectRoot = NormalizePath(Directory.GetCurrentDirectory()) + "/";

            if (normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(projectRoot.Length);
            }

            return NormalizePath(normalized);
        }

        #endregion

        #region 资产类别识别 (Asset Kind Detection)

        /// <summary>
        /// 识别指定路径对应的资产类型
        /// </summary>
        public static WorkbenchAssetKind DetectAssetKind(string path)
        {
            if (string.IsNullOrEmpty(path)) return WorkbenchAssetKind.Unknown;

            string normalized = NormalizePath(path);
            string ext = Path.GetExtension(normalized);

            if (string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
            {
                if (normalized.IndexOf("ActionTimelines", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalized.IndexOf("/JSON/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return WorkbenchAssetKind.TimelineJson;
                }
            }
            else if (string.Equals(ext, ".asset", StringComparison.OrdinalIgnoreCase))
            {
                if (normalized.IndexOf("ActionTimelines", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return WorkbenchAssetKind.TimelineSo;
                }
                if (normalized.IndexOf("CharacterConfig", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return WorkbenchAssetKind.ActionConfigAsset;
                }
            }

            return WorkbenchAssetKind.Unknown;
        }

        #endregion

        #region 针对角色的目录解析 (Role Workspace Resolvers)

        /// <summary>
        /// 根据角色物理子目录解析 Timeline JSON 目标物理目录：TimelineJsonRoot + "/" + folderName
        /// </summary>
        public static string ResolveTimelineJsonDirectory(string folderName, string rootOverride = null)
        {
            string root = !string.IsNullOrEmpty(rootOverride) ? NormalizePath(rootOverride) : TimelineJsonRoot;
            if (string.IsNullOrEmpty(folderName)) return root;

            string cleanFolder = NormalizePath(folderName).TrimStart(SlashChars);
            return NormalizePath($"{root}/{cleanFolder}");
        }

        /// <summary>
        /// 根据角色公共工作区定义解析 Timeline JSON 目标物理目录
        /// </summary>
        public static string ResolveTimelineJsonDirectory(SharedWorkspaceDefinition ws, string rootOverride = null)
        {
            if (ws == null) return ResolveTimelineJsonDirectory(string.Empty, rootOverride);
            return ResolveTimelineJsonDirectory(ws.FolderName, rootOverride);
        }

        /// <summary>
        /// 根据角色物理子目录解析 Timeline SO 目标物理目录：TimelineSoRoot + "/" + folderName
        /// </summary>
        public static string ResolveTimelineSoDirectory(string folderName, string rootOverride = null)
        {
            string root = !string.IsNullOrEmpty(rootOverride) ? NormalizePath(rootOverride) : TimelineSoRoot;
            if (string.IsNullOrEmpty(folderName)) return root;

            string cleanFolder = NormalizePath(folderName).TrimStart(SlashChars);
            return NormalizePath($"{root}/{cleanFolder}");
        }

        /// <summary>
        /// 根据角色公共工作区定义解析 Timeline SO 目标物理目录
        /// </summary>
        public static string ResolveTimelineSoDirectory(SharedWorkspaceDefinition ws, string rootOverride = null)
        {
            if (ws == null) return ResolveTimelineSoDirectory(string.Empty, rootOverride);
            return ResolveTimelineSoDirectory(ws.FolderName, rootOverride);
        }

        /// <summary>
        /// 根据角色分类与标识解析 ActionConfig 目标标准目录：CharacterConfigRoot + "/{Category}/{RoleToken}/Action"
        /// </summary>
        public static string ResolveActionConfigDirectory(string category, string roleToken, string rootOverride = null)
        {
            string root = !string.IsNullOrEmpty(rootOverride) ? NormalizePath(rootOverride) : CharacterConfigRoot;
            string cleanCat = !string.IsNullOrEmpty(category) ? category.Trim() : "Role";
            string cleanToken = !string.IsNullOrEmpty(roleToken) ? roleToken.Trim() : "Common";

            return NormalizePath($"{root}/{cleanCat}/{cleanToken}/Action");
        }

        /// <summary>
        /// 根据角色公共工作区定义解析 ActionConfig 标准目录
        /// </summary>
        public static string ResolveActionConfigDirectory(SharedWorkspaceDefinition ws, string rootOverride = null)
        {
            if (ws == null) return NormalizePath(CharacterConfigRoot);

            string roleToken = !string.IsNullOrEmpty(ws.FolderName) 
                ? Path.GetFileName(NormalizePath(ws.FolderName)) 
                : ws.Id;

            return ResolveActionConfigDirectory(ws.Category, roleToken, rootOverride);
        }

        /// <summary>
        /// 根据角色公共工作区定义获取 ActionConfig 候选探测目录列表
        /// </summary>
        public static List<string> ResolveActionConfigCandidateDirectories(SharedWorkspaceDefinition ws, string rootOverride = null)
        {
            if (ws == null) return ResolveActionConfigCandidateDirectories(string.Empty, null, rootOverride);
            return ResolveActionConfigCandidateDirectories(ws.FolderName, ws.Category, rootOverride);
        }

        /// <summary>
        /// 获取针对该角色的 ActionConfig 候选探测目录列表（兼顾标准目录与无 Action 子目录的历史兼容结构）
        /// </summary>
        public static List<string> ResolveActionConfigCandidateDirectories(string folderName, string category = null, string rootOverride = null)
        {
            var candidates = new List<string>();
            string root = !string.IsNullOrEmpty(rootOverride) ? NormalizePath(rootOverride) : CharacterConfigRoot;

            if (string.IsNullOrEmpty(folderName))
            {
                candidates.Add(root);
                return candidates;
            }

            string cleanFolder = NormalizePath(folderName).Trim(SlashChars);
            string roleToken = Path.GetFileName(cleanFolder);

            // 1. 若提供了 Category，优先加入标准路径组合
            if (!string.IsNullOrEmpty(category))
            {
                candidates.Add(NormalizePath($"{root}/{category}/{roleToken}/Action"));
                candidates.Add(NormalizePath($"{root}/{category}/{roleToken}"));
            }

            // 2. 若 folderName 自身形如 "Role/Ellen" 或 "Monster/Tyrfing"
            candidates.Add(NormalizePath($"{root}/{cleanFolder}/Action"));
            candidates.Add(NormalizePath($"{root}/{cleanFolder}"));

            // 去重
            var uniqueList = new List<string>();
            foreach (var c in candidates)
            {
                if (!uniqueList.Contains(c))
                {
                    uniqueList.Add(c);
                }
            }

            return uniqueList;
        }

        #endregion

        #region 对偶资产推导 (Counterpart Resolution - 彻底替代脆弱字符串替换)

        /// <summary>
        /// 基于已知的 Timeline 资产路径（无论是 JSON 还是 SO），精准解析出对应的对偶资产路径。
        /// <para>例如输入: Assets/.../JSON/ActionTimelines/Role/Ellen/Atk_01.json，目标: TimelineSo</para>
        /// <para>输出: Assets/.../ScriptableObjects/ActionTimelines/Role/Ellen/Atk_01.asset</para>
        /// </summary>
        /// <param name="sourceAssetOrJsonPath">已知的源资产或文件路径</param>
        /// <param name="targetKind">期望推导的目标资产类别 (TimelineJson 或 TimelineSo)</param>
        /// <param name="resolvedPath">解析出的最终完整相对路径</param>
        /// <param name="workspaceFolderName">工作区物理子目录（可选，若提供则优先精准定位，避免反向推导误差）</param>
        /// <returns>推导是否成功</returns>
        public static bool TryResolveCounterpartTimelinePath(
            string sourceAssetOrJsonPath,
            WorkbenchAssetKind targetKind,
            out string resolvedPath,
            string workspaceFolderName = null)
        {
            resolvedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceAssetOrJsonPath)) return false;

            string normalizedSource = NormalizePath(sourceAssetOrJsonPath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(normalizedSource);
            string targetExt = targetKind == WorkbenchAssetKind.TimelineJson ? ".json" : ".asset";
            string targetRoot = targetKind == WorkbenchAssetKind.TimelineJson ? TimelineJsonRoot : TimelineSoRoot;

            // 策略 1: 若显式提供了角色子目录 (workspaceFolderName)，直接基于目标根目录组装
            if (!string.IsNullOrEmpty(workspaceFolderName))
            {
                string cleanSubDir = NormalizePath(workspaceFolderName).Trim(SlashChars);
                resolvedPath = NormalizePath($"{targetRoot}/{cleanSubDir}/{fileNameWithoutExt}{targetExt}");
                return true;
            }

            // 策略 2: 提取源路径相对于其根目录的子相对路径
            string sourceRoot = targetKind == WorkbenchAssetKind.TimelineJson ? TimelineSoRoot : TimelineJsonRoot;
            string relativeSubPath = null;

            if (normalizedSource.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
            {
                string sub = normalizedSource.Substring(sourceRoot.Length).TrimStart(SlashChars);
                relativeSubPath = Path.GetDirectoryName(sub)?.Replace('\\', '/');
            }

            // 若源路径不在默认源根目录下，尝试按关键标记提取子目录 (如 ActionTimelines/...)
            if (string.IsNullOrEmpty(relativeSubPath))
            {
                const string marker = "ActionTimelines/";
                int markerIdx = normalizedSource.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIdx >= 0)
                {
                    string subAfterMarker = normalizedSource.Substring(markerIdx + marker.Length);
                    relativeSubPath = Path.GetDirectoryName(subAfterMarker)?.Replace('\\', '/');
                }
            }

            // 策略 3: 基于提取出的 relativeSubPath 重新挂载到目标根目录
            if (!string.IsNullOrEmpty(relativeSubPath))
            {
                resolvedPath = NormalizePath($"{targetRoot}/{relativeSubPath}/{fileNameWithoutExt}{targetExt}");
                return true;
            }

            // 策略 4: 兜底方案 —— 若完全无法提取子目录，则直接放在目标根目录下
            resolvedPath = NormalizePath($"{targetRoot}/{fileNameWithoutExt}{targetExt}");
            return true;
        }

        #endregion

        #region 目录健康检测与安全操作 (Health & Safety Operations)

        /// <summary>
        /// 确保指定资产目录或系统绝对目录存在，若不存在则安全创建
        /// </summary>
        public static bool EnsureDirectoryExists(string assetOrSystemPath)
        {
            if (string.IsNullOrEmpty(assetOrSystemPath)) return false;

            string absPath = ToAbsoluteSystemPath(assetOrSystemPath);
            if (!Directory.Exists(absPath))
            {
                try
                {
                    Directory.CreateDirectory(absPath);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorkbenchPathResolver] 创建目录失败: '{absPath}'. 异常: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查目录健康状态（是否存在、包含文件总数）
        /// </summary>
        public static bool CheckDirectoryHealth(string assetOrSystemPath, out bool exists, out int fileCount)
        {
            exists = false;
            fileCount = 0;
            if (string.IsNullOrEmpty(assetOrSystemPath)) return false;

            string absPath = ToAbsoluteSystemPath(assetOrSystemPath);
            exists = Directory.Exists(absPath);
            if (exists)
            {
                try
                {
                    fileCount = Directory.GetFiles(absPath, "*.*", SearchOption.TopDirectoryOnly).Length;
                }
                catch
                {
                    fileCount = 0;
                }
            }

            return exists;
        }

        #endregion
    }
}
