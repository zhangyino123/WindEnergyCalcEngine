using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.DTOs
{
    /// <summary>
    /// 机组能效诊断与电量精算综合结果 DTO
    /// </summary>
    public record TurbineEfficiencyResultDto
    {
        public required string UnitId { get; init; }
        public DateTime StatDate { get; init; } // 统计维度 (日/月)

        // ========== 1. MWh 积分电量 ==========
        public double ActualEnergyMwh { get; init; }       // 实际发电量
        public double TheoreticalEnergyMwh { get; init; }  // 物理基准应发电量

        // ========== 2. 多维 PBA (核心考核指标) ==========
        public double PbaAll { get; init; }      // 全口径风能利用率
        public double PbaNoAgc { get; init; }    // 去限电 PBA
        public double PbaPure { get; init; }     // 去限电、去测风异常 纯净PBA

        // ========== 3. 损失拆解与时长 ==========
        public double AgcLossMwh { get; init; }           // AGC 调度限电损失
        public double AgcStopHours { get; init; }         // 限电停机时长 (h)
        public double AgcDerateHours { get; init; }       // 限电降容时长 (h)

        public double PerformanceLossMwh { get; init; }   // 自由发电期机组性能损失

        // ========== 4. 散点多维定界 (点数统计) ==========
        public int IcingAnomalyCount { get; init; }       // 风速仪结冰超发点数
        public int YawMisalignmentCount { get; init; }    // 偏航失准掉出力点数
        public int PitchErrorCount { get; init; }         // 变桨异常点数
        public int ControlDeadbandCount { get; init; }    // 扭矩控制死区点数

        // ========== 5. 偏航动力学诊断 ==========
        public double YawStaticBias { get; init; }        // 静态偏置误差 (对风偏移量，需校对风向标)
        public double YawDynamicVariance { get; init; }   // 动态摇摆度 (偏航系统阻尼健康度)
        public double YawPassRate { get; init; }          // 对风合格率 (偏差 ±15° 内的占比)
    }


}
