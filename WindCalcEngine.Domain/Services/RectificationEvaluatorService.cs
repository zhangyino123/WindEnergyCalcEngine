using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Models;
using WindCalcEngine.Domain.ValueObjects;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 技改优化效益 A/B Test 评估服务 (引入威布尔分布资源归一化)
    /// </summary>
    public class RectificationEvaluatorService
    {
        /// <summary>
        /// 评估两套拟合模型在标准年度风况下的等效发电量差异
        /// </summary>
        /// <param name="curveBefore">技改前(如刷机前)的实际功率函数模型</param>
        /// <param name="curveAfter">技改后(如刷机后)的实际功率函数模型</param>
        /// <param name="config">机组静态配置(包含当地 Weibull A和k 参数)</param>
        public RectificationEvaluationResultDto EvaluateImprovement(
            Func<double, double> curveBefore,
            Func<double, double> curveAfter,
            TurbineStaticConfig config)
        {
            var weibull = new WeibullDistribution(config.WeibullScaleA, config.WeibullShapeK);

            double aepBeforeKwh = 0;
            double aepAfterKwh = 0;
            double hoursPerYear = 8760.0;
            double binSize = 0.5;

            var ptsBefore = new List<PowerCurvePointDto>();
            var ptsAfter = new List<PowerCurvePointDto>();

            // 从切入风速到切出风速，进行概率密度积分
            for (double v = config.CutInWindSpeed; v <= config.CutOutWindSpeed; v += binSize)
            {
                double prob = weibull.GetProbabilityDensity(v);
                double hoursAtThisWind = prob * hoursPerYear; // 该风速一年中出现的小时数

                double pBefore = curveBefore(v);
                double pAfter = curveAfter(v);

                aepBeforeKwh += pBefore * hoursAtThisWind;
                aepAfterKwh += pAfter * hoursAtThisWind;

                ptsBefore.Add(new PowerCurvePointDto(v, Math.Round(pBefore, 2)));
                ptsAfter.Add(new PowerCurvePointDto(v, Math.Round(pAfter, 2)));
            }

            double stdAepBeforeMwh = Math.Round(aepBeforeKwh / 1000.0, 2);
            double stdAepAfterMwh = Math.Round(aepAfterKwh / 1000.0, 2);
            double improvementRate = stdAepBeforeMwh > 0 ? (stdAepAfterMwh - stdAepBeforeMwh) / stdAepBeforeMwh : 0;

            return new RectificationEvaluationResultDto
            {
                UnitId = config.UnitId,
                StandardAepBeforeMwh = stdAepBeforeMwh,
                StandardAepAfterMwh = stdAepAfterMwh,
                CurveBefore = ptsBefore,
                CurveAfter = ptsAfter,
                ConclusionText = $"归一化标准风况下，预期年增发电量为 {(stdAepAfterMwh - stdAepBeforeMwh):F2} MWh，设备性能实际提升 {improvementRate:P2}。"
            };
        }
    }
}
