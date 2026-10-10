using System;
using System.Collections.Generic;
using System.Linq;
using Game.Editor.Workspace;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 全流程工作台 - 角色完整业务上下文聚合根
    /// 统一聚合选定角色的共享定义、生效配置快照、Luban 元数据以及该角色在工程中的全部 Action 与 Timeline 资产索引。
    /// </summary>
    public class WorkbenchCharacterContext
    {
        public SharedWorkspaceDefinition Workspace { get; }
        public EffectiveWorkbenchConfig EffectiveConfig { get; }
        public LubanCharacterInfo LubanInfo { get; }

        public IReadOnlyList<ActionAssetIndexItem> Actions { get; }
        public IReadOnlyList<TimelineAssetIndexItem> Timelines { get; }

        public DateTime ScannedTime { get; }

        public int ActionCount => Actions.Count;
        public int FullyLinkedActionCount => Actions.Count(a => a.IsFullyLinked);
        public int MissingJsonActionCount => Actions.Count(a => !a.HasTimelineJson);
        public int MissingSoActionCount => Actions.Count(a => !a.HasTimelineSo);

        public int TimelineJsonCount => Timelines.Count(t => t.IsJson);
        public int TimelineSoCount => Timelines.Count(t => t.IsSo);

        public WorkbenchCharacterContext(
            SharedWorkspaceDefinition workspace,
            EffectiveWorkbenchConfig effectiveConfig,
            LubanCharacterInfo lubanInfo,
            IEnumerable<ActionAssetIndexItem> actions,
            IEnumerable<TimelineAssetIndexItem> timelines)
        {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            EffectiveConfig = effectiveConfig ?? throw new ArgumentNullException(nameof(effectiveConfig));
            LubanInfo = lubanInfo;

            Actions = actions != null ? new List<ActionAssetIndexItem>(actions).AsReadOnly() : Array.Empty<ActionAssetIndexItem>();
            Timelines = timelines != null ? new List<TimelineAssetIndexItem>(timelines).AsReadOnly() : Array.Empty<TimelineAssetIndexItem>();

            ScannedTime = DateTime.Now;
        }

        public ActionAssetIndexItem FindAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return null;
            return Actions.FirstOrDefault(a => string.Equals(a.ActionName, actionName, StringComparison.OrdinalIgnoreCase));
        }

        public ActionAssetIndexItem FindActionByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return Actions.FirstOrDefault(a => string.Equals(a.Guid, guid, StringComparison.OrdinalIgnoreCase));
        }

        public TimelineAssetIndexItem FindTimeline(string timelineName, bool? isJson = null)
        {
            if (string.IsNullOrEmpty(timelineName)) return null;
            return Timelines.FirstOrDefault(t =>
                string.Equals(t.TimelineName, timelineName, StringComparison.OrdinalIgnoreCase) &&
                (!isJson.HasValue || t.IsJson == isJson.Value));
        }

        public override string ToString()
        {
            return $"[WorkbenchCharacterContext: {Workspace.Id}] Actions={ActionCount} (FullyLinked={FullyLinkedActionCount}), Timelines={Timelines.Count} (JSON={TimelineJsonCount}, SO={TimelineSoCount})";
        }
    }
}
