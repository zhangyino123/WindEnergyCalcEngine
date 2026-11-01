using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 偏航动力学诊断评估结果
    /// </summary>
    public record YawMetricsResult(double StaticBias, double DynamicVariance, double PassRate);

    /// <summary>
    /// 偏航诊断引擎
    /// </summary>
    public class YawAnalyzer
    {
        private const double YawDeadband = 15.0; // 大型风机偏航容差死区 ±15°

        /// <summary>
        /// 计算偏航多维指标
        /// </summary>
        /// <param name="yawErrors">在爬坡段提取的对风偏差序列(已处理360度卷绕)</param>
        public YawMetricsResult Analyze(IReadOnlyList<double> yawErrors)
        {
            if (yawErrors == null || !yawErrors.Any())
            {
                return new YawMetricsResult(0, 0, 0);
            }

            // 1. 静态偏置 (算术平均值，反映风向标安装是否歪了)
            double staticBias = yawErrors.Average();

            // 2. 动态摇摆方差 (标准差，反映偏航阻尼、频繁寻风问题)
            // 使用总体标准差公式
            double sumOfSquares = yawErrors.Sum(v => Math.Pow(v - staticBias, 2));
            double dynamicVariance = Math.Sqrt(sumOfSquares / yawErrors.Count);

            // 3. 容差死区合格率
            int passCount = yawErrors.Count(e => Math.Abs(e) <= YawDeadband);
            double passRate = (passCount / (double)yawErrors.Count) * 100.0;

            return new YawMetricsResult(staticBias, dynamicVariance, passRate);
        }
    }
}
