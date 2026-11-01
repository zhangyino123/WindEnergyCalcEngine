using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Models;
using WindCalcEngine.Domain.MLModels;
using WindCalcEngine.Domain.ValueObjects;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 五项损失电量精算与多维散点归因服务 (总指挥调度)
    /// </summary>
    public class LossEvaluatorService
    {
        // 依赖注入独立的领域服务 (体现 SRP 单一职责)
        private readonly DataCleanerService _cleaner = new();
        private readonly PhysicsCalculator _physicsCalc = new();
        private readonly YawAnalyzer _yawAnalyzer = new();
        private readonly BaselineGeneratorService _baselineGen = new();

        public TurbineEfficiencyResultDto EvaluateEnergyLoss(
            TurbineStaticConfig config,
            IEnumerable<AggregatedScadaPoint> rawPoints,
            IEnumerable<AgcSetpoint> farmAgcSets,
            int resolutionMinutes)
        {
            double hoursPerPoint = resolutionMinutes / 60.0;
            double farmCapacityMw = config.RatedPower * 47 / 1000.0;

            var agcMap = farmAgcSets.ToDictionary(a => a.Timestamp, a => a.ActivePowerSetPointMw);

            double totalActEnergy = 0, totalTheoEnergy = 0;
            double agcActEnergy = 0, agcTheoEnergy = 0, icingActEnergy = 0, icingTheoEnergy = 0;
            double agcLoss = 0, perfLoss = 0;
            double agcStopHours = 0, agcDerateHours = 0;

            int cntIcing = 0, cntYaw = 0, cntPitch = 0, cntControl = 0;
            List<double> yawErrors = new();
            var validPointsForAi = new List<AggregatedScadaPoint>();

            foreach (var rawPt in rawPoints)
            {
                // 1. 调用 DataCleaner 清洗并打标
                var pt = _cleaner.CleanAndCalibrate(rawPt, config);

                if (pt.WindSpeed < config.CutInWindSpeed || double.IsNaN(pt.ActivePower)) continue;
                double validPower = pt.ActivePower < 0 ? 0 : pt.ActivePower;

                // 2. 调用 PhysicsCalculator 计算绝对基准
                double rho = EnvironmentStandard.CalculateAirDensity(pt.Pressure, pt.Temperature);
                double absTheoPwr = _physicsCalc.CalculateTheoreticalPower(pt.WindSpeed, rho, config);

                double actMwh = (validPower * hoursPerPoint) / 1000.0;
                double theoMwh = (absTheoPwr * hoursPerPoint) / 1000.0;

                totalActEnergy += actMwh;
                totalTheoEnergy += theoMwh;

                // AGC 融合判定
                bool isAgcCurtailed = agcMap.TryGetValue(pt.Timestamp, out double agcVal) &&
                                      (agcVal < farmCapacityMw * 0.95) &&
                                      (validPower < absTheoPwr * 0.90);

                if (isAgcCurtailed)
                {
                    agcActEnergy += actMwh;
                    agcTheoEnergy += theoMwh;
                    agcLoss += Math.Max(0, theoMwh - actMwh);
                    if (validPower <= 50) agcStopHours += hoursPerPoint;
                    else agcDerateHours += hoursPerPoint;
                }
                else
                {
                    perfLoss += Math.Max(0, theoMwh - actMwh);
                    validPointsForAi.Add(pt);
                }
            }

            // 3. 调用 BaselineGenerator 训练自学习 ML 模型 (接 Phase 2 的 ML.NET 引擎)
            var predictionEngine = _baselineGen.TrainBaselineModel(validPointsForAi, config);

            foreach (var pt in validPointsForAi)
            {
                double validPower = pt.ActivePower < 0 ? 0 : pt.ActivePower;
                double rho = EnvironmentStandard.CalculateAirDensity(pt.Pressure, pt.Temperature);
                double absTheoPwr = _physicsCalc.CalculateTheoreticalPower(pt.WindSpeed, rho, config);

                // 预测包络线基准
                double baselinePower = predictionEngine.Predict(new PowerCurveInput { WindSpeed = (float)pt.WindSpeed }).PredictedPower;
                if (baselinePower < 0) baselinePower = 0;

                // 偏航数据收集 (4-10m/s爬坡段)
                if (pt.WindSpeed >= 4.0 && pt.WindSpeed <= 10.0)
                {
                    double yErr = pt.WindDirection - pt.NacellePosition;
                    if (yErr > 180) yErr -= 360;
                    if (yErr < -180) yErr += 360;
                    yawErrors.Add(yErr);
                }

                bool isClassified = false;

                // 异常归因树
                if (validPower > absTheoPwr * 1.4 && pt.WindSpeed < 8.0 && validPower > 500)
                {
                    cntIcing++; isClassified = true;
                    icingActEnergy += (validPower * hoursPerPoint) / 1000.0;
                    icingTheoEnergy += (absTheoPwr * hoursPerPoint) / 1000.0;
                }

                if (!isClassified && validPower < baselinePower * 0.5 && pt.WindSpeed >= 3.0 && pt.WindSpeed <= 10.0)
                {
                    if (pt.WindSpeed < 7.5)
                    {
                        double pAvg = (pt.PitchAngle1 + pt.PitchAngle2 + pt.PitchAngle3) / 3.0;
                        double pMax = Math.Max(pt.PitchAngle1, Math.Max(pt.PitchAngle2, pt.PitchAngle3));
                        double pMin = Math.Min(pt.PitchAngle1, Math.Min(pt.PitchAngle2, pt.PitchAngle3));
                        if (pAvg > 3.0 || (pMax - pMin) > 2.0) { cntPitch++; isClassified = true; }
                    }

                    if (!isClassified)
                    {
                        double yawDeviation = Math.Abs((pt.WindDirection - pt.NacellePosition) % 360);
                        if (yawDeviation > 180) yawDeviation = 360 - yawDeviation;
                        if (yawDeviation > 15.0) { cntYaw++; isClassified = true; }
                    }

                    if (!isClassified) cntControl++;
                }
            }

            // 4. 调用独立的 YawAnalyzer 引擎
            var yawMetrics = _yawAnalyzer.Analyze(yawErrors);

            // PBA 计算
            double pbaAll = totalTheoEnergy > 0 ? (totalActEnergy / totalTheoEnergy) * 100.0 : 0;
            double validTheoNoAgc = totalTheoEnergy - agcTheoEnergy;
            double validActNoAgc = totalActEnergy - agcActEnergy;
            double pbaNoAgc = validTheoNoAgc > 0 ? (validActNoAgc / validTheoNoAgc) * 100.0 : 0;
            double validTheoPure = validTheoNoAgc - icingTheoEnergy;
            double validActPure = validActNoAgc - icingActEnergy;
            double pbaPure = validTheoPure > 0 ? (validActPure / validTheoPure) * 100.0 : 0;

            return new TurbineEfficiencyResultDto
            {
                UnitId = config.UnitId,
                StatDate = DateTime.Today,
                ActualEnergyMwh = Math.Round(totalActEnergy, 2),
                TheoreticalEnergyMwh = Math.Round(totalTheoEnergy, 2),
                PbaAll = Math.Round(pbaAll, 2),
                PbaNoAgc = Math.Round(pbaNoAgc, 2),
                PbaPure = Math.Round(pbaPure, 2),
                AgcLossMwh = Math.Round(agcLoss, 2),
                AgcStopHours = Math.Round(agcStopHours, 1),
                AgcDerateHours = Math.Round(agcDerateHours, 1),
                PerformanceLossMwh = Math.Round(perfLoss, 2),
                IcingAnomalyCount = cntIcing,
                YawMisalignmentCount = cntYaw,
                PitchErrorCount = cntPitch,
                ControlDeadbandCount = cntControl,

                // 填入从 YawAnalyzer 算出的结果
                YawStaticBias = Math.Round(yawMetrics.StaticBias, 2),
                YawDynamicVariance = Math.Round(yawMetrics.DynamicVariance, 2),
                YawPassRate = Math.Round(yawMetrics.PassRate, 2)
            };
        }
    }
}
