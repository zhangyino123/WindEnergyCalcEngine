using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Models;
using WindCalcEngine.Domain.MLModels;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 工业级动态自学习基线生成引擎 (分箱包络 + ML.NET 梯度提升树)
    /// </summary>
    public class BaselineGeneratorService
    {
        private const double BinSize = 0.5; // 分箱步长

        /// <summary>
        /// 训练风机专属的非线性功率曲线模型
        /// </summary>
        public PredictionEngine<PowerCurveInput, PowerCurvePrediction> TrainBaselineModel(
            IEnumerable<AggregatedScadaPoint> cleanPoints,
            TurbineStaticConfig config)
        {
            // ================= 1. 工业级数据清洗与特征工程 =================
            var bins = new Dictionary<double, List<double>>();

            foreach (var pt in cleanPoints)
            {
                // 限定风速区间，极低风和极高风不参与训练
                if (pt.WindSpeed < 2.0 || pt.WindSpeed > 25.0) continue;

                double binKey = Math.Floor(pt.WindSpeed / BinSize) * BinSize;
                if (!bins.ContainsKey(binKey)) bins[binKey] = new List<double>();
                bins[binKey].Add(pt.ActivePower);
            }

            var envelopePoints = new List<PowerCurveInput>();

            // 提取每个风速箱子的 Top 5% 作为“能力天花板”，过滤掉散点下方的掉出力脏数据
            foreach (var bin in bins)
            {
                var powers = bin.Value.OrderByDescending(p => p).ToList();
                int topCount = Math.Max(1, (int)(powers.Count * 0.05));
                double maxCapability = powers.Take(topCount).Average();

                // 物理常识平滑限幅
                if (maxCapability > config.RatedPower * 1.05) maxCapability = config.RatedPower * 1.05;
                if (maxCapability < 0) maxCapability = 0;

                // 存入训练集 (取箱子中心点风速)
                envelopePoints.Add(new PowerCurveInput
                {
                    WindSpeed = (float)(bin.Key + BinSize / 2),
                    Power = (float)maxCapability
                });
            }

            // 补充 0m/s 的边界点，防止模型在极低风速时发散预测出负数
            envelopePoints.Add(new PowerCurveInput { WindSpeed = 0f, Power = 0f });

            // ================= 2. ML.NET 模型训练 =================
            var mlContext = new MLContext(seed: 42); // 固定种子确保每次训练可复现
            var trainingData = mlContext.Data.LoadFromEnumerable(envelopePoints);

            // 构建数据处理流水线
            // 风电功率曲线是非线性的(爬坡段三次幂，满发段平移)，选用 FastTree (梯度提升树) 最为合适
            var pipeline = mlContext.Transforms.Concatenate("Features", nameof(PowerCurveInput.WindSpeed))
                .Append(mlContext.Regression.Trainers.FastTree(
                    labelColumnName: nameof(PowerCurveInput.Power),
                    featureColumnName: "Features",
                    numberOfLeaves: 20,         // 控制树的复杂度
                    numberOfTrees: 100,         // 森林规模
                    minimumExampleCountPerLeaf: 2, // 防止过拟合
                    learningRate: 0.2));

            // 训练模型
            var trainedModel = pipeline.Fit(trainingData);

            // 创建高频预测引擎 (PredictionEngine 是非线程安全的，建议每次分析任务创建一个)
            var predictionEngine = mlContext.Model.CreatePredictionEngine<PowerCurveInput, PowerCurvePrediction>(trainedModel);

            return predictionEngine;
        }
    }
}
