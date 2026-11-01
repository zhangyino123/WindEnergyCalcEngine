using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Enums;

namespace WindCalcEngine.Contracts.Models
{
    /// <summary>
    /// 标准化 SCADA 聚合测点 (10分钟/5分钟) - 算法的唯一输入源
    /// </summary>
    public class AggregatedScadaPoint
    {
        public required string UnitId { get; set; }
        public DateTime Timestamp { get; set; }

        // ------ 核心运行参数 ------
        public double ActivePower { get; set; }     // 平均有功功率 (kW)
        public double WindSpeed { get; set; }       // 平均风速 (m/s)
        public double WindDirection { get; set; }   // 风向平均值 (deg)
        public double NacellePosition { get; set; } // 机舱位置 (deg)

        // ------ 变桨与转速 ------
        public double PitchAngle1 { get; set; }
        public double PitchAngle2 { get; set; }
        public double PitchAngle3 { get; set; }
        public double RotorSpeed { get; set; }      // 叶轮转速 (rpm)
        public double GeneratorSpeed { get; set; }  // 发电机转速 (rpm)

        // ------ 环境参数 (用于空气密度计算) ------
        public double Temperature { get; set; }     // 舱外温度 (℃)
        public double Pressure { get; set; }        // 舱外气压 (hPa)

        // ------ 偏航高阶分析参数 ------
        // 必须有方差/极差，才能计算“动态偏航摇摆度”，不能只用平均值！
        public double YawErrorVariance { get; set; } // 10分钟内对风偏差的方差

        // ------ 质量标识 ------
        public DataQualityEnum QualityStatus { get; set; }
    }
}
