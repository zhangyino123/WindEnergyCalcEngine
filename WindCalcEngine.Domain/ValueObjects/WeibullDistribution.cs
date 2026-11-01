using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Domain.ValueObjects
{
    /// <summary>
    /// 威布尔(Weibull)风频分布模型 - 用于消除技改前后的风资源差异
    /// </summary>
    public record WeibullDistribution(double ScaleA, double ShapeK)
    {
        public double GetProbabilityDensity(double windSpeed)
        {
            if (windSpeed < 0) return 0;
            return (ShapeK / ScaleA)
                   * Math.Pow(windSpeed / ScaleA, ShapeK - 1)
                   * Math.Exp(-Math.Pow(windSpeed / ScaleA, ShapeK));
        }
    }
}
