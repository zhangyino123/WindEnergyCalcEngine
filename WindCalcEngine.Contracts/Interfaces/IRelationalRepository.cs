using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.DTOs;
using WindCalcEngine.Contracts.Enums;
using WindCalcEngine.Contracts.Models;

namespace WindCalcEngine.Contracts.Interfaces
{
    /// <summary>
    /// 业务关系数据库访问契约 (对应 PostgreSQL / 达梦)
    /// </summary>
    public interface IRelationalRepository
    {
        /// <summary>
        /// 保存算出的最终能效体检报告 (写入 pg 业务表，Java端从中读取展示)
        /// </summary>
        Task SaveTurbineEfficiencyResultAsync(
            TurbineEfficiencyResultDto result,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新任务运行状态
        /// </summary>
        Task UpdateTaskStatusAsync(
            string taskId,
            TaskStateEnum state,
            string? errorMsg = null,
            CancellationToken cancellationToken = default);

        // ========== M2 阶段新增 ==========

        /// <summary>
        /// 获取某台风机的历史技改记录，用于确定 A/B Test 的时间切分点
        /// </summary>
        Task<IEnumerable<RectificationRecord>> GetRectificationRecordsAsync(string unitId, CancellationToken ct = default);

        /// <summary>
        /// 注册/更新 机器学习训练好的自适应基线模型信息
        /// </summary>
        Task SaveMlModelMetadataAsync(MlModelMetadata metadata, CancellationToken ct = default);

        /// <summary>
        /// 获取某风机最新或指定时间段的 ML 模型元数据
        /// </summary>
        Task<MlModelMetadata?> GetLatestMlModelMetadataAsync(string unitId, CancellationToken ct = default);
    }
}
