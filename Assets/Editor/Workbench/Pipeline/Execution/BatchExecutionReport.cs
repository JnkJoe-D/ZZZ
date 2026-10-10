using System;
using System.Collections.Generic;

namespace Game.Editor.Workbench
{
    /// <summary>
    /// 批处理执行单项结果
    /// </summary>
    public class BatchExecutionItemResult
    {
        public ChangePlanItem PlanItem { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批处理执行全量审计报告 (BatchExecutionReport)
    /// </summary>
    public class BatchExecutionReport
    {
        public string PlanTitle { get; set; } = string.Empty;
        public DateTime ExecutionTime { get; set; } = DateTime.Now;
        public List<BatchExecutionItemResult> Results { get; } = new List<BatchExecutionItemResult>();

        public int TotalExecuted => Results.Count;
        public int SuccessCount => Results.FindAll(r => r.Success).Count;
        public int FailedCount => Results.FindAll(r => !r.Success).Count;
        public bool HasFailures => FailedCount > 0;

        public void AddResult(ChangePlanItem item, bool success, string message)
        {
            Results.Add(new BatchExecutionItemResult
            {
                PlanItem = item,
                Success = success,
                Message = message
            });
        }
    }
}
