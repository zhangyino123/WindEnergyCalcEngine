using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.DTOs
{
    // ========== 以下为 M2 阶段新增 DTO ==========

    /// <summary>
    /// 技改前后效果评价请求 DTO
    /// </summary>
    public record RectificationEvaluationRequestDto
    {
        public required string UnitId { get; init; }
        public DateTime BeforePeriodStart { get; init; }
        public DateTime BeforePeriodEnd { get; init; }
        public DateTime AfterPeriodStart { get; init; }
        public DateTime AfterPeriodEnd { get; init; }
    }

}
