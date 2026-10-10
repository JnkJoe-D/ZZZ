using System;
using UnityEngine;
using Game.Editor.Workspace;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditor 角色工作区定义
    /// 承载 ATEditor 编辑器专有的预览场景配置（PreviewPrefab、站位与朝向），
    /// 并与中立公共 SharedWorkspaceDefinition 进行字段映射与双向同步。
    /// </summary>
    [Serializable]
    public class ATWorkspaceDefinition
    {
        public string Id;                  // 唯一标识，如 "Role_Ellen", "Monster_TyrfingInfested"
        public string Category;            // 固定分类："Role" | "Monster" | "Common"
        public string DisplayName;         // UI 显示名称，如 "艾莲 (Ellen)"
        public string FolderName;          // 相对子目录名，如 "Role/Ellen", "Monster/TyrfingInfested"
        
        [Header("预览设置")]
        public GameObject PreviewPrefab;   // 绑定的角色预制体（唯一模型源）
        public Vector3 SpawnPosition = Vector3.zero;      // 生成原点坐标
        public Vector3 SpawnRotationEuler = Vector3.zero; // 生成默认旋转

        public ATWorkspaceDefinition() { }

        public ATWorkspaceDefinition(string id, string category, string displayName, string folderName, GameObject previewPrefab = null)
        {
            Id = id;
            Category = category;
            DisplayName = displayName;
            FolderName = folderName;
            PreviewPrefab = previewPrefab;
        }

        public ATWorkspaceDefinition(SharedWorkspaceDefinition shared, GameObject previewPrefab = null)
        {
            if (shared != null)
            {
                Id = shared.Id;
                Category = shared.Category;
                DisplayName = shared.DisplayName;
                FolderName = shared.FolderName;
            }
            PreviewPrefab = previewPrefab;
        }

        public SharedWorkspaceDefinition ToSharedDefinition()
        {
            return new SharedWorkspaceDefinition(
                id: this.Id,
                category: this.Category,
                displayName: this.DisplayName,
                folderName: this.FolderName
            );
        }

        public void ApplySharedDefinition(SharedWorkspaceDefinition shared)
        {
            if (shared == null) return;
            Id = shared.Id;
            Category = shared.Category;
            DisplayName = shared.DisplayName;
            FolderName = shared.FolderName;
        }

        public ATWorkspaceDefinition Clone()
        {
            return new ATWorkspaceDefinition
            {
                Id = this.Id,
                Category = this.Category,
                DisplayName = this.DisplayName,
                FolderName = this.FolderName,
                PreviewPrefab = this.PreviewPrefab,
                SpawnPosition = this.SpawnPosition,
                SpawnRotationEuler = this.SpawnRotationEuler
            };
        }
    }
}
