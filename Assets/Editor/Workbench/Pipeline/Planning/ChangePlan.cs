using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 变更操作类型
    /// </summary>
    public enum ChangeOperationType
    {
        /// <summary>基于配表创建新的 Action 资产</summary>
        CreateAction,

        /// <summary>为已有 Action 资产对齐绑定时间轴 (JSON / SO)</summary>
        BindTimeline,

        /// <summary>资产重命名</summary>
        RenameAsset,

        /// <summary>资产路径迁移</summary>
        MoveAsset
    }

    /// <summary>
    /// 变更计划冲突与就绪状态
    /// </summary>
    public enum ChangeConflictStatus
    {
        /// <summary>安全就绪，无冲突</summary>
        Ready,

        /// <summary>目标资产已存在冲突</summary>
        ConflictTargetExists,

        /// <summary>源文件未找到</summary>
        SourceNotFound,

        /// <summary>非法命名或非法相对路径</summary>
        InvalidIdentifier,

        /// <summary>已处于完全绑定状态，无需重复操作</summary>
        AlreadyLinked,

        /// <summary>数据源异常或不支持的分类</summary>
        Unsupported
    }

    /// <summary>
    /// 单项变更计划项
    /// </summary>
    public class ChangePlanItem
    {
        /// <summary>唯一标识（如配表技能 ID 或动作名）</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>操作类型</summary>
        public ChangeOperationType OperationType { get; set; }

        /// <summary>源对象名称或原始技能名称</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>目标资产名称</summary>
        public string TargetName { get; set; } = string.Empty;

        /// <summary>源文件路径（若为新建则为空）</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>目标保存相对路径</summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>拟绑定的 JSON 时间轴路径</summary>
        public string MatchedJsonPath { get; set; } = string.Empty;

        /// <summary>拟绑定的 SO 时间轴路径</summary>
        public string MatchedSoPath { get; set; } = string.Empty;

        /// <summary>冲突与就绪状态</summary>
        public ChangeConflictStatus ConflictStatus { get; set; } = ChangeConflictStatus.Ready;

        /// <summary>状态详细描述或冲突原因</summary>
        public string StatusDescription { get; set; } = string.Empty;

        /// <summary>是否在界面中被勾选（默认 true，冲突项默认 false）</summary>
        public bool IsSelected { get; set; } = true;

        /// <summary>关联的上下文或业务数据载体（如 SkillTableItem 或 ActionAssetIndexItem）</summary>
        public object UserData { get; set; }

        /// <summary>是否属于阻塞性严重冲突</summary>
        public bool HasBlockingConflict =>
            ConflictStatus == ChangeConflictStatus.ConflictTargetExists ||
            ConflictStatus == ChangeConflictStatus.SourceNotFound ||
            ConflictStatus == ChangeConflictStatus.InvalidIdentifier ||
            ConflictStatus == ChangeConflictStatus.Unsupported;
    }

    /// <summary>
    /// 完整变更计划聚合根 (ChangePlan)
    /// </summary>
    public class ChangePlan
    {
        /// <summary>计划标题</summary>
        public string Title { get; set; } = "资产变更计划";

        /// <summary>计划说明</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>所有变更条目</summary>
        public List<ChangePlanItem> Items { get; } = new List<ChangePlanItem>();

        public int TotalCount => Items.Count;
        public int SelectedCount => Items.Count(i => i.IsSelected);
        public int ConflictCount => Items.Count(i => i.HasBlockingConflict);
        public int ReadyCount => Items.Count(i => i.ConflictStatus == ChangeConflictStatus.Ready);

        public bool HasSelectedItems => Items.Any(i => i.IsSelected);
        public bool HasSelectedConflicts => Items.Any(i => i.IsSelected && i.HasBlockingConflict);

        public ChangePlan(string title, string description = "")
        {
            Title = title;
            Description = description;
        }

        public void AddItem(ChangePlanItem item)
        {
            if (item == null) return;
            // 若存在阻塞冲突，默认不勾选，防误触
            if (item.HasBlockingConflict)
            {
                item.IsSelected = false;
            }
            Items.Add(item);
        }

        public void SelectAll(bool select)
        {
            foreach (var item in Items)
            {
                if (select && item.HasBlockingConflict) continue;
                item.IsSelected = select;
            }
        }
    }
}
