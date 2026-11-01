using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.DTOs
{
    /// <summary>
    /// 计算任务请求 DTO (由 RocketMQ 投递)
    /// </summary>
    public record CalcTaskRequestDto
    {
        public required string TaskId { get; init; }
        public required string FarmId { get; init; }
        public required DateTime StartTime { get; init; }
        public required DateTime EndTime { get; init; }

        /// <summary>
        /// 数据分辨率 (如 10 分钟, 5分钟) - 决定电量积分的权重
        /// </summary>
        public int ResolutionMinutes { get; init; }

        /// <summary>
        /// 是否执行强制重算 (覆盖历史数据)
        /// </summary>
        public bool IsForceRecalculate { get; init; }
    }
}
