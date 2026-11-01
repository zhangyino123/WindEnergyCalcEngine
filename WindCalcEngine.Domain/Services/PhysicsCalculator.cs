using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Domain.Services
{
    /// <summary>
    /// 空气动力学域服务 (物理标尺计算)
    /// </summary>
    public class PhysicsCalculator
    {
        /// <summary>
        /// 基于绝对物理公式的理论功率计算 (平滑限幅)
        /// </summary>
        /// <param name="windSpeed">经过NTF修正的自由流风速 (m/s)</param>
        /// <param name="airDensity">实时空气密度 (kg/m³)</param>
        /// <param name="config">风机静态设计档案</param>
        /// <returns>理论有功功率 (kW)</returns>
        public double CalculateTheoreticalPower(double windSpeed, double airDensity, TurbineStaticConfig config)
        {
            // 小于切入或大于切出，理论不发电
            if (windSpeed < config.CutInWindSpeed || windSpeed > config.CutOutWindSpeed)
                return 0;

            // 核心物理公式: P = 0.5 * ρ * A * V³ * Cp / 1000
            double theoPower = 0.5 * airDensity * config.SweptArea * Math.Pow(windSpeed, 3) * config.DesignCp / 1000.0;

            // 严格按照发电机设计额定功率截断保护 (允许极小的 5% 超发容忍)
            return theoPower > config.RatedPower * 1.05 ? config.RatedPower * 1.05 : theoPower;
        }
    }
}
