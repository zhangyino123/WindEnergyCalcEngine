using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 机组可靠性运行评价服务 (遵循 GB/T 43904-2024)
    /// 补齐前端未涉及的 MTBF、MTTR 等核心时间维度指标
    /// </summary>
    public class ReliabilityEvaluatorService
    {
        public record ReliabilityMetrics(double MTBF, double MTTR, double TimeBasedAvailability);

        /// <summary>
        /// 计算可靠性指标
        /// </summary>
        /// <param name="scadaStatusCodes">一段时间内的 SCADA 状态码序列 (假设10分钟一个点)</param>
        public ReliabilityMetrics CalculateReliability(List<string> scadaStatusCodes, int resolutionMinutes)
        {
            double totalHours = scadaStatusCodes.Count * (resolutionMinutes / 60.0);

            int faultCount = 0;
            double faultHours = 0;
            double maintenanceHours = 0;

            bool inFault = false;

            foreach (var code in scadaStatusCodes)
            {
                // 依据国标：故障状态、检修状态提取
                if (code == "FAULT")
                {
                    faultHours += (resolutionMinutes / 60.0);
                    if (!inFault)
                    {
                        faultCount++; // 触发首次故障计 1 次
                        inFault = true;
                    }
                }
                else
                {
                    inFault = false;
                    if (code == "MAINTENANCE") maintenanceHours += (resolutionMinutes / 60.0);
                }
            }

            double mtbf = faultCount > 0 ? (totalHours - faultHours - maintenanceHours) / faultCount : totalHours;
            double mttr = faultCount > 0 ? faultHours / faultCount : 0;

            // TBA 基于时间的可利用率 = (总时间 - 故障停机时间 - 检修时间) / 总时间
            double tba = totalHours > 0 ? (totalHours - faultHours - maintenanceHours) / totalHours * 100 : 0;

            return new ReliabilityMetrics(Math.Round(mtbf, 1), Math.Round(mttr, 2), Math.Round(tba, 2));
        }
    }
}
