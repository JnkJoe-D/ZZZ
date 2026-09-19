using System;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// ATEditor 角色工作区定义
    /// 描述单个角色或实体的编辑上下文与资源目录
    /// 严格独立于 GamePlay 命名空间
    /// </summary>
    [Serializable]
    public class ATWorkspaceDefinition
    {
        public string Id;                  // 唯一标识，如 "Player_Ellen"
        public string Category;            // 分类："Player" | "Monster" | "NPC" | "Common"
        public string DisplayName;         // UI 显示名称，如 "艾莲 (Ellen)"
        public string FolderName;          // 相对子目录名（全英文规范），如 "Player/Ellen"
        
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
