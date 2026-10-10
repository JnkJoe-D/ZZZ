using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Workspace
{
    /// <summary>
    /// 公共角色工作区数据根容器 (Shared Workspace Data Root)
    /// 序列化为 SharedWorkspaces.json，作为各工具共享的单一权威源。
    /// </summary>
    [Serializable]
    public class SharedWorkspaceData
    {
        public int SchemaVersion = 1;

        public List<string> Categories = new List<string>
        {
            SharedWorkspaceDefinition.CategoryRole,
            SharedWorkspaceDefinition.CategoryMonster,
            SharedWorkspaceDefinition.CategoryCommon
        };

        public List<SharedWorkspaceDefinition> Workspaces = new List<SharedWorkspaceDefinition>();

        public void EnsureDefaults()
        {
            if (Categories == null || Categories.Count == 0)
            {
                Categories = new List<string>
                {
                    SharedWorkspaceDefinition.CategoryRole,
                    SharedWorkspaceDefinition.CategoryMonster,
                    SharedWorkspaceDefinition.CategoryCommon
                };
            }
            else
            {
                // 确保固定分类存在且顺序标准
                foreach (var fixedCat in SharedWorkspaceDefinition.ValidCategories)
                {
                    if (!Categories.Contains(fixedCat, StringComparer.OrdinalIgnoreCase))
                    {
                        Categories.Add(fixedCat);
                    }
                }
            }

            if (Workspaces == null)
            {
                Workspaces = new List<SharedWorkspaceDefinition>();
            }
        }

        public SharedWorkspaceDefinition FindById(string id)
        {
            if (string.IsNullOrEmpty(id) || Workspaces == null) return null;

            return Workspaces.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public SharedWorkspaceDefinition GetWorkspaceById(string id) => FindById(id);

        public List<SharedWorkspaceDefinition> FindByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || Workspaces == null) return new List<SharedWorkspaceDefinition>();
            return Workspaces
                .Where(w => string.Equals(w.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.Order)
                .ThenBy(w => w.DisplayName)
                .ToList();
        }

        public bool AddOrUpdate(SharedWorkspaceDefinition def, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (def == null)
            {
                errorMessage = "工作区定义不能为 null。";
                return false;
            }

            if (!def.Validate(out errorMessage))
            {
                return false;
            }

            EnsureDefaults();

            int index = Workspaces.FindIndex(w => string.Equals(w.Id, def.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                Workspaces[index] = def.Clone();
            }
            else
            {
                Workspaces.Add(def.Clone());
            }

            // 按 Order 排序
            Workspaces.Sort((a, b) => a.Order.CompareTo(b.Order));
            return true;
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id) || Workspaces == null) return false;
            int removed = Workspaces.RemoveAll(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            return removed > 0;
        }
    }
}
