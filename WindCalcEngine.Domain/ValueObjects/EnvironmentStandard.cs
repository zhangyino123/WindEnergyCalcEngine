using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Domain.ValueObjects
{
    /// <summary>
    /// 国际电工委员会 (IEC) 标准环境物理常数
    /// </summary>
    public static class EnvironmentStandard
    {
        public const double StandardAirDensity = 1.225; // 标准空气密度 (kg/m³)
        public const double GasConstantR = 287.058;     // 理想气体常数 (J/(kg·K))
        public const double AbsoluteZeroCelsius = -273.15; // 绝对零度

        /// <summary>
        /// 计算实时空气密度 (ρ = P / (R * T))
        /// </summary>
        public static double CalculateAirDensity(double pressureHpa, double temperatureCelsius)
        {
            double tempKelvin = temperatureCelsius - AbsoluteZeroCelsius;
            double pressurePa = pressureHpa * 100; // hPa 转 Pa
            return pressurePa / (GasConstantR * tempKelvin);
        }
    }
}
