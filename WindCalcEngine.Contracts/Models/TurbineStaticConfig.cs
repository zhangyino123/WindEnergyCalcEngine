using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Enums;

namespace WindCalcEngine.Contracts.Models
{
    /// <summary>
    /// 机组静态档案与设计参数 (由 Redis 或 PostgreSQL 加载)
    /// </summary>
    public record TurbineStaticConfig
    {
        public required string UnitId { get; init; }
        public required string FarmId { get; init; }

        /// <summary>
        /// 额定功率 (kW)
        /// </summary>
        public double RatedPower { get; init; }

        /// <summary>
        /// 叶轮直径 (m)
        /// </summary>
        public double RotorDiameter { get; init; }

        /// <summary>
        /// 扫风面积 (m²) - 通过直径预计算，提升性能
        /// </summary>
        public double SweptArea => Math.PI * Math.Pow(RotorDiameter / 2.0, 2);

        /// <summary>
        /// 空气动力学最优设计 Cp 值 (通常为 0.45 左右，作为物理标尺基准)
        /// </summary>
        public double DesignCp { get; init; }

        /// <summary>
        /// 机舱传递函数(NTF)拟合多项式系数 [a, b, c] - 极为关键！
        /// 用于修正机舱测风仪风速 (V_free = a*V_nacelle^2 + b*V_nacelle + c)
        /// </summary>
        public double[] NtfCoefficients { get; init; } = [0, 1, 0]; // 默认不修正: V_free = V_nacelle

        public double CutInWindSpeed { get; init; }
        public double CutOutWindSpeed { get; init; }


   

        // ========== 新增：风资源归一化评估参数 ==========
        /// <summary>
        /// 当地标准风频 Weibull 分布尺度参数 A (衡量平均风速大小，如 7.0)
        /// </summary>
        public double WeibullScaleA { get; init; } = 7.0;

        /// <summary>
        /// 当地标准风频 Weibull 分布形状参数 k (衡量风速集中度，如 2.0)
        /// </summary>
        public double WeibullShapeK { get; init; } = 2.0;
    }

  

 
}
