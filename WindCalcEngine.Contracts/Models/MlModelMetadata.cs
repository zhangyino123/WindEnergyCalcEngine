using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Models
{
    /// <summary>
    /// 机器学习动态包络线模型元数据注册表
    /// </summary>
    public record MlModelMetadata
    {
        public required string ModelId { get; init; }
        public required string UnitId { get; init; }
        public DateTime TrainedAt { get; init; }

        /// <summary>
        /// 模型文件(如 .zip)在 MinIO 中的存储路径
        /// </summary>
        public required string MinioPath { get; init; }

        /// <summary>
        /// 模型拟合优度 R-Squared (用于评估模型置信度)
        /// </summary>
        public double RSquared { get; init; }
    }
}
