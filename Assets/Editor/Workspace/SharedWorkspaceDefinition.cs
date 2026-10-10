using System;
using System.Linq;

namespace Game.Editor.Workspace
{
    /// <summary>
    /// 中立公共角色工作区数据定义 (Shared Workspace Definition)
    /// 纯 POCO 数据模型，不含任何 Unity 场景/组件强引用，负责在全流程工作台与各编辑器工具间互通。
    /// </summary>
    [Serializable]
    public class SharedWorkspaceDefinition
    {
        public const string CategoryRole = "Role";
        public const string CategoryMonster = "Monster";
        public const string CategoryCommon = "Common";

        public static readonly string[] ValidCategories = new[]
        {
            CategoryRole,
            CategoryMonster,
            CategoryCommon
        };

        /// <summary>
        /// 工作区全局稳定唯一键，如 "Role_Ellen", "Monster_TyrfingInfested", "Common_Shared"
        /// </summary>
        public string Id;

        /// <summary>
        /// 固定角色分类："Role" | "Monster" | "Common"
        /// </summary>
        public string Category = CategoryRole;

        /// <summary>
        /// 界面显示名，如 "艾莲 (Ellen)"
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 相对子目录名，如 "Role/Ellen", "Monster/TyrfingInfested", "Common"
        /// </summary>
        public string FolderName;

        /// <summary>
        /// 排序权重（升序）
        /// </summary>
        public int Order = 0;

        /// <summary>
        /// 角色描述或备忘说明
        /// </summary>
        public string Description;

        public SharedWorkspaceDefinition() { }

        public SharedWorkspaceDefinition(string id, string category, string displayName, string folderName, int order = 0, string description = "")
        {
            Id = id;
            Category = category;
            DisplayName = displayName;
            FolderName = folderName;
            Order = order;
            Description = description;
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Id))
            {
                errorMessage = "工作区 Id 不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Category))
            {
                errorMessage = "分类 Category 不能为空。";
                return false;
            }

            if (!ValidCategories.Contains(Category, StringComparer.OrdinalIgnoreCase))
            {
                errorMessage = $"分类 '{Category}' 非法。当前允许的分类固定为：{string.Join(", ", ValidCategories)}。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                errorMessage = "显示名称 DisplayName 不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FolderName))
            {
                errorMessage = "物理子目录 FolderName 不能为空。";
                return false;
            }

            char[] invalidChars = System.IO.Path.GetInvalidPathChars();
            if (FolderName.IndexOfAny(invalidChars) >= 0 || FolderName.Contains(":") || FolderName.Contains("*") || FolderName.Contains("?"))
            {
                errorMessage = "物理子目录 FolderName 包含非法字符。";
                return false;
            }

            return true;
        }

        public SharedWorkspaceDefinition Clone()
        {
            return new SharedWorkspaceDefinition
            {
                Id = this.Id,
                Category = this.Category,
                DisplayName = this.DisplayName,
                FolderName = this.FolderName,
                Order = this.Order,
                Description = this.Description
            };
        }
    }
}
