using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Models
{
    /// <summary>
    /// AGC 场站级指令数据
    /// </summary>
    public class AgcSetpoint
    {
        public required string FarmId { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 场站 AGC 有功设定值 (MW)
        /// </summary>
        public double ActivePowerSetPointMw { get; set; }
    }
}
