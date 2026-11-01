using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Domain.MLModels
{
    /// <summary>
    /// 机器学习输入特征模型 (特征：风速；标签：功率)
    /// </summary>
    public class PowerCurveInput
    {
        [LoadColumn(0)]
        public float WindSpeed { get; set; }

        [LoadColumn(1)]
        public float Power { get; set; }
    }

    /// <summary>
    /// 机器学习预测输出模型
    /// </summary>
    public class PowerCurvePrediction
    {
        // ML.NET 默认将回归结果输出到 "Score" 列
        [ColumnName("Score")]
        public float PredictedPower { get; set; }
    }
}
