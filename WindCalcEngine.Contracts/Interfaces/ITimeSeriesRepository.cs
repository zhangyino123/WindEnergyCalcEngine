using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Enums;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Contracts.Interfaces
{
    /// <summary>
    /// 时序数据库访问契约 (对应 TDengine / 庚顿 / 麦杰)
    /// </summary>
    public interface ITimeSeriesRepository
    {
        /// <summary>
        /// 获取指定时间段内的 10/5 分钟聚合 SCADA 数据
        /// </summary>
        Task<IEnumerable<AggregatedScadaPoint>> GetAggregatedPointsAsync(
            string unitId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量写入经过 C# 清洗与二次聚合的标准化测点 (存回时序库，供日后审计)
        /// </summary>
        Task BulkInsertAggregatedPointsAsync(
            IEnumerable<AggregatedScadaPoint> points,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取场站级 AGC 设定值序列
        /// </summary>
        Task<IEnumerable<AgcSetpoint>> GetFarmAgcSetpointsAsync(
            string farmId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);
    }

   

   
}
