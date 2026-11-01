using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.DTOs
{
    /// <summary>
    /// 技改整改效益评估报告 DTO (消除风资源差异后的标准评价)
    /// </summary>
    public record RectificationEvaluationResultDto
    {
        public required string UnitId { get; init; }

        /// <summary>
        /// 技改前，在标准威布尔风况下运行一年的等效发电量 (MWh)
        /// </summary>
        public double StandardAepBeforeMwh { get; init; }

        /// <summary>
        /// 技改后，在【同等】标准风况下运行一年的等效发电量 (MWh)
        /// </summary>
        public double StandardAepAfterMwh { get; init; }

        /// <summary>
        /// 年化等效增发电量绝对值 (MWh) -> 向管理层汇报的经济账
        /// </summary>
        public double ImprovedEnergyMwh => StandardAepAfterMwh - StandardAepBeforeMwh;

        /// <summary>
        /// 纯设备气动效能提升比例 (%)
        /// </summary>
        public double ImprovementRate => (StandardAepAfterMwh - StandardAepBeforeMwh) / StandardAepBeforeMwh;

        /// <summary>
        /// 技改前后的真实健康度对比
        /// </summary>
        public double PbaPureBefore { get; init; }
        public double PbaPureAfter { get; init; }

        /// <summary>
        /// 技改前 AI 拟合的实际功率曲线 (前端画虚线)
        /// </summary>
        public List<PowerCurvePointDto> CurveBefore { get; init; } = [];

        /// <summary>
        /// 技改后 AI 拟合的实际功率曲线 (前端画实线)
        /// </summary>
        public List<PowerCurvePointDto> CurveAfter { get; init; } = [];

        /// <summary>
        /// 诊断结论文本
        /// </summary>
        public required string ConclusionText { get; init; }
    }
}
