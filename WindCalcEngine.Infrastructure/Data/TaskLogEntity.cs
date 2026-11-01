using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Enums;

namespace WindCalcEngine.Infrastructure.Data
{
    /// <summary>
    /// 任务日志内部实体
    /// </summary>
    public class TaskLogEntity
    {
        public required string TaskId { get; set; }
        public TaskStateEnum State { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
