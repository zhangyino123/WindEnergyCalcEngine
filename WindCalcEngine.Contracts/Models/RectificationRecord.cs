using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Enums;

namespace WindCalcEngine.Contracts.Models
{
    /// <summary>
    /// 机组技改/整改历史台账 (写入 PostgreSQL)
    /// </summary>
    public record RectificationRecord
    {
        public required string RecordId { get; init; }
        public required string UnitId { get; init; }
        public DateTime RectificationDate { get; init; }
        public RectificationTypeEnum Type { get; init; }
        public required string Description { get; init; }
    }
}
