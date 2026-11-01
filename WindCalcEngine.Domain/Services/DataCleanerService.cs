using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Enums;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 测点清洗与机舱风速修正域服务 (DataOps)
    /// </summary>
    public class DataCleanerService
    {
        /// <summary>
        /// 执行传感器清洗、质量打标与机舱风速传递函数(NTF)校准
        /// </summary>
        public AggregatedScadaPoint CleanAndCalibrate(AggregatedScadaPoint rawPt, TurbineStaticConfig config)
        {
            DataQualityEnum currentQuality = rawPt.QualityStatus;

            // 1. 传感器死值越界清洗与质量打标 (Clamped)
            double validTemp = rawPt.Temperature;
            if (rawPt.Temperature < -40 || rawPt.Temperature > 60 || double.IsNaN(rawPt.Temperature))
            {
                validTemp = 15.0;
                currentQuality = DataQualityEnum.Clamped;
            }

            double validPress = rawPt.Pressure;
            if (rawPt.Pressure < 800 || rawPt.Pressure > 1100 || double.IsNaN(rawPt.Pressure))
            {
                validPress = 1013.25; // 修正死值
                currentQuality = DataQualityEnum.Clamped;
            }

            // 2. NTF 机舱风速畸变修正 (V_free = a*V_nac^2 + b*V_nac + c)
            double vNac = rawPt.WindSpeed;
            double vFree = vNac;
            if (config.NtfCoefficients != null && config.NtfCoefficients.Length == 3)
            {
                var coef = config.NtfCoefficients;
                vFree = coef[0] * Math.Pow(vNac, 2) + coef[1] * vNac + coef[2];
                if (vFree < 0) vFree = 0;
            }

            return new AggregatedScadaPoint
            {
                UnitId = rawPt.UnitId,
                Timestamp = rawPt.Timestamp,
                ActivePower = rawPt.ActivePower,
                WindSpeed = vFree,
                WindDirection = rawPt.WindDirection,
                NacellePosition = rawPt.NacellePosition,
                PitchAngle1 = rawPt.PitchAngle1,
                PitchAngle2 = rawPt.PitchAngle2,
                PitchAngle3 = rawPt.PitchAngle3,
                RotorSpeed = rawPt.RotorSpeed,
                GeneratorSpeed = rawPt.GeneratorSpeed,
                Temperature = validTemp,
                Pressure = validPress,
                YawErrorVariance = rawPt.YawErrorVariance,
                QualityStatus = currentQuality // 记录该点是否被强制修正过
            };
        }
    }
}
