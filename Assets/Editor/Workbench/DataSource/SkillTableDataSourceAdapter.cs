using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 配表动作/技能条目数据模型
    /// </summary>
    [Serializable]
    public class SkillTableItem
    {
        public int Id;
        public string Name;
        public string Desc;
        public int InterruptLevel;
        public int ResilienceBonus;

        public int SkillId => Id;
        public string SkillName => Name;

        /// <summary>
        /// 是否在工程中已存在对应同名的 ActionConfigAsset
        /// </summary>
        public bool IsActionGenerated;

        /// <summary>
        /// 是否属于模糊/ID 匹配关联（而非严格规范命名全等）
        /// </summary>
        public bool IsFuzzyMatched;

        /// <summary>
        /// 匹配到的已存在动作资产对象
        /// </summary>
        public ActionAssetIndexItem MatchedAction;
    }

    /// <summary>
    /// 全流程工作台 - 技能配表数据源适配器 (Skill Table Data Source Adapter)
    /// 负责读取指定的动作/技能配表 JSON 文件（支持分表与角色覆盖），
    /// 并与当前角色的动作资产进行关联诊断。
    /// </summary>
    public static class SkillTableDataSourceAdapter
    {
        private class CachedSkillTable
        {
            public DateTime LastWriteTimeUtc;
            public List<SkillTableItem> Items;
        }

        private static readonly Dictionary<string, CachedSkillTable> _cache = new Dictionary<string, CachedSkillTable>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 加载指定路径的技能表条目（支持磁盘文件变更自动感知与热重载）
        /// </summary>
        public static List<SkillTableItem> LoadSkillTable(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                relativePath = "Assets/Configs/zzz_tbskill.json";
            }

            string cleanPath = relativePath.Trim().Replace('\\', '/');

            if (!File.Exists(cleanPath))
            {
                var empty = new List<SkillTableItem>();
                _cache[cleanPath] = new CachedSkillTable { LastWriteTimeUtc = DateTime.MinValue, Items = empty };
                return empty;
            }

            DateTime currentWriteTime = File.GetLastWriteTimeUtc(cleanPath);

            // 检查缓存且比对磁盘最后修改时间戳
            if (_cache.TryGetValue(cleanPath, out var cached) && cached.LastWriteTimeUtc == currentWriteTime)
            {
                return cached.Items;
            }

            var items = new List<SkillTableItem>();

            try
            {
                string json = File.ReadAllText(cleanPath, Encoding.UTF8);
                JSONNode root = JSONNode.Parse(json);
                if (root != null && root.IsArray)
                {
                    var array = root.AsArray;
                    for (int i = 0; i < array.Count; i++)
                    {
                        var node = array[i];
                        var item = new SkillTableItem
                        {
                            Id = node["id"].AsInt,
                            Name = node["name"]?.Value ?? string.Empty,
                            Desc = node["desc"]?.Value ?? string.Empty,
                            InterruptLevel = node["resilience"]?["interrupt_level"]?.AsInt ?? 0,
                            ResilienceBonus = node["resilience"]?["resilience_bonus"]?.AsInt ?? 0
                        };
                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SkillTableDataSourceAdapter] 解析技能表 JSON 异常 ({cleanPath}): {ex.Message}");
            }

            _cache[cleanPath] = new CachedSkillTable
            {
                LastWriteTimeUtc = currentWriteTime,
                Items = items
            };

            return items;
        }

        /// <summary>
        /// 获取针对当前角色上下文诊断后的技能配表条目列表
        /// </summary>
        public static List<SkillTableItem> GetDiagnosedSkillsForCharacter(WorkbenchCharacterContext context, bool showAllInTable = false)
        {
            if (context == null) return new List<SkillTableItem>();

            string tablePath = context.EffectiveConfig.SkillTablePath;
            var allItems = LoadSkillTable(tablePath);

            // 建立当前已有的 Action 映射（小写去空格匹配）
            var existingActions = new Dictionary<string, ActionAssetIndexItem>(StringComparer.OrdinalIgnoreCase);
            if (context.Actions != null)
            {
                foreach (var action in context.Actions)
                {
                    if (!string.IsNullOrEmpty(action.ActionName) && !existingActions.ContainsKey(action.ActionName))
                    {
                        existingActions[action.ActionName] = action;
                    }
                }
            }

            string roleFolder = context.Workspace.FolderName ?? string.Empty;
            string roleName = GetShortRoleName(roleFolder);

            var result = new List<SkillTableItem>();

            foreach (var item in allItems)
            {
                bool matchesRole = IsItemMatchCharacter(item, context.Workspace.Category, roleName);

                if (!showAllInTable && !matchesRole)
                {
                    continue;
                }

                // 诊断是否已生成对应 Action（支持 RoleName_Action_ActionName 与 RoleName_ActionName 对齐）
                bool isGenerated = false;
                bool isFuzzy = false;
                ActionAssetIndexItem matched = null;

                if (!string.IsNullOrEmpty(item.Name))
                {
                    if (existingActions.TryGetValue(item.Name, out matched))
                    {
                        isGenerated = true;
                    }
                    else
                    {
                        // 1. 智能对齐推导（支持中缀 _Action_ 与 _Action_Debuff_ 自动对齐）
                        foreach (var kvp in existingActions)
                        {
                            if (IsSkillMatchAction(item.Name, kvp.Key, roleName))
                            {
                                matched = kvp.Value;
                                isGenerated = true;
                                break;
                            }
                        }

                        // 2. 多维置信度兜底推导 (ID 相等或修饰词轻微差异的高置信度匹配)
                        if (!isGenerated && context.Actions != null)
                        {
                            ActionAssetIndexItem bestCandidate = null;
                            float bestScore = 0f;

                            foreach (var act in context.Actions)
                            {
                                float score = ActionMappingDiagnosticService.CalculateMatchScore(item, act, roleName);
                                if (score > 300f && score > bestScore)
                                {
                                    bestScore = score;
                                    bestCandidate = act;
                                }
                            }

                            if (bestCandidate != null)
                            {
                                matched = bestCandidate;
                                isGenerated = true;
                                isFuzzy = true;
                            }
                        }
                    }
                }

                var diagnosedItem = new SkillTableItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Desc = item.Desc,
                    InterruptLevel = item.InterruptLevel,
                    ResilienceBonus = item.ResilienceBonus,
                    IsActionGenerated = isGenerated,
                    IsFuzzyMatched = isFuzzy,
                    MatchedAction = matched
                };

                result.Add(diagnosedItem);
            }

            return result;
        }

        /// <summary>
        /// 判断配表技能名称与工程中的 Action 资产名称是否对应
        /// 支持：
        /// 1. 完全相同
        /// 2. 插入或去除中缀 "_Action_"：TyrfingInfested_Attack_01 <==> TyrfingInfested_Action_Attack_01
        /// 3. 包含 Debuff 扩展前缀：TyrfingInfested_Stun_Start <==> TyrfingInfested_Action_Debuff_Stun_Start
        /// 4. 提取短动作标识一致
        /// </summary>
        public static bool IsSkillMatchAction(string skillName, string actionName, string roleName)
        {
            if (string.IsNullOrEmpty(skillName) || string.IsNullOrEmpty(actionName)) return false;

            // 1. 完全相同
            if (string.Equals(skillName, actionName, StringComparison.OrdinalIgnoreCase)) return true;

            // 2. 将 actionName 中的 "_Action_" 替换为 "_"
            string actionWithoutAction = actionName.Replace("_Action_", "_");
            if (string.Equals(skillName, actionWithoutAction, StringComparison.OrdinalIgnoreCase)) return true;

            // 3. 将 actionName 中的 "_Action_Debuff_" 替换为 "_"
            string actionWithoutDebuff = actionName.Replace("_Action_Debuff_", "_");
            if (string.Equals(skillName, actionWithoutDebuff, StringComparison.OrdinalIgnoreCase)) return true;

            // 4. 将 skillName 插入 "_Action_" 进行比对
            if (!string.IsNullOrEmpty(roleName) && skillName.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = skillName.Substring(roleName.Length + 1);
                string expectedAction = $"{roleName}_Action_{suffix}";
                if (string.Equals(actionName, expectedAction, StringComparison.OrdinalIgnoreCase)) return true;

                string expectedDebuff = $"{roleName}_Action_Debuff_{suffix}";
                if (string.Equals(actionName, expectedDebuff, StringComparison.OrdinalIgnoreCase)) return true;
            }

            // 5. 提取动作短名后缀比对
            string skillSuffix = ExtractActionSuffix(skillName, roleName);
            string actionSuffix = ExtractActionSuffix(actionName, roleName);
            if (!string.IsNullOrEmpty(skillSuffix) && !string.IsNullOrEmpty(actionSuffix))
            {
                if (string.Equals(skillSuffix, actionSuffix, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals("Debuff_" + skillSuffix, actionSuffix, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string ExtractActionSuffix(string name, string roleName)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string s = name;
            if (!string.IsNullOrEmpty(roleName) && s.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(roleName.Length + 1);
            }
            if (s.StartsWith("Action_", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(7);
            }
            return s;
        }

        private static bool IsItemMatchCharacter(SkillTableItem item, string category, string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return true;

            // 1. 若怪物名在技能名称中出现（如 "TyrfingInfested_Attack_02"）
            if (item.Name.IndexOf(roleName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // 2. 若是角色且以角色名前缀命名
            if (item.Name.StartsWith(roleName + "_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 3. 基础通用技能（如果当前是通用分类）
            if (string.Equals(category, "Common", StringComparison.OrdinalIgnoreCase) && item.Name.StartsWith("基础_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string GetShortRoleName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return string.Empty;
            string clean = folderName.Trim().Replace('\\', '/').Trim('/');
            int slash = clean.LastIndexOf('/');
            return slash >= 0 ? clean.Substring(slash + 1) : clean;
        }
    }
}
